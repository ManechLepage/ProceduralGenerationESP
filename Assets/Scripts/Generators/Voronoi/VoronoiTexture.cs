using UnityEngine;
using System.Collections.Generic;

public class VoronoiTexture : MonoBehaviour
{
    public int numberOfPoints = 10;

    public Texture2D LoadVoronoiTexture(int textureWidth = 512, int textureHeight = 512)
    {
        List<List<float>> heightMap = GenerateVoronoiHeightMap(textureWidth, textureHeight, numberOfPoints);
        Texture2D texture = GameManager.Instance.TextureHelpers.HeightMapToTexture(heightMap);
        texture = NormalizeTexture(texture);
        
        return texture;
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

    Texture2D NormalizeTexture(Texture2D texture)
    {
        int width = texture.width;
        int height = texture.height;
        float minIntensity = float.MaxValue;
        float maxIntensity = float.MinValue;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color color = texture.GetPixel(x, y);
                float intensity = color.r;
                if (intensity < minIntensity) minIntensity = intensity;
                if (intensity > maxIntensity) maxIntensity = intensity;
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color color = texture.GetPixel(x, y);
                float intensity = color.r;
                float normalizedIntensity = (intensity - minIntensity) / (maxIntensity - minIntensity);
                // Debug.Log(normalizedIntensity);
                texture.SetPixel(x, y, new Color(normalizedIntensity, normalizedIntensity, normalizedIntensity));
            }
        }

        texture.Apply();
        return texture;    
    }

    Texture AddTextures(Texture texture1, Texture texture2, Vector2 position)
    {
        return texture1;
    }
}
