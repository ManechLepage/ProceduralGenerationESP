using UnityEngine;
using System.Collections.Generic;

public class ErosionTesting : MonoBehaviour
{
    public bool isEnabled = true;

    [Header("Graphic Settings")]
    public Vector2Int textureSize = new Vector2Int(256, 256);
    public Vector2 previewSize = new Vector2(16f, 16f);
    public float terrainHeight = 50f;


    [Header("Algorithm Settings")]
    public FBMSettings fbmSettings;
    public ErosionSettings erosionSettings;

    [Header("Color Settings")]
    public MeshColorSettings colorSettings;

    private List<List<float>> heightMap;
    private GameObject meshGO;
    private bool didErode = false;

    void Start()
    {
        if (isEnabled)
        {
            GenerateBaseTerrain();
            UpdateMesh();
        }
    }

    void Update()
    {
        if (!isEnabled) return;

        if (Input.GetKeyDown(KeyCode.E) && !didErode)
        {
            Debug.Log("Erosion started!");
            ErodeTerrain();
            UpdateMesh();
            didErode = true;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            GenerateBaseTerrain();
            UpdateMesh();
            didErode = false;
        }
    }

    public void GenerateBaseTerrain()
    {
        heightMap = GameManager.Instance.fbmAlgorithm.GetHeightMapThreading(textureSize, fbmSettings);
    }

    public void ErodeTerrain()
    {
        GameManager.Instance.erosionAlgorithm.ErosionProcess(heightMap, erosionSettings, OnProgress);
    }

    public void OnProgress(float current, float total)
    {
        float progress = current / total;
        Debug.Log($"Erosion progress: {progress * 100f}%");
        UpdateMesh();
    }

    public void UpdateMesh()
    {
        if (meshGO == null)
            meshGO = GameManager.Instance.meshGenerator.CreateMeshObject(transform, colorSettings.isEnabled);
        
        Mesh mesh = GameManager.Instance.meshGenerator.HeightMapToMesh(heightMap, terrainHeight, previewSize, false, colorSettings);
        GameManager.Instance.meshGenerator.UpdateMesh(meshGO, mesh, previewSize);
    }
}
