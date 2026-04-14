using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

public class MinecraftConversionTesting : MonoBehaviour
{
    [Header("Minecraft Settings")]
    public MinecraftConverterSettings minecraftSettings;
    public MinecraftConverter converter;

    [Header("Terrain Settings")]
    public Vector2Int terrainSize = new Vector2Int(16, 16);
    public Vector2 physicalSize = new Vector2(16f, 16f);
    public float heightScale = 10f;

    [Header("Generation Settings")]
    public FBMSettings fbmSettings;
    public MeshColorSettings colorSettings;

    private GameObject meshGO;
    private List<List<float>> heightMap;

    public void Start()
    {
        Regenerate();
        UpdateMesh();
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Regenerate();
            UpdateMesh();
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            SaveToSchem();
        }
    }

    public void SaveToSchem()
    {
        string path = "Assets/Textures/Minecraft/GeneratedSchematic.schem";
        converter.SaveToSchem(heightMap, path, minecraftSettings);
        UnityEditor.AssetDatabase.Refresh();
    }

    public void Regenerate()
    {
        heightMap = GameManager.Instance.fbmAlgorithm.GetHeightMapThreading(terrainSize, fbmSettings);

        Texture2D tex = GameManager.Instance.textureHelpers.HeightMapToTexture(heightMap);
        GameManager.Instance.textureHelpers.SaveTexture(tex, "Assets/Textures/Minecraft/HeightMap.exr", makeReadable: true);
    }

    public void UpdateMesh()
    {
        if (meshGO == null)
        {
            meshGO = GameManager.Instance.meshGenerator.CreateMeshObject(this.transform, colored: true);
        }

        Mesh mesh = GameManager.Instance.meshGenerator.HeightMapToMesh(heightMap, heightScale, colorSettings: colorSettings, lowBorders: true);
        GameManager.Instance.meshGenerator.UpdateMesh(meshGO, mesh, physicalSize);
    }
}
