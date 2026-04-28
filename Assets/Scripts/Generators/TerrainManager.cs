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
    public ComputerSpeedTest computerSpeedTest = new ComputerSpeedTest();
    public GameObject statsContent;
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
    private bool _isGenerating = false;
    private bool _isRunningSpeedTest = false;

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
        LaunchSpeedTest();

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

        if (Input.GetKeyDown(KeyCode.K))
        {
            _ = ReloadPredictions();
        }
    }

    public async void GenerateVoid() => await Generate(onlyIfModified: false);

    async public Task Generate(bool onlyIfModified = false)
    {
        /*
        Générer le terrain à partir du master node, puis mettre à jour le mesh.
        */

        if (IsGenerating() || IsRunningSpeedTest())
            return;


        generationStatistics.predicted = await masterNode.GetPredictedStatistics();

        if (!onlyIfModified)
        {
            ClearActualStatisticsTexts();
            generationStatistics.actual.Reset();
        }

        _isGenerating = true;

        heightMap = (await masterNode.GetInputValue("heightmap", onlyIfModified: onlyIfModified)).GetValue<List<List<float>>>();
        terrainHeight = initialTerrainHeight * (await masterNode.GetInputValue("height", onlyIfModified: onlyIfModified)).GetValue<float>();

        if (heightMap != null && heightMap.Count > 0)
        {
            UpdateMesh();
        }

        UpdateStatisticsTexts();

        _isGenerating = false;
    }

    public bool IsGenerating()
    {
        return _isGenerating;
    }

    public bool IsRunningSpeedTest()
    {
        return _isRunningSpeedTest;
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

        await ReloadPredictions();

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
        terrainPredictedTimeText.text = $"{generationStatistics.predicted.GetTotalTime(NodeTimeType.Terrain) * 1000f:F0}";
        erosionPredictedTimeText.text = $"{generationStatistics.predicted.GetTotalTime(NodeTimeType.Erosion) * 1000f:F0}";
        terrainActualTimeText.text = $"{generationStatistics.actual.GetTotalTime(NodeTimeType.Terrain) * 1000f:F0}";
        erosionActualTimeText.text = $"{generationStatistics.actual.GetTotalTime(NodeTimeType.Erosion) * 1000f:F0}";

        statsContent.GetComponent<FixInputLayout>().Reload();
    }

    public void ClearActualStatisticsTexts()
    {
        terrainActualTimeText.text = "0";
        erosionActualTimeText.text = "0";

        statsContent.GetComponent<FixInputLayout>().Reload();
    }

    public async Task ReloadPredictions()
    {
        Debug.Log("Reloading predictions...");
        generationStatistics.predicted = await masterNode.GetPredictedStatistics();
        UpdateStatisticsTexts();
    }

    async public void LaunchSpeedTest()
    {
        _isRunningSpeedTest = true;
        ComputerSpeedTest test = new ComputerSpeedTest();
        test.loopScore = await GetLoopScore();
        test.threadScore = await GetThreadScore();
        computerSpeedTest = test;
        Debug.Log($"Loop Score: {test.loopScore} ({test.GetLoopScoreFactor():F2}x), Thread Score: {test.threadScore} ({test.GetThreadScoreFactor():F2}x)");
        _isRunningSpeedTest = false;
    }

    //on teste combien de loops on peut faire en 0,5 sec pour comparer avec la machine de Alexis

    private async Task<long> GetLoopScore()
    {
        return await Task.Run(() =>
        {
            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            float dummy = 0f;
            long iterations = 0;

            while (sw.Elapsed.TotalSeconds < 0.5)
            {
                dummy += Mathf.Sqrt(iterations) * Mathf.Sin(iterations * 0.001f);
                iterations++;
            }

            sw.Stop();
            return iterations;
        });
    }

    private async Task<long> GetThreadScore()
    {
        int threadCount = 4;
        long[] iterationsPerThread = new long[threadCount];

        List<Task> tasks = new List<Task>();
        for (int t = 0; t < threadCount; t++)
        {
            int threadIndex = t;
            tasks.Add(Task.Run(() =>
            {
                System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
                float dummy = 0f;
                long iterations = 0;

                while (sw.Elapsed.TotalSeconds < 0.5)
                {
                    dummy += Mathf.Sqrt(iterations) * Mathf.Sin(iterations * 0.001f);
                    iterations++;
                }

                iterationsPerThread[threadIndex] = iterations;
            }));
        }

        await Task.WhenAll(tasks);

        long totalIterations = 0;
        foreach (long count in iterationsPerThread)
            totalIterations += count;

        return totalIterations;
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
    public List<StatisticsTypeInfo> stats = new List<StatisticsTypeInfo>();

    public float GetTotalTime(NodeTimeType type)
    {
        float total = 0f;
        foreach (var statType in stats)
        {
            if (statType.type == type)
            {
                foreach (var stat in statType.statistics)
                    total += stat.value;
            }
        }

        return total;
    }

    public float GetTotalTime()
    {
        float total = 0f;
        foreach (var statType in stats)
        {
            foreach (var stat in statType.statistics)
                total += stat.value;
        }

        return total;
    }

    public void AddTime(NodeTimeType type, string name, float time)
    {
        StatisticsTypeInfo statType = stats.Find(s => s.type == type);
        if (statType == null)
        {
            statType = new StatisticsTypeInfo { type = type, statistics = new List<StatisticInfo>() };
            stats.Add(statType);
        }

        StatisticInfo stat = statType.statistics.Find(s => s.name == name);
        if (stat == null)
        {
            stat = new StatisticInfo { name = name, value = time };
            statType.statistics.Add(stat);
        }
        else
            stat.value += time;
    }

    public void Reset()
    {
        stats.Clear();
    }
}

[System.Serializable]
public class StatisticsTypeInfo
{
    public NodeTimeType type;
    public List<StatisticInfo> statistics;
}

[System.Serializable]
public class StatisticInfo
{
    public string name;
    public float value;
}

[System.Serializable]
public class ComputerSpeedTest
{
    public long loopScore = 1;
    public long threadScore = 1;

    private long nitroLoopScore = 5_848_999;
    private long nitroThreadScore = 20_932_065;

    public float GetLoopScoreFactor()
    {
        float startFactor = (float)nitroLoopScore / (float)loopScore;
        return 14.099f * Mathf.Pow(startFactor, 2f) - 24.018f * startFactor + 10.995f;
    }
    public float GetThreadScoreFactor()
    {
        float startFactor = (float)nitroThreadScore / (float)threadScore;
        return -0.0883f * Mathf.Pow(startFactor, 2f) + 1.6375f * startFactor;
    }

    public long GetNitroLoopScore() { return nitroLoopScore; }
    public long GetNitroThreadScore() { return nitroThreadScore; }
}
