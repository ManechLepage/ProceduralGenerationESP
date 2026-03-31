using UnityEngine;
using System.Collections.Generic;

public class MinecraftConverter : MonoBehaviour
{
    /*
    Cette classe permet la conversion de heightmap en fichier .schem (v2) pour Minecraft.

    Étapes clés :
     1 - Créer une palette de blocs sous forme de dictionnaire "nom" -> indice
     2 - Convertir la heightmap en array 3d de blocs (selon la block palette)
     3 - Créer le fichier .schem en utilisant le array 3d
    
    Ce fichier dépend donc du fichier SchematicConverter.cs
    */

    public SchematicConverter schematicConverter;

    public void SaveToSchem(List<List<float>> heightMap, string path, MinecraftConverterSettings settings)
    {
        // First, create the block palette dictionary
        Dictionary<string, int> paletteDict = CreateBlockPalette(settings.blockPalette);
        int[,,] blockMap = CreateBlockMap(heightMap, settings, paletteDict);
        SchematicConverter.Export(blockMap, paletteDict, path);

    }

    public int[,,] CreateBlockMap(List<List<float>> heightMap, MinecraftConverterSettings settings, Dictionary<string, int> paletteDict)
    {
        int width = settings.size.x;
        int length = settings.size.y;
        int height = settings.height;

        int[,,] blockMap = new int[width, height, length];

        for (int z = 0; z < length; z++)
        {
            for (int x = 0; x < width; x++)
            {
                float normalizedHeight = SampleHeightFromInterpolation(heightMap, x, z, settings.size);
                float slope = CalculateSlope(heightMap, x, z, settings.size) * 7.5f;
                int blockHeight = Mathf.RoundToInt(normalizedHeight * (height - 1));

                for (int y = 0; y <= blockHeight; y++)
                {
                    float localHeight = normalizedHeight - (blockHeight - y) / (float)height;
                    int blockType = paletteDict[GetBlockFromHeightAndSlope(localHeight, slope, settings.blockPalette)];
                    blockMap[x, y, z] = blockType;
                }

                for (int y = blockHeight + 1; y < height; y++)
                {
                    blockMap[x, y, z] = paletteDict["minecraft:air"];
                }
            }
        }

        return blockMap;
    }

    public float SampleHeightFromInterpolation(List<List<float>> heightMap, float x, float z, Vector2Int targetSize)
    {
        Vector2Int heightMapSize = new Vector2Int(heightMap[0].Count, heightMap.Count);
        float mappedX = (float)x / targetSize.x * (heightMapSize.x - 1);
        float mappedZ = (float)z / targetSize.y * (heightMapSize.y - 1);

        int heightMapX = Mathf.FloorToInt(mappedX);
        int heightMapZ = Mathf.FloorToInt(mappedZ);

        float xDiff = mappedX - heightMapX;
        float zDiff = mappedZ - heightMapZ;

        float height1 = heightMap[heightMapZ][heightMapX];
        float height2 = (heightMapX < heightMapSize.x - 1) ? heightMap[heightMapZ][heightMapX + 1] : height1;
        float height3 = (heightMapZ < heightMapSize.y - 1) ? heightMap[heightMapZ + 1][heightMapX] : height1;
        float height4 = (heightMapX < heightMapSize.x - 1 && heightMapZ < heightMapSize.y - 1) ? heightMap[heightMapZ + 1][heightMapX + 1] : height1;

        return Mathf.Lerp(Mathf.Lerp(height1, height2, xDiff), Mathf.Lerp(height3, height4, xDiff), zDiff);
    }

    public string GetBlockFromHeightAndSlope(float normalizedHeight, float slope, BlockPalette blockPalette)
    {
        slope = Mathf.Clamp01(slope);
        normalizedHeight = Mathf.Clamp01(normalizedHeight);

        foreach (BlockConstraint constraint in blockPalette.blockConstraints)
        {
            if (normalizedHeight >= constraint.heightRange.x && normalizedHeight <= constraint.heightRange.y &&
                slope >= constraint.slopeRange.x && slope <= constraint.slopeRange.y)
            {
                return GetWeightedRandomBlock(constraint.blockProportions);
            }
        }

        return "minecraft:air";
    }

    public string GetWeightedRandomBlock(List<BlockProportion> blockProportions)
    {
        float totalProportion = 0f;
        foreach (BlockProportion bp in blockProportions)
        {
            totalProportion += bp.proportion;
        }

        float randomValue = Random.Range(0f, totalProportion);
        float cumulative = 0f;

        foreach (BlockProportion bp in blockProportions)
        {
            cumulative += bp.proportion;
            if (randomValue <= cumulative)
            {
                return bp.blockName;
            }
        }

        return blockProportions[blockProportions.Count - 1].blockName;
    }

    public float CalculateSlope(List<List<float>> heightMap, int x, int z, Vector2Int targetSize)
    {
        Vector2Int hmSize = new Vector2Int(heightMap[0].Count, heightMap.Count);

        // Convertir (x, z) espace target → espace heightmap
        float mx = (float)x / targetSize.x * (hmSize.x - 1);
        float mz = (float)z / targetSize.y * (hmSize.y - 1);

        // Échantillonner les 4 voisins en espace heightmap
        float heightL = SampleHeightFromInterpolation(heightMap, Mathf.Max(x - 1, 0), z, targetSize);
        float heightR = SampleHeightFromInterpolation(heightMap, Mathf.Min(x + 1, targetSize.x-1), z, targetSize);
        float heightD = SampleHeightFromInterpolation(heightMap, x, Mathf.Max(z - 1, 0), targetSize);
        float heightU = SampleHeightFromInterpolation(heightMap, x, Mathf.Min(z + 1, targetSize.y-1), targetSize);

        float slopeX = Mathf.Abs(heightR - heightL);
        float slopeZ = Mathf.Abs(heightU - heightD);

        return Mathf.Sqrt(slopeX * slopeX + slopeZ * slopeZ);
    }

    public Dictionary<string, int> CreateBlockPalette(BlockPalette blockPalette)
    {
        Dictionary<string, int> paletteDict = new Dictionary<string, int>
        {
            { "minecraft:air", 0 }
        };

        for (int i = 0; i < blockPalette.blockConstraints.Count; i++)
        {
            foreach (BlockProportion bp in blockPalette.blockConstraints[i].blockProportions)
            {
                string blockName = bp.blockName;
                if (!paletteDict.ContainsKey(blockName))
                {
                    paletteDict.Add(blockName, paletteDict.Count);
                }
            }
        }

        return paletteDict;
    }
}


[System.Serializable]
public class MinecraftConverterSettings
{
    public Vector2Int size = new Vector2Int(16, 16);
    public int height = 50;

    [Space]
    public BlockPalette blockPalette;
}

[System.Serializable]
public class BlockPalette
{
    public List<BlockConstraint> blockConstraints;
}

[System.Serializable]
public class BlockConstraint
{
    public List<BlockProportion> blockProportions;
    public Vector2 slopeRange = new Vector2(0f, 1f);
    public Vector2 heightRange = new Vector2(0f, 1f);
}

[System.Serializable]
public class BlockProportion
{
    public string blockName;
    public float proportion = 1f;
}
