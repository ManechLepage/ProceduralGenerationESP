using UnityEngine;
using System.Collections.Generic;

public class DomainWarping : MonoBehaviour
{
    public NoiseGenerator noiseGenerator;
    public SimpleMeshGenerator meshGenerator;
    public TextureHelpers textureHelpers;
    public bool isEnabled = false;

    [Header("Warping Settings")]
    public Vector2 size = new Vector2(256, 256);
    public int octaves = 4;
    public float scale = 20f;
    public float persistence = 0.5f;
    public float lacunarity = 2f;

    [Header("Render Settings")]
    public Vector2 terrainSize = new Vector2(16f, 16f);
    public float terrainHeight = 50f;

    public void Start()
    {
        if (isEnabled)
            GenerateAndShow();
    }

    public void GenerateAndShow()
    {
        List<List<float>> warpedNoiseHeightMap = GenerateWarpedNoise(size);

        Texture2D warpedTexture = textureHelpers.HeightMapToTexture(warpedNoiseHeightMap);
        textureHelpers.SaveTexture(warpedTexture, "Assets/Textures/Warping/WarpedTexture.png");

        Mesh mesh = meshGenerator.HeightMapToMesh(warpedNoiseHeightMap, terrainHeight, terrainSize, false);
        meshGenerator.ShowMesh(mesh);
    }

    public List<List<float>> GenerateWarpedNoise(Vector2 size)
    {
        List<List<float>> heightMap = new List<List<float>>();

        for (int x = 0; x < size.x; x++)
        {
            heightMap.Add(new List<float>());
            for (int y = 0; y < size.y; y++)
            {
                float xCoord = (float)x / size.x;
                float yCoord = (float)y / size.y;

                float warpedValue = GetWarpedValue(xCoord, yCoord);
                heightMap[x].Add(warpedValue);
            }
        }

        return heightMap;
    }

    public float GetWarpedValue(float x, float y)
    {
        Vector2 warp1 = new Vector2(
            GetNoiseValue(x, y),
            GetNoiseValue(x + 5.2f, y + 1.3f)
        );

        Vector2 warp2 = new Vector2(
            GetNoiseValue(x + 4f * warp1.x + 1.7f, y + 4f * warp1.y + 9.2f),
            GetNoiseValue(x + 4f * warp1.x + 8.3f, y + 4f * warp1.y + 2.8f)
        );

        return GetNoiseValue(
            x + 4f * warp2.x,
            y + 4f * warp2.y
        ) * 1.5f;
    }

    public float GetNoiseValue(float x, float y)
    {
        return noiseGenerator.GetNoiseValue(x, y, octaves, scale, persistence, lacunarity, false, null);
    }
}
