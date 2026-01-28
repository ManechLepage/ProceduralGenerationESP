using UnityEngine;

public class VoronoiTexture : MonoBehaviour
{
    public int textureWidth = 512;
    public int textureHeight = 512;
    public int numberOfPoints = 10;

    public SimpleMeshGenerator meshGenerator;

    void Start()
    {
        Texture2D texture = GenerateVoronoiTexture(textureWidth, textureHeight, numberOfPoints);
        texture = NormalizeTexture(texture);
        
        System.IO.File.WriteAllBytes(Application.dataPath + "/Textures/Voronoi/VoronoiTexture.png", texture.EncodeToPNG());
        
        // Mesh mesh = meshGenerator.TextureToMesh(texture, 50f, new Vector2(16.0f, 16.0f));
        // meshGenerator.ShowMesh(mesh);
    }
    Texture2D GenerateVoronoiTexture(int width, int height, int numPoints)
    {
        Texture2D texture = new Texture2D(width, height);
        Vector2[] points = new Vector2[numPoints];


        for (int i = 0; i < numPoints; i++)
        {
            points[i] = new Vector2(Random.Range(0, width), Random.Range(0, height));
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 pixel = new Vector2(x, y);
                float minDist = GetMinDistance(pixel, points);
                float intensity = Mathf.InverseLerp(0, Mathf.Sqrt(width * width + height * height), minDist);
                Debug.Log(intensity);
                Color color = new Color(intensity, intensity, intensity);
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return texture;
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
                Debug.Log(normalizedIntensity);
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
