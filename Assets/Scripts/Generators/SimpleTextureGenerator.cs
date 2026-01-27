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
                float sample = Mathf.PerlinNoise(x, y);
                //sample *= heightCurve.Evaluate(sample);
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
        Mesh mesh = meshGenerator.TextureToMesh(texture, 50f, size);
        meshGenerator.ShowMesh(mesh);
    }
}
