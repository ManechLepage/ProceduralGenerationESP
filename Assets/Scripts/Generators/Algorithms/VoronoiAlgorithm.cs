using UnityEngine;
using System.Collections.Generic;
using System;

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

        int gridX = Mathf.FloorToInt(scaledX);
        int gridY = Mathf.FloorToInt(scaledY);

        List<Vector2> corners = new List<Vector2>()
        {
            GetModifiedCorner(new Vector2Int(gridX, gridY), settings.variation, settings.seed),
            GetModifiedCorner(new Vector2Int(gridX + 1, gridY), settings.variation, settings.seed),
            GetModifiedCorner(new Vector2Int(gridX, gridY + 1), settings.variation, settings.seed),
            GetModifiedCorner(new Vector2Int(gridX + 1, gridY + 1), settings.variation, settings.seed)
        };

        float closestDistance = GetClosestDistance(corners, new Vector2(scaledX, scaledY));

        float maxDistance = Mathf.Sqrt(2) * settings.variation;
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

    public float GetClosestDistance(List<Vector2> corners, Vector2 point)
    {
        float closestDistance = float.MaxValue;
        foreach (Vector2 corner in corners)
        {
            float distance = Vector2.Distance(point, corner);
            if (distance < closestDistance)
                closestDistance = distance;
        }
        return closestDistance;
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
    public float variation = 0.5f;

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
            this.inverted == other.inverted;
    }
}
