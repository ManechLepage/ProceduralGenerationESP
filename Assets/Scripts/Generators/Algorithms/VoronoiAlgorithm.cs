using UnityEngine;
using System.Collections.Generic;
using System;

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

        for (int i = -(evenX ? halfSizeX : halfSizeY - 1); i <= halfSizeX; i++)
        {
            for (int j = -(evenY ? halfSizeY : halfSizeX - 1); j <= halfSizeY; j++)
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

        float maxDistance = Mathf.Sqrt(2) + Mathf.Sqrt(settings.variation) / 2;
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
        // Cantor pairing function
        int newSeed = (corner.x + corner.y) * (corner.x + corner.y + 1) / 2 + corner.y;
        newSeed += seed;

        System.Random random = new System.Random(newSeed);
        float offsetX = (float)random.NextDouble() * variation;
        float offsetY = (float)random.NextDouble() * variation;
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
