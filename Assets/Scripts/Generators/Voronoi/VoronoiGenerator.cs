using UnityEngine;
using System.Collections.Generic;

public class VoronoiGenerator : MonoBehaviour
{
    public VoronoiTexture voronoiTexture;
    public TextureHelpers textureHelpers;
    public SimpleTextureGenerator textureGenerator;
    public float noiseRatio = 0.2f;
    public Vector2 textureSize = new Vector2(512, 512);

    void Start()
    {
        GenerateTexture();
    }
    public void GenerateTexture()
    {
        Texture2D voronoiTex = voronoiTexture.LoadVoronoiTexture((int)textureSize.x, (int)textureSize.y);
        List<List<float>> voronoiHeightMap = textureHelpers.TextureToHeightMap(voronoiTex, false);
        textureHelpers.SaveTexture(voronoiTex, "Assets/Textures/Voronoi/VoronoiBase.png");

        List<List<float>> noiseHeightMap = textureGenerator.Generate(textureSize);

        List<List<float>> combinedHeightMap = textureHelpers.AddHeightMaps(voronoiHeightMap, noiseHeightMap, noiseRatio);

        Texture2D finalTexture = textureHelpers.HeightMapToTexture(combinedHeightMap);
        textureHelpers.SaveTexture(finalTexture, "Assets/Textures/Voronoi/VoronoiFinal.png");
    }
}
