using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class TerrainManager : MonoBehaviour
{
    [Header("General Settings")]
    public Vector2 previewSize = new Vector2(16f, 16f);
    public float terrainHeight = 50f;
    public int smoothing = 1;

    [Header("Statistics Settings")]
    public GenerationStatistics generationStatistics = new GenerationStatistics();
    public TextMeshProUGUI terrainPredictedTimeText;
    public TextMeshProUGUI erosionPredictedTimeText;
    public TextMeshProUGUI terrainActualTimeText;
    public TextMeshProUGUI erosionActualTimeText;
    
    [Header("Color Settings")]
    public MeshColorSettings colorSettings;

    [Header("Environment Settings")]
    public GameObject sea;

    private List<List<float>> heightMap;
    private GameObject meshGO;
    private MasterNode masterNode;
    private float initialTerrainHeight;

    public static TerrainManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        initialTerrainHeight = terrainHeight;
        ClearActualStatisticsTexts();

        masterNode = GraphManager.Instance.masterNode as MasterNode;
        if (masterNode != null)
        {
            masterNode.onFire.AddListener(Generate);
            masterNode.onInputUpdated.AddListener(MasterNodeUpdated);
        }
    }

    void Update()
    {
        if (!GameManager.Instance.openedUI && Input.GetKeyDown(KeyCode.Return))
        {
            if (masterNode != null)
            {
                masterNode.Fire(onlyIfModified: false);
            }
        }
    }

    public void Generate() => Generate(onlyIfModified: false);

    public void Generate(bool onlyIfModified = false)
    {
        /*
        Générer le terrain à partir du master node, puis mettre à jour le mesh.
        */

        if (!onlyIfModified)
        {
            ClearActualStatisticsTexts();
            generationStatistics.erosionActualTime = 0f;
            generationStatistics.terrainActualTime = 0f;
        }

        heightMap = masterNode.GetInputValue("heightmap", onlyIfModified: onlyIfModified).GetValue<List<List<float>>>();
        terrainHeight = initialTerrainHeight * masterNode.GetInputValue("height", onlyIfModified: onlyIfModified).GetValue<float>();

        if (heightMap != null && heightMap.Count > 0)
        {
            UpdateMesh();
        }

        if (!onlyIfModified)
        {
            UpdateStatisticsTexts();
        }
    }

    public void SetActiveSea(bool active)
    {
        if (this.sea != null)
        {
            this.sea.SetActive(active);
        }
    }

    public void MasterNodeUpdated()
    {
        if (masterNode.GetInputValue("auto_reload").GetValue<bool>())
        {
            Generate(onlyIfModified: true);
        }
    }

    public void UpdateMesh()
    {
        /*
        Régénérer le mesh à partir du heightmap et l'appliquer au GameObject. Si le GameObject n'existe pas encore, le créer.
        */

        if (meshGO == null)
            meshGO = GameManager.Instance.meshGenerator.CreateMeshObject(transform, colorSettings.isEnabled);
        
        if (smoothing > 0)
        {
            heightMap = GameManager.Instance.textureHelpers.SmoothHeightMap(heightMap, smoothing);
        }

        float pixelDistanceMultiplier = 256f / heightMap.Count;
        
        Mesh mesh = GameManager.Instance.meshGenerator.HeightMapToMesh(heightMap, terrainHeight, previewSize, false, colorSettings, lowBorders: true, pixelDistance: pixelDistanceMultiplier);
        GameManager.Instance.meshGenerator.UpdateMesh(meshGO, mesh, previewSize);
    }

    public void UpdateStatisticsTexts()
    {
        terrainPredictedTimeText.text = $"{generationStatistics.terrainpredictedTime * 1000f:F0}";
        erosionPredictedTimeText.text = $"{generationStatistics.erosionPredictedTime * 1000f:F0}";
        terrainActualTimeText.text = $"{generationStatistics.terrainActualTime * 1000f:F0}";
        erosionActualTimeText.text = $"{generationStatistics.erosionActualTime * 1000f:F0}";
    }

    public void ClearActualStatisticsTexts()
    {
        terrainActualTimeText.text = "0";
        erosionActualTimeText.text = "0";
    }
}

[System.Serializable]
public class GenerationStatistics
{
    [Header("Predicted Times")]
    public float terrainpredictedTime;
    public float erosionPredictedTime;

    [Header("Actual Times")]
    public float terrainActualTime;
    public float erosionActualTime;
}
