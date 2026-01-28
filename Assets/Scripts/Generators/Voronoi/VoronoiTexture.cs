using UnityEngine;
using System.Collections.Generic;

public class VoronoiTexture : MonoBehaviour
{
    public int numberOfPoints = 10;

    public List<List<float>> LoadVoronoiTexture(int textureWidth = 512, int textureHeight = 512)
    {
        List<List<float>> heightMap = GenerateVoronoiHeightMap(textureWidth, textureHeight, numberOfPoints);
        // Texture2D texture = GameManager.Instance.TextureHelpers.HeightMapToTexture(heightMap);
        List<List<float>> normalizedHeightMap = NormalizeHeightMap(heightMap);
        
        return normalizedHeightMap;
    }

    List<List<float>> GenerateVoronoiHeightMap(int width, int height, int numPoints)
    {
        List<List<float>> heightMap = new List<List<float>>();
        Vector2[] points = new Vector2[numPoints];


        for (int i = 0; i < numPoints; i++)
        {
            points[i] = new Vector2(Random.Range(0, width), Random.Range(0, height));
        }

        for (int y = 0; y < height; y++)
        {
            heightMap.Add(new List<float>());
            for (int x = 0; x < width; x++)
            {
                Vector2 pixel = new Vector2(x, y);
                float minDist = GetMinDistance(pixel, points);
                float intensity = Mathf.InverseLerp(0, Mathf.Sqrt(width * width + height * height), minDist);
                // Debug.Log(intensity);
                heightMap[y].Add(intensity);
            }
        }

        return heightMap;
    }

    float Distance(Vector2 a, Vector2 b)
    {
        return Vector2.Distance(a, b);
    }

    float GetMinDistance(Vector2 pixel, Vector2[] points)
    {
        float minDist = float.MaxValue;
        foreach (var point in points)
        {
            float dist = Distance(pixel, point);
            if (dist < minDist)
            {
                minDist = dist;
            }
        }
        return minDist;
    }

    List<List<float>> NormalizeHeightMap(List<List<float>> heightMap)
    {
        float minVal = float.MaxValue;
        float maxVal = float.MinValue;

        for (int y = 0; y < heightMap.Count; y++)
        {
            for (int x = 0; x < heightMap[0].Count; x++)
            {
                if (heightMap[y][x] < minVal)
                    minVal = heightMap[y][x];
                if (heightMap[y][x] > maxVal)
                    maxVal = heightMap[y][x];
            }
        }

        List<List<float>> normalizedMap = new List<List<float>>();

        for (int y = 0; y < heightMap.Count; y++)
        {
            normalizedMap.Add(new List<float>());
            for (int x = 0; x < heightMap[0].Count; x++)
            {
                float normalizedValue = (heightMap[y][x] - minVal) / (maxVal - minVal);
                normalizedMap[y].Add(normalizedValue);
            }
        }

        return normalizedMap;
    }

    Texture AddTextures(Texture texture1, Texture texture2, Vector2 position)
    {
        return texture1;
    }
}
