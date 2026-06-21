using UnityEngine;
using System.Collections.Generic;

public class MinecraftBlockStateConverter
{
    private static readonly BlockState Air = new BlockState("minecraft:air");
    private static readonly BlockState Stone = new BlockState("minecraft:stone");
    private static readonly BlockState Water = new BlockState("minecraft:water");

    public (BlockState[,,] Blocks, int MaxNonAirY) CreateChunkBlockState(List<List<float>> heightMap, MinecraftConverterSettings settings, int chunkWorldX, int chunkWorldZ)
    {
        var blocks = CreateBlockState(heightMap, settings, out int maxColumnHeight, chunkWorldX * ChunkBuilder.ChunkSize, chunkWorldZ * ChunkBuilder.ChunkSize,
            regionSizeX: ChunkBuilder.ChunkSize, regionSizeZ: ChunkBuilder.ChunkSize);
        return (blocks, maxColumnHeight);
    }

    public BlockState[,,] CreateBlockState(List<List<float>> heightMap, MinecraftConverterSettings settings, out int maxColumnHeight,
        int regionStartX = 0, int regionStartZ = 0, int regionSizeX = default, int regionSizeZ = default)
    {
        if (regionSizeX == default) { regionSizeX = settings.size.x; }
        if (regionSizeZ == default) { regionSizeZ = settings.size.y; }

        int width = regionSizeX;
        int length = regionSizeZ;
        int height = settings.height;

        int startX = regionStartX;
        int startZ = regionStartZ;

        BlockState[,,] blockState = new BlockState[width, height, length];
        maxColumnHeight = 0;

        // +2 : une bordure d'un bloc de chaque côté, échantillonnée en coordonnées mondiales,
        // pour connaître la hauteur des chunks voisins même si on ne les génère pas ici
        int[,] heightMapArray = new int[width + 2, length + 2];

        float xScaleFactor = (float)heightMap[0].Count / settings.size.x;
        float zScaleFactor = (float)heightMap.Count / settings.size.y;

        for (int x = -1; x <= width; x++)
        {
            for (int z = -1; z <= length; z++)
            {
                int sampleX = Mathf.Clamp(x + startX, 0, settings.size.x - 1);
                int sampleZ = Mathf.Clamp(z + startZ, 0, settings.size.y - 1);
                
                float normalizedHeight = SampleHeightFromInterpolation(heightMap, sampleX, sampleZ, settings.size);
                heightMapArray[x + 1, z + 1] = GetBlockHeight(normalizedHeight, height);

                maxColumnHeight = Mathf.Max(maxColumnHeight, heightMapArray[x + 1, z + 1]);
            }
        }

        for (int z = 0; z < length; z++)
        {
            for (int x = 0; x < width; x++)
            {
                float slope = CalculateSlope(heightMap, x + startX, z + startZ, settings.size) * 5.5f * (settings.size.x / 128f);
                int blockHeight = heightMapArray[x + 1, z + 1];

                int undergroundStart = -1;
                int surfaceStart = blockHeight;

                int n1Height = heightMapArray[x + 1, z];      // voisin z - 1
                int n2Height = heightMapArray[x + 1, z + 2];  // voisin z + 1
                int n3Height = heightMapArray[x, z + 1];      // voisin x - 1
                int n4Height = heightMapArray[x + 2, z + 1];  // voisin x + 1

                int minNeighborHeight = Mathf.Min(n1Height, n2Height, n3Height, n4Height);

                int groundLevel = Mathf.Min(blockHeight, minNeighborHeight);
                surfaceStart = groundLevel;

                if (settings.blockPalette.hasUnderground)
                    undergroundStart = Mathf.Min(blockHeight - 1, groundLevel - settings.blockPalette.undergroundDepth);

                for (int y = 0; y <= blockHeight; y++)
                {
                    if (settings.onlySurface && y < surfaceStart)
                    {
                        blockState[x, y, z] = Air;
                        continue;
                    }

                    if (y > undergroundStart)
                    {
                        float localHeight = y / (float)height;
                        string blockType = GetBlockFromHeightAndSlope(localHeight, slope, settings.blockPalette);
                        blockState[x, y, z] = new BlockState(blockType);
                    }
                    else
                    {
                        blockState[x, y, z] = Stone;
                    }
                }

                for (int y = blockHeight + 1; y < height; y++)
                {
                    if (y <= settings.waterLevel)
                    {
                        blockState[x, y, z] = Water;
                    }
                    else
                    {
                        blockState[x, y, z] = Air;
                    }
                }
            }
        }

        return blockState;
    }

    public int GetBlockHeight(float normalizedHeight, int maxHeight)
    {
        return Mathf.RoundToInt(normalizedHeight * (maxHeight - 1));
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

        if (heightMapZ >= heightMap.Count || heightMapX >= heightMap[0].Count || heightMapZ < 0 || heightMapX < 0)
        {
            Debug.Log("Bad heightmap sampling coordinates: " + heightMapX + ", " + heightMapZ + " (mapped from " + x + ", " + z + ")");
        }

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
            { "minecraft:air", 0 },
        };

        if (blockPalette.hasUnderground)
        {
            if (!paletteDict.ContainsKey("minecraft:stone"))
                paletteDict.Add("minecraft:stone", paletteDict.Count);
        }

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
