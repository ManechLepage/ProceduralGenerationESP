using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

public class RescaleHeightmapNode : NodeBehaviour
{
    async public override Task<Variant> OnFire()
    {
        if (!GetInputConnection("heightmap").IsConnected())
            return new Variant(new List<List<float>>());
        
        List<List<float>> heightmap = (await GetInputValue("heightmap")).GetValue<List<List<float>>>();

        Vector2 newSize = (await GetInputValue("size")).GetValue<Vector2>();
        Vector2Int size = new Vector2Int(Mathf.RoundToInt(newSize.x), Mathf.RoundToInt(newSize.y));
        
        if (heightmap == null || heightmap.Count == 0)
            return new Variant(new List<List<float>>());
        
        List<List<float>> rescaledHeightmap = new List<List<float>>();

        for (int y = 0; y < size.y; y++)
        {
            rescaledHeightmap.Add(new List<float>());
            for (int x = 0; x < size.x; x++)
            {
                float sampledHeight = SampleHeightFromInterpolation(heightmap, x, y, size);
                rescaledHeightmap[y].Add(sampledHeight);
            }
        }

        return new Variant(rescaledHeightmap);
    }

    public float SampleHeightFromInterpolation(List<List<float>> heightMap, float x, float y, Vector2Int targetSize)
    {
        // Même méthode que dans MinecraftConverter...

        Vector2Int heightMapSize = new Vector2Int(heightMap[0].Count, heightMap.Count);
        float mappedX = (float)x / targetSize.x * (heightMapSize.x - 1);
        float mappedY = (float)y / targetSize.y * (heightMapSize.y - 1);

        int heightMapX = Mathf.FloorToInt(mappedX);
        int heightMapY = Mathf.FloorToInt(mappedY);

        float xDiff = mappedX - heightMapX;
        float yDiff = mappedY - heightMapY;

        float height1 = heightMap[heightMapY][heightMapX];
        float height2 = (heightMapX < heightMapSize.x - 1) ? heightMap[heightMapY][heightMapX + 1] : height1;
        float height3 = (heightMapY < heightMapSize.y - 1) ? heightMap[heightMapY + 1][heightMapX] : height1;
        float height4 = (heightMapX < heightMapSize.x - 1 && heightMapY < heightMapSize.y - 1) ? heightMap[heightMapY + 1][heightMapX + 1] : height1;

        return Mathf.Lerp(Mathf.Lerp(height1, height2, xDiff), Mathf.Lerp(height3, height4, xDiff), yDiff);
    }
}
