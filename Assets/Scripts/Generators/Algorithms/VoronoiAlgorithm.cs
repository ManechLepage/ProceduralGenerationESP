using UnityEngine;
using System.Collections.Generic;
using System;
using System.Threading;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;

public enum DistanceType
{
    Euclidean,
    Manhattan
}

public class VoronoiAlgorithm : MonoBehaviour
{
    private VoronoiSettings baseSettings;

    void Awake()
    {
        baseSettings = new VoronoiSettings();
    }

    public float GetValue(float x, float y, VoronoiSettings settings = null)
    {
        settings = settings ?? baseSettings;

        float scaledX = x * settings.scale;
        float scaledY = y * settings.scale;

        Vector2 scaledPoint = new Vector2(scaledX, scaledY);

        int gridX = Mathf.FloorToInt(scaledX);
        int gridY = Mathf.FloorToInt(scaledY);

        bool evenX = settings.neighborhoodSize.x % 2 == 0;
        bool evenY = settings.neighborhoodSize.y % 2 == 0;
        int halfSizeX = evenX ? settings.neighborhoodSize.x / 2 : (settings.neighborhoodSize.x + 1) / 2;
        int halfSizeY = evenY ? settings.neighborhoodSize.y / 2 : (settings.neighborhoodSize.y + 1) / 2;

        float closestDistance = float.MaxValue;

        for (int i = -(evenX ? halfSizeX : halfSizeX - 1); i <= halfSizeX; i++)
        {
            for (int j = -(evenY ? halfSizeY : halfSizeY - 1); j <= halfSizeY; j++)
            {
                Vector2 corner = GetModifiedCorner(new Vector2Int(gridX + i, gridY + j), settings.variation, settings.seed);

                float distance = 0f;
            
                switch (settings.distanceType)
                {
                    case DistanceType.Euclidean:
                        distance = GetFastEucleideanDistance(scaledPoint, corner);
                        break;
                    case DistanceType.Manhattan:
                        distance = GetManhattanDistance(scaledPoint, corner);
                        break;
                }

                if (distance < closestDistance)
                    closestDistance = distance;
            }
        }

        float maxDistance = Mathf.Pow(Mathf.Sqrt(2) + settings.variation, 2);
        closestDistance = closestDistance / maxDistance;

        return settings.inverted ? 1 - closestDistance : closestDistance;
    }

    public List<List<float>> GetHeightMap(Vector2 size, VoronoiSettings settings = null)
    {
        settings = settings ?? baseSettings;

        List<List<float>> heightMap = new List<List<float>>();

        for (int x = 0; x < size.x; x++)
        {
            heightMap.Add(new List<float>());
            for (int y = 0; y < size.y; y++)
            {
                float xCoord = (float)(x + settings.offset.x) / size.x;
                float yCoord = (float)(y + settings.offset.y) / size.y;

                float value = GetValue(xCoord, yCoord, settings);
                heightMap[heightMap.Count - 1].Add(value);
            }
        }
        
        return heightMap;
    }

