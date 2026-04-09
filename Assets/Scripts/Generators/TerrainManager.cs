using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Threading.Tasks;

public class TerrainManager : MonoBehaviour
{
    [Header("General Settings")]
    public Vector2 previewSize = new Vector2(16f, 16f);
    public float terrainHeight = 50f;
    public int smoothing = 1;

    [Header("Statistics Settings")]
    public GlobalGenerationStatistics generationStatistics = new GlobalGenerationStatistics();
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
    private float initialWaterLevel;

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
        initialWaterLevel = sea != null ? sea.transform.position.y : 0f;
        ClearActualStatisticsTexts();

        masterNode = GraphManager.Instance.masterNode as MasterNode;
        if (masterNode != null)
        {
            masterNode.onFire.AddListener(GenerateVoid);
            masterNode.onInputUpdated.AddListener(MasterNodeUpdatedVoid);
        }
    }

    void Update()
    {
        if (!GameManager.Instance.openedUI && Input.GetKeyDown(KeyCode.Return))
        {
            if (masterNode != null)
            {
                _ = masterNode.Fire(onlyIfModified: false);
            }
        }
    }

    public async void GenerateVoid() => await Generate(onlyIfModified: false);

    async public Task Generate(bool onlyIfModified = false)
    {
        /*
        Générer le terrain à partir du master node, puis mettre à jour le mesh.
        */

        generationStatistics.predicted = masterNode.GetPredictedStatistics();

        if (!onlyIfModified)
        {
            ClearActualStatisticsTexts();
            generationStatistics.actual.erosionTime = 0f;
            generationStatistics.actual.terrainTime = 0f;
        }

        heightMap = (await masterNode.GetInputValue("heightmap", onlyIfModified: onlyIfModified)).GetValue<List<List<float>>>();
        terrainHeight = initialTerrainHeight * (await masterNode.GetInputValue("height", onlyIfModified: onlyIfModified)).GetValue<float>();

        if (heightMap != null && heightMap.Count > 0)
        {
            UpdateMesh();
        }

        UpdateStatisticsTexts();
    }

    public void PreviewHeightMap(List<List<float>> heightMap)
    {
        this.heightMap = heightMap;
        UpdateMesh();
    }

    public void SetActiveSea(bool active)
    {
        if (this.sea != null)
        {
            this.sea.SetActive(active);
        }
    }

    async public void MasterNodeUpdatedVoid(ConnectorBehaviour connector) => await MasterNodeUpdated(connector);

    public async Task MasterNodeUpdated(ConnectorBehaviour connector)
    {
        if (connector == null) { return; }

        if (connector.connectionName == "water")
        {
            // Ne pas recharger tout le terrain.
            bool hasWater = (await masterNode.GetInputValue("water")).GetValue<bool>();
            SetActiveSea(hasWater);
            return;
        }

        if (connector.connectionName == "water_level")
        {
            float waterLevel = (await masterNode.GetInputValue("water_level")).GetValue<float>();
            float waterGOLevel = initialWaterLevel + waterLevel * initialTerrainHeight;
            sea.transform.position = new Vector3(sea.transform.position.x, waterGOLevel, sea.transform.position.z);
            return;
        }

        ReloadPredictions();

        if ((await masterNode.GetInputValue("auto_reload")).GetValue<bool>())
        {
            await Generate(onlyIfModified: true);
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
        terrainPredictedTimeText.text = $"{generationStatistics.predicted.terrainTime * 1000f:F0}";
        erosionPredictedTimeText.text = $"{generationStatistics.predicted.erosionTime * 1000f:F0}";
        terrainActualTimeText.text = $"{generationStatistics.actual.terrainTime * 1000f:F0}";
        erosionActualTimeText.text = $"{generationStatistics.actual.erosionTime * 1000f:F0}";
    }

    public void ClearActualStatisticsTexts()
    {
        terrainActualTimeText.text = "0";
        erosionActualTimeText.text = "0";
    }

    public void ReloadPredictions()
    {
        generationStatistics.predicted = masterNode.GetPredictedStatistics();
        UpdateStatisticsTexts();
    }
}

[System.Serializable]
public class GlobalGenerationStatistics
{
    [Header("Predicted Times")]
    public GenerationStatistics predicted;

    [Header("Actual Times")]
    public GenerationStatistics actual;
}

[System.Serializable]
public class GenerationStatistics
{
    public float terrainTime;
    public float erosionTime;
}
