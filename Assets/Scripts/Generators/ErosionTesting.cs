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
    public AlgorithmType algorithmType = AlgorithmType.FBM;
    public bool island = false;
    public float islandScale = 1f;
    public float islandFlatness = 5f;
    public Texture2D heightMapTexture;
    public FBMSettings fbmSettings;
    public VoronoiSettings voronoiSettings;
    public HydraulicErosionSettings hydraulicErosionSettings;
    public ThermalErosionSettings thermalErosionSettings;
    public FluvialErosionSettings fluvialErosionSettings;

    [Header("Color Settings")]
    public MeshColorSettings colorSettings;

    private List<List<float>> heightMap;
    private GameObject meshGO;
    private bool didHydraulicErosion = false;
    private bool didThermalErosion = false;
    private bool didFluvialErosion = false;

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

        if (Input.GetKeyDown(KeyCode.E) && !didHydraulicErosion)
        {
            Debug.Log("Hydraulic Erosion started!");
            StartHydraulicErosion();
            UpdateMesh();
            //didHydraulicErosion = true;
        }

        if (Input.GetKeyDown(KeyCode.T) && !didThermalErosion)
        {
            Debug.Log("Thermal erosion started!");
            StartThermalErosion();
            UpdateMesh();
            //didThermalErosion = true;
        }
        
        if (Input.GetKeyDown(KeyCode.F) && !didFluvialErosion)
        {
            Debug.Log("Fluvial erosion started!");
            GameManager.Instance.fluvialErosionAlgorithm.ApplyErosion(heightMap, fluvialErosionSettings);
            UpdateMesh();
            //didFluvialErosion = true;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            GameManager.Instance.hydraulicErosionAlgorithm.StopAllCoroutines();
            GameManager.Instance.thermalErosionAlgorithm.StopAllCoroutines();
            GenerateBaseTerrain();
            UpdateMesh();
            didHydraulicErosion = false;
            didThermalErosion = false;
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("Saving terrain...");
            SaveTerrain();
        }
    }

    public void GenerateBaseTerrain()
    {
        if (algorithmType == AlgorithmType.Texture)
        {
            heightMap = GameManager.Instance.textureHelpers.TextureToHeightMap(heightMapTexture);
        }
        else
        {
            if (algorithmType == AlgorithmType.Voronoi)
            {
                heightMap = GameManager.Instance.voronoiAlgorithm.GetHeightMapThreading(textureSize, voronoiSettings);
            }
            else
            {
                heightMap = GameManager.Instance.fbmAlgorithm.GetHeightMapThreading(textureSize, fbmSettings);
            }
        }

        if (island)
        {
            TransformToIsland(heightMap);
        }
    }

    public void StartHydraulicErosion()
    {
        GameManager.Instance.hydraulicErosionAlgorithm.ErosionProcess(heightMap, hydraulicErosionSettings, OnProgress);
    }

    public void StartThermalErosion()
    {
        float pixelDistanceFactor = previewSize.x / textureSize.x * 50f / terrainHeight;
        GameManager.Instance.thermalErosionAlgorithm.ErosionProcess(heightMap, thermalErosionSettings, pixelDistanceFactor, OnProgress);
    }

    public void OnProgress(float current, float total)
    {
        float progress = current / total;
        Debug.Log($"Erosion progress: {progress * 100f}%");
        UpdateMesh();
    }

    public void TransformToIsland(List<List<float>> heightMap)
    {
        int width = heightMap.Count;
        int height = heightMap[0].Count;
        Vector2 center = new Vector2(width / 2f, height / 2f);
        float maxDistance = width / 2f;
        float intensityAtMax = 0.1f * islandScale;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float distanceToCenter = Vector2.Distance(new Vector2(x, y), center);
                float islandFactor = 1f / (1f + (1f / intensityAtMax - 1f) * Mathf.Pow(distanceToCenter / maxDistance, islandFlatness));
                heightMap[x][y] *= islandFactor;
            }
        }
    }

    public void UpdateMesh()
    {
        if (meshGO == null)
            meshGO = GameManager.Instance.meshGenerator.CreateMeshObject(transform, colorSettings.isEnabled);
        
        Mesh mesh = GameManager.Instance.meshGenerator.HeightMapToMesh(heightMap, terrainHeight, previewSize, false, colorSettings, lowBorders: true);
        GameManager.Instance.meshGenerator.UpdateMesh(meshGO, mesh, previewSize);
    }

    public void SaveTerrain()
    {
        Texture2D heightMapTexture = GameManager.Instance.textureHelpers.HeightMapToTexture(heightMap);
        GameManager.Instance.textureHelpers.SaveTexture(heightMapTexture, "Assets/Textures/Erosion/HeightMapTexture.exr", makeReadable: true);
    }
}
