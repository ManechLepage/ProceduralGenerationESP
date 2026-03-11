using UnityEngine;
using System.Collections.Generic;

public class VoronoiGenerator : MonoBehaviour
{
    /*
    Test de génération de voronoi et de noise
    */
    
    public VoronoiTexture voronoiTexture;
    public TextureHelpers textureHelpers;
    public NoiseGenerator noiseGenerator;
    public MeshGenerator meshGenerator;
    

    [Header("Settings")]
    public bool isEnabled = false;
    public bool multiplyLayers = false;
    public float noiseRatio = 0.2f;
    public Vector2 terrainSize = new Vector2(16f, 16f);
    public Vector2 textureSize = new Vector2(512, 512);
    public float heightMultiplier = 50f;

    public bool drawToMesh = false;
    private GameObject meshGO;


    void Start()
    {
        
        if (isEnabled)
            GenerateTexture(drawToMesh);
    }
    public void GenerateTexture(bool drawMesh)
    {
        /*
        Créer des textures de voronoi et de noise pour ensuite les combiner et les convertir en mesh.
        */
        
        List<List<float>> voronoiHeightMap = voronoiTexture.LoadVoronoiTexture((int)textureSize.x, (int)textureSize.y);
        Texture2D voronoiTex = textureHelpers.HeightMapToTexture(voronoiHeightMap);
        textureHelpers.SaveTexture(voronoiTex, "Assets/Textures/Voronoi/VoronoiBase.exr");

        List<List<float>> noiseHeightMap = noiseGenerator.GenerateDefaultNoise(textureSize);
        Texture2D noiseTex = textureHelpers.HeightMapToTexture(noiseHeightMap);
        textureHelpers.SaveTexture(noiseTex, "Assets/Textures/Voronoi/NoiseLayer.exr");

        // Combiner les deux heightmaps en fonction du ratio défini dans l'inspecteur.
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
        textureHelpers.SaveTexture(finalTexture, "Assets/Textures/Voronoi/VoronoiFinal.exr");

        if (!drawMesh)
            return;
        
        Mesh mesh = meshGenerator.TextureToMesh(finalTexture, heightMultiplier, terrainSize);
        if (meshGO == null)
            meshGO = GameManager.Instance.meshGenerator.CreateMeshObject(transform);

        // Mettre à jour le mesh dans la scène.
        meshGenerator.UpdateMesh(meshGO, mesh, terrainSize);
    }
}
