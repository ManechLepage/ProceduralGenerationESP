using UnityEngine;
using System.Collections.Generic;

public class VoronoiGenerator : MonoBehaviour
{
    public VoronoiTexture voronoiTexture;
    public TextureHelpers textureHelpers;
    public NoiseGenerator noiseGenerator;
    public SimpleMeshGenerator meshGenerator;

    [Header("Settings")]
    public bool multiplyLayers = false;
    public float noiseRatio = 0.2f;
    public Vector2 TerrainSize = new Vector2(16f, 16f);
    public Vector2 textureSize = new Vector2(512, 512);
    public float heightMultiplier = 50f;

    public bool drawToMesh = false;

    void Start()
    {
        GenerateTexture(drawToMesh);
    }
    public void GenerateTexture(bool drawMesh)
    {
        List<List<float>> voronoiHeightMap = voronoiTexture.LoadVoronoiTexture((int)textureSize.x, (int)textureSize.y);
        Texture2D voronoiTex = textureHelpers.HeightMapToTexture(voronoiHeightMap);
        textureHelpers.SaveTexture(voronoiTex, "Assets/Textures/Voronoi/VoronoiBase.png");

        List<List<float>> noiseHeightMap = noiseGenerator.GenerateDefaultNoise(textureSize);
        Texture2D noiseTex = textureHelpers.HeightMapToTexture(noiseHeightMap);
        textureHelpers.SaveTexture(noiseTex, "Assets/Textures/Voronoi/NoiseLayer.png");

        List<List<float>> combinedHeightMap;
        if (multiplyLayers)
        {
            combinedHeightMap = textureHelpers.MultiplyHeightMaps(voronoiHeightMap, noiseHeightMap);
        }
        else
        {
            combinedHeightMap = textureHelpers.AddHeightMaps(voronoiHeightMap, noiseHeightMap, noiseRatio);
        }

        Texture2D finalTexture = textureHelpers.HeightMapToTexture(combinedHeightMap);
        textureHelpers.SaveTexture(finalTexture, "Assets/Textures/Voronoi/VoronoiFinal.png");

        if (!drawMesh)
            return;
        
        Mesh mesh = meshGenerator.HeightMapToMesh(combinedHeightMap, heightMultiplier, TerrainSize);
        meshGenerator.ShowMesh(mesh);
    }
}
