using UnityEngine;
using System.Collections.Generic;

public class SimpleTextureGenerator : MonoBehaviour
{
    public Vector2 textureSize = new Vector2(256, 256);
    public int seed = 0;
    public float scale = 1f;
    public AnimationCurve heightCurve;

    [Space]
    public Vector2 previewSize = new Vector2(16, 16);

    [Space]
    public SimpleMeshGenerator meshGenerator;

    void Start()
    {
        // Regenerate();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Regenerate();
        }
    }

    void Regenerate()
    {
        seed = Random.Range(0, 10000000);

        List<List<float>> heightMap = Generate(textureSize);
        Texture2D texture = GameManager.Instance.TextureHelpers.HeightMapToTexture(heightMap);
        GameManager.Instance.TextureHelpers.SaveTexture(texture, "Assets/Textures/GeneratedTexture.png");

        ShowHeightMap(heightMap, previewSize);
    }

    public float SignedPerlinNoise(float x, float y)
    {
        return Mathf.PerlinNoise(x, y) * 2f - 1f;
    }

    public List<List<float>> Generate(Vector2 size=default)
    {
        if (size == default) size = textureSize;
        List<List<float>> heightMap = new List<List<float>>();

        for (int x=0; x<size.x; x++)
        {
            heightMap.Add(new List<float>());
            for (int y=0; y<size.y; y++)
            {
                float xCoord = (float)(x + seed) / size.x / scale;
                float yCoord = (float)(y + seed) / size.y / scale;

                float sampleX = Mathf.PerlinNoise(xCoord / 2f, yCoord / 2f);
                float sampleY = Mathf.PerlinNoise(xCoord / 1.5f, yCoord / 1.5f);
                yCoord += (sampleY - 0.5f) * 0.01f;
                xCoord += (sampleX - 0.5f) * 0.01f;


                float sample = Mathf.PerlinNoise(xCoord, yCoord);
                float sample2 = Mathf.PerlinNoise(xCoord / 3f, yCoord / 3f);
                float sample3 = Mathf.PerlinNoise(xCoord * 10f, yCoord * 10f);

                sample *= heightCurve.Evaluate(sample);
                float slope = GetCurveSlope(heightCurve, sample);

                sample = sample * 0.7f + sample2 * 0.275f + sample3 * Mathf.Min(0.025f * slope, 0.025f);
                heightMap[x].Add(sample);
            }
        }

        return heightMap;
    }

    public void ShowTexture(Texture2D texture, Vector2 size)
    {
        Mesh mesh = meshGenerator.TextureToMesh(texture, 50f * (scale / 0.1f), size);
        meshGenerator.ShowMesh(mesh);
    }

    public void ShowHeightMap(List<List<float>> heightMap, Vector2 size)
    {
        Mesh mesh = meshGenerator.HeightMapToMesh(heightMap, 50f * (scale / 0.1f), size);
        meshGenerator.ShowMesh(mesh);
    }

    public float GetCurveSlope(AnimationCurve curve, float value, float delta=0.01f)
    {
        float value1 = curve.Evaluate(value);
        float value2 = curve.Evaluate(value + delta);
        return (value2 - value1) / delta;
    }
}
