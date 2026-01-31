using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class NoiseGenerator : MonoBehaviour
{
    public TextureHelpers textureHelpers;
    [Header("Noise Settings")]
    public Vector2 textureSize = new Vector2(256, 256);
    public int octaves = 4;
    public float scale = 20f;
    public float persistence = 0.5f;
    public float lacunarity = 2f;
    public Vector2 offset = Vector2.zero;
    [Space]
    public bool absoluteNoise = false;
    public AnimationCurve heightCurve;
    [Space]
    public bool GenerateTexture = false;

    void Update()
    {
        if (GenerateTexture)
        {
            GenerateTexture = false;
            List<List<float>> heightMap = GenerateNoise(textureSize, octaves, scale, persistence, lacunarity, offset);
            Texture2D texture = textureHelpers.HeightMapToTexture(heightMap);
            textureHelpers.SaveTexture(texture, "Assets/Textures/Previews/Noise.png");
        }
    }

    public List<List<float>> GenerateDefaultNoise(Vector2 size)
    {
        return GenerateNoise(size, octaves, scale, persistence, lacunarity, offset);
    }
    public List<List<float>> GenerateNoise(Vector2 size, int octaves, float scale, float persistence, float lacunarity, Vector2 offset)
    {
        List<List<float>> heightMap = new List<List<float>>();

        for (int x = 0; x < size.x; x++)
        {
            heightMap.Add(new List<float>());
            for (int y = 0; y < size.y; y++)
            {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < octaves; i++)
                {
                    float xCoord = (float)(x + offset.x) / size.x * scale * frequency;
                    float yCoord = (float)(y + offset.y) / size.y * scale * frequency;
                    
                    float sample = Mathf.PerlinNoise(xCoord, yCoord) * 2f - 1f;

                    if (absoluteNoise)
                        sample = Mathf.Abs(sample);

                    noiseHeight += sample * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }
                
                if (!absoluteNoise)
                    noiseHeight = (noiseHeight + 1f) / 2f; // Normalize to [0,1]
                
                noiseHeight *= heightCurve.Evaluate(noiseHeight);
                heightMap[x].Add(noiseHeight);
            }
        }

        return heightMap;
    }
    public List<List<float>> GenerateSimpleNoise(Vector2 size, float scale)
    {
        List<List<float>> heightMap = new List<List<float>>();

        for (int x = 0; x < size.x; x++)
        {
            heightMap.Add(new List<float>());
            for (int y = 0; y < size.y; y++)
            {
                float xCoord = (float)x / size.x * scale;
                float yCoord = (float)y / size.y * scale;

                float sample = Mathf.PerlinNoise(xCoord, yCoord);
                heightMap[x].Add(sample);
            }
        }

        return heightMap;
    }
}
