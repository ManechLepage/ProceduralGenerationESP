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
        Regenerate();
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
        Texture2D texture = Generate(textureSize);
        Show(texture, previewSize);
    }

    public Texture2D Generate(Vector2 size=default)
    {
        if (size == default) size = textureSize;
        Texture2D texture = new Texture2D((int)size.x, (int)size.y);

        for (int x=0; x<texture.width; x++)
        {
            for (int y=0; y<texture.height; y++)
            {
                float xCoord = (float)(x + seed) / texture.width / scale;
                float yCoord = (float)(y + seed) / texture.height / scale;

                float sampleX = Mathf.PerlinNoise(xCoord / 2f, yCoord / 2f);
                float sampleY = Mathf.PerlinNoise(xCoord / 1.5f, yCoord / 1.5f);
                yCoord += (sampleY - 0.5f) * 0.01f;
                xCoord += (sampleX - 0.5f) * 0.01f;


                float sample = Mathf.PerlinNoise(xCoord, yCoord);
                float sample2 = Mathf.PerlinNoise(xCoord / 3f, yCoord / 3f);
                float sample3 = Mathf.PerlinNoise(xCoord * 10f, yCoord * 10f);

                sample *= heightCurve.Evaluate(sample);
                float slope = GetCurveSlope(heightCurve, sample);

                sample = sample * 0.7f + sample2 * 0.25f + sample3 * Mathf.Min(0.05f * slope, 0.05f);
                texture.SetPixel(x, y, new Color(sample, sample, sample));
            }
        }
        texture.Apply();

        // save texture in Assets/Textures/GeneratedTexture.png

        System.IO.File.WriteAllBytes("Assets/Textures/GeneratedTexture.png", texture.EncodeToPNG());
        UnityEditor.AssetDatabase.Refresh();

        return texture;
    }

    public void Show(Texture2D texture, Vector2 size)
    {
        Mesh mesh = meshGenerator.TextureToMesh(texture, 50f * (scale / 0.1f), size);
        meshGenerator.ShowMesh(mesh);
    }

    public float GetCurveSlope(AnimationCurve curve, float value, float delta=0.01f)
    {
        float value1 = curve.Evaluate(value);
        float value2 = curve.Evaluate(value + delta);
        return (value2 - value1) / delta;
    }
}