    public List<List<float>> GetHeightMapThreading(Vector2 size, VoronoiSettings settings = null)
    {
        settings = settings ?? baseSettings;

        int width = (int)size.x;
        int height = (int)size.y;
        int totalCells = width * height;

        NativeArray<float> results = new NativeArray<float>(totalCells, Allocator.TempJob);

        /*NativeArray<float2> sites = new NativeArray<float2>(width*height, Allocator.TempJob);
        int2 realSitesSize = new int2((int)math.ceil(settings.scale), (int)math.ceil(settings.scale));
        int2 realSitesOffset = new int2((int)math.floor(settings.offset.x / width * settings.scale), (int)math.floor(settings.offset.y / height * settings.scale));
        
        bool evenX = settings.neighborhoodSize.x % 2 == 0;
        bool evenY = settings.neighborhoodSize.y % 2 == 0;
        int halfSizeX = evenX ? settings.neighborhoodSize.x / 2 : (settings.neighborhoodSize.x + 1) / 2;
        int halfSizeY = evenY ? settings.neighborhoodSize.y / 2 : (settings.neighborhoodSize.y + 1) / 2;

        int2 additionalStartSize = new int2(
            evenX ? halfSizeX : halfSizeX - 1,
            evenY ? halfSizeY : halfSizeY - 1
        );
        int2 additionalEndSize = new int2(
            evenX ? halfSizeX : halfSizeX - 1,
            evenY ? halfSizeY : halfSizeY - 1
        );

        for (int i = 0; i < realSitesSize.x + additionalStartSize.x + additionalEndSize.x; i++)
        {
            for (int j = 0; j < realSitesSize.y + additionalStartSize.y + additionalEndSize.y; j++)
            {
                int index = j * width + i;
                int2 sitePos = new int2(i - additionalStartSize.x, j - additionalStartSize.y) + realSitesOffset;

                uint h = math.hash(sitePos) + (uint)settings.seed;
                if (h == 0) h = 1;

                float jitterX = (HashToFloat(h) - 0.5f) * settings.variation;
                float jitterY = (HashToFloat(h ^ 0x9E3779B9u) - 0.5f) * settings.variation;

                sites[index] = new float2(sitePos.x + jitterX, sitePos.y + jitterY);
            }
        }*/
        
        /*for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;

                uint h = math.hash(new int2(x, y)) + (uint)settings.seed;
                if (h == 0) h = 1;

                float jitterX = (HashToFloat(h) - 0.5f) * settings.variation;
                float jitterY = (HashToFloat(h ^ 0x9E3779B9u) - 0.5f) * settings.variation;

                sites[index] = new float2(x + jitterX, y + jitterY);
            }
        }*/

        CalculateHeightJob job = new CalculateHeightJob
        {
            //sites = sites,
            //sitesOffset = new int2(additionalStartSize.x, additionalStartSize.y),
            //pixelToSiteFactor = new float2(settings.scale / width, settings.scale / height),
            width = width,
            height = height,
            seed = settings.seed,
            scale = settings.scale,
            offset = (float2)settings.offset,
            //chunkOffset = new float2(settings.offset.x / size.x, settings.offset.y / size.y),
            variation = settings.variation,
            distanceType = settings.distanceType,
            neighborhoodSize = new int2(settings.neighborhoodSize.x, settings.neighborhoodSize.y),
            inverted = settings.inverted,
            maxDistance = Mathf.Sqrt(Mathf.Sqrt(2) + settings.variation),
            results = results
        };

        JobHandle handle = job.Schedule(totalCells, 256);
        handle.Complete();

        List<List<float>> heightMap = CombineResults(results, size);

        results.Dispose();
        //sites.Dispose();

        return heightMap;
    }

    static float HashToFloat(uint x)
    {
        x ^= x >> 16;
        x *= 0x7feb352d;
        x ^= x >> 15;
        x *= 0x846ca68b;
        x ^= x >> 16;
        return (x & 0x00FFFFFF) / 16777216f;
    }

    [BurstCompile]
    struct CalculateHeightJob : IJobParallelFor
    {
        //[ReadOnly] public NativeArray<float2> sites;
        //[ReadOnly] public int2 sitesOffset;
        //[ReadOnly] public float2 pixelToSiteFactor;
        [ReadOnly] public int width;
        [ReadOnly] public int height;
        [ReadOnly] public int seed;
        [ReadOnly] public float scale;
        [ReadOnly] public float2 offset;
        //[ReadOnly] public float2 chunkOffset;
        [ReadOnly] public float variation;
        [ReadOnly] public DistanceType distanceType;
        [ReadOnly] public int2 neighborhoodSize;
        [ReadOnly] public bool inverted;
        [ReadOnly] public float maxDistance;

        [WriteOnly] public NativeArray<float> results;

        public void Execute(int index)
        {
            int x = index % width;
            int y = index / width;

            float xCoord = (float)(x + offset.x) / width;
            float yCoord = (float)(y + offset.y) / height;

            results[index] = GetValueJob(xCoord, yCoord, x, y);
        }

