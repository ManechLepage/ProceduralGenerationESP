using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Threading.Tasks;

public class HydraulicErosionNode : NodeBehaviour
{
    public HydraulicErosionAlgorithm hydraulicErosionAlgorithm;
    private bool _running_erosion = false;

    async public override Task<Variant> OnFire()
    {
        if (!GetInputConnection("heightmap").IsConnected())
            return new Variant(new List<List<float>>());

        HydraulicErosionSettings settings = await GetSettings();
        List<List<float>> heightMap = (await GetInputValue("heightmap")).GetValue<List<List<float>>>();

        if (heightMap.Count == 0)
            return new Variant(heightMap);

        List<List<float>> heightMapCopy = new List<List<float>>(heightMap.Count);
        for (int i = 0; i < heightMap.Count; i++)
        {
            heightMapCopy.Add(new List<float>(heightMap[i]));
        }

        if (IsFlagged())
            TerrainManager.Instance.PreviewHeightMap(heightMapCopy);

        ShowLoadingIcon(true);
        _running_erosion = true;
        await RunErosionCoroutine(heightMapCopy, settings);
        // Task.Run(() => hydraulicErosionAlgorithm.ApplyInstantErosion(heightMapCopy, settings)).Wait();

        if (IsFlagged())
        {
            PauseGeneration();
            await WaitForUnpause();
        }

        ShowLoadingIcon(false);

        return new Variant(heightMapCopy);
    }

    void Update()
    {
        if (_running_erosion && Input.GetKeyDown(KeyCode.Space))
        {
            if (IsGenerationPaused())
                UnpauseGeneration();
            else
                PauseGeneration();
        }
    }

    Task RunErosionCoroutine(List<List<float>> heightMap, HydraulicErosionSettings settings)
    {
        var taskCompletionSource = new TaskCompletionSource<bool>();
        
        GraphManager.Instance.StartCoroutine(ApplyErosion(heightMap, settings, (current, total) => {
            if (current >= total)
                taskCompletionSource.TrySetResult(true);
        }));
        
        return taskCompletionSource.Task;
    }

    public IEnumerator ApplyErosion(List<List<float>> heightMap, HydraulicErosionSettings settings, Action<float, float> onProgress=null)
    {
        float delayBetweenUpdates = 0.015f; // Délai en secondes entre chaque mise à jour du UI
        float delayBetweenTerrainUpdates = 0.25f;
        var stopWatch = System.Diagnostics.Stopwatch.StartNew();
        float lastTime = 0f;
        float lastTerrainTime = 0f;

        for (int i = 1; i < settings.steps + 1; i++)
        {
            // Faire tomber une goutte d'eau avec une quantité d'eau diminuant au fil des inérations.
            float currentDropSize = hydraulicErosionAlgorithm.ProcessDropSize(settings.waterQuantity, i, settings.steps);
            hydraulicErosionAlgorithm.ApplyErosionStep(heightMap, currentDropSize, settings);

            // Appeler le callback.
            onProgress?.Invoke(i, settings.steps);

            if (i % 1000 == 0)
                Debug.Log($"Erosion step {i}/{settings.steps}");
            
            float t = stopWatch.ElapsedMilliseconds / 1000f;
            float delaySinceLastUpdate = t - lastTime;

            if (delaySinceLastUpdate > delayBetweenUpdates)
            {
                // Reloader le UI chaque 0.015 secondes
                lastTime = t;
                yield return null;
            }

            float terrainDelaySinceLastUpdate = t - lastTerrainTime;
            if (terrainDelaySinceLastUpdate > delayBetweenTerrainUpdates && IsFlagged())
            {
                // Mettre à jour le terrain chaque 0.25 secondes s'il y a un flag
                lastTerrainTime = t;
                TerrainManager.Instance.PreviewHeightMap(heightMap);
                yield return null;
            }

            while (IsGenerationPaused())
                yield return null;
        }

        yield return null;
    }

    public async Task<HydraulicErosionSettings> GetSettings()
    {
        HydraulicErosionSettings settings = new HydraulicErosionSettings();

        settings.steps = (await GetInputValue("steps")).GetValue<int>();
        settings.waterQuantity = (await GetInputValue("water_quantity")).GetValue<float>();
        settings.intensity = (await GetInputValue("intensity")).GetValue<float>();
        settings.radius = (await GetInputValue("drop_radius")).GetValue<float>();
        settings.maxStepsPerDrop = (await GetInputValue("max_steps_per_drop")).GetValue<int>();

        return settings;
    }

    async public override Task<float> GetPredictedTime()
    {
        ConnectorBehaviour heightmapInput = GetInputConnection("heightmap");
        if (!heightmapInput.IsConnected())
            return 0f;

        Vector2Int terrainSize = await heightmapInput.multipleConnectedTo[0].node.GetTerrainSize();

        int steps = (await GetInputValue("steps")).GetValue<int>();
        int max_steps = (await GetInputValue("max_steps_per_drop")).GetValue<int>();

        float duration = Mathf.Log(Mathf.Sqrt(terrainSize.x * terrainSize.y) / 7.15f) * max_steps * steps / 2_670_000f;
        
        return duration;
    }

    async public override Task<Vector2Int> GetTerrainSize()
    {
        ConnectorBehaviour heightmapInput = GetInputConnection("heightmap");
        if (heightmapInput.IsConnected())
            return await heightmapInput.multipleConnectedTo[0].node.GetTerrainSize();
        
        return Vector2Int.zero;
    }
}