        float GetValueJob(float x, float y, int pixelX, int pixelY)
        {
            float scaledX = x * scale;
            float scaledY = y * scale;

            float2 scaledPoint = new float2(scaledX, scaledY);

            int gridX = Mathf.FloorToInt(scaledX);
            int gridY = Mathf.FloorToInt(scaledY);

            bool evenX = neighborhoodSize.x % 2 == 0;
            bool evenY = neighborhoodSize.y % 2 == 0;
            int halfSizeX = evenX ? neighborhoodSize.x / 2 : (neighborhoodSize.x + 1) / 2;
            int halfSizeY = evenY ? neighborhoodSize.y / 2 : (neighborhoodSize.y + 1) / 2;

            float closestDistance = float.MaxValue;

            for (int i = -(evenX ? halfSizeX : halfSizeX - 1); i <= halfSizeX; i++)
            {
                for (int j = -(evenY ? halfSizeY : halfSizeY - 1); j <= halfSizeY; j++)
                {
                    float2 corner = GetModifiedCornerJob(new float2(gridX + i, gridY + j));
                    /*int nx = gridX + i;
                    int ny = gridY + j;
                    if (nx < 0 || ny < 0 || nx >= width || ny >= height)
                        continue;
                    int nIndex = ny * width + nx;

                    float2 corner = (sites[nIndex] - offset) / new float2(width, height) * scale;
                    //Debug.Log($"Corner: {corner}, ScaledPoint: {scaledPoint.x}, {scaledPoint.y}");
                    */

                    /*int2 sitePos = new int2(i + (int)math.floor(pixelX * pixelToSiteFactor.x), (int)math.floor(pixelY * pixelToSiteFactor.y)) + sitesOffset;
                    int siteIndex = (sitePos.y * width) + sitePos.x;
                    float2 corner = sites[siteIndex];*/

                    float distance = 0f;
                
                    switch (distanceType)
                    {
                        case DistanceType.Euclidean:
                            distance = GetFastEucleideanDistanceJob(scaledPoint, corner);
                            break;
                        case DistanceType.Manhattan:
                            distance = GetManhattanDistanceJob(scaledPoint, corner);
                            break;
                    }

                    if (distance < closestDistance)
                        closestDistance = distance;
                }
            }

            closestDistance = closestDistance / maxDistance;

            return inverted ? 1 - closestDistance : closestDistance;
        }

        float2 GetModifiedCornerJob(float2 corner)
        {
            uint s = math.hash(new int2((int)corner.x, (int)corner.y)) + (uint)seed;
            if (s == 0) s = 1u;

            float offsetX = (HashToFloat(s) - 0.5f) * variation;
            float offsetY = (HashToFloat(s ^ 0x9E3779B9u) - 0.5f) * variation;
            return new float2(corner.x + offsetX, corner.y + offsetY);
        }

        float GetFastEucleideanDistanceJob(float2 a, float2 b)
        {
            float dx = a.x - b.x;
            float dy = a.y - b.y;
            return dx * dx + dy * dy;
        }

        float GetManhattanDistanceJob(float2 a, float2 b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        static float HashToFloat(uint x)
        {
            x ^= x >> 16;
            x *= 0x7feb352d;
            x ^= x >> 15;
            x *= 0x846ca68b;
            x ^= x >> 16;
            return (x & 0x00FFFFFF) / 16777216f;
        }
    }

    List<List<float>> CombineResults(NativeArray<float> results, Vector2 size)
    {
        List<List<float>> heightMap = new List<List<float>>();

        for (int x = 0; x < size.x; x++)
        {
            heightMap.Add(new List<float>());
            for (int y = 0; y < size.y; y++)
            {
                int index = y * (int)size.x + x;
                heightMap[x].Add(results[index]);
            }
        }

        return heightMap;
    }

    public float GetFastEucleideanDistance(Vector2 a, Vector2 b)
    {
        float dx = a.x - b.x;
        float dy = a.y - b.y;
        return dx * dx + dy * dy;
    }

    public float GetManhattanDistance(Vector2 a, Vector2 b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    public Vector2 GetModifiedCorner(Vector2Int corner, float variation, int seed)
    {
        // Unity Mathematics Random is faster outside of jobs.
        uint s = math.hash(new int2(corner.x, corner.y)) + (uint)seed;
        if (s == 0) s = 1u;

        Unity.Mathematics.Random random = new Unity.Mathematics.Random(s);
        float offsetX = (random.NextFloat() - 0.5f) * variation;
        float offsetY = (random.NextFloat() - 0.5f) * variation;
        return new Vector2(corner.x + offsetX, corner.y + offsetY);
    }
}


[System.Serializable]
public class VoronoiSettings
{
    public int seed = 0;

    [Space]
    public float scale = 1;
    public Vector2 offset = Vector2.zero;
    public float variation = 0.75f;
    public DistanceType distanceType = DistanceType.Euclidean;
    public Vector2Int neighborhoodSize = new Vector2Int(3, 3);

    [Space]
    public bool inverted = false;

    public VoronoiSettings GetCopy()
    {
        return new VoronoiSettings
        {
            seed = this.seed,
            scale = this.scale,
            offset = this.offset,
            variation = this.variation,
            distanceType = this.distanceType,
            neighborhoodSize = this.neighborhoodSize,
            inverted = this.inverted
        };
    }

    public bool SameSettings(VoronoiSettings other)
    {
        return 
            this.seed == other.seed &&
            this.scale == other.scale &&
            this.offset == other.offset &&
            this.variation == other.variation &&
            this.distanceType == other.distanceType &&
            this.neighborhoodSize == other.neighborhoodSize &&
            this.inverted == other.inverted;
    }
}
