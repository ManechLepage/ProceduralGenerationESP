using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine.UI;
using System.Threading.Tasks;

public class FluvialErosionNode : NodeBehaviour
{
    public FluvialErosionAlgorithm fluvialErosionAlgorithm;
    private bool _running_erosion = false;

    async public override Task<Variant> OnFire()
    {
        if (!GetInputConnection("heightmap").IsConnected())
            return new Variant(new List<List<float>>());

        FluvialErosionSettings settings = await GetSettings();
        List<List<float>> heightMap = (await GetInputValue("heightmap")).GetValue<List<List<float>>>();
        int steps = (await GetInputValue("steps")).GetValue<int>();

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
        StartStopwatch();
        /*for (int step = 1; step < steps + 1; step++)
        {
            fluvialErosionAlgorithm.ApplyErosion(heightMap, settings);
        }*/
        await RunErosionCoroutine(heightMapCopy, settings, steps);

        if (IsFlagged())
        {
            PauseGeneration();
            await WaitForUnpause();
        }

        ShowLoadingIcon(false);
        _running_erosion = false;

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

    Task RunErosionCoroutine(List<List<float>> heightMap, FluvialErosionSettings settings, int steps)
    {
        var taskCompletionSource = new TaskCompletionSource<bool>();
        
        GraphManager.Instance.StartCoroutine(ApplyErosion(heightMap, settings, steps, (current, total) => {
            if (current >= total)
                taskCompletionSource.TrySetResult(true);
        }));
        
        return taskCompletionSource.Task;
    }

    public IEnumerator ApplyErosion(List<List<float>> heightMap, FluvialErosionSettings settings, int steps, Action<float, float> onProgress=null)
    {
        for (int step = 1; step < steps + 1; step++)
        {
            fluvialErosionAlgorithm.ApplyErosion(heightMap, settings);
            onProgress?.Invoke(step, steps);

            if (IsFlagged())
                GraphManager.Instance.SetNextButtonSliderValue((float)step / steps);

            if (IsFlagged())
                TerrainManager.Instance.PreviewHeightMap(heightMap);
            
            yield return null;

            while (IsGenerationPaused())
                yield return null;
        }

        yield return null;
    }

    public async Task<FluvialErosionSettings> GetSettings()
    {
        FluvialErosionSettings settings = new FluvialErosionSettings();

        settings.erosionIntensity = (await GetInputValue("intensity")).GetValue<float>();
        settings.waterQuantity = (await GetInputValue("water_quantity")).GetValue<float>();
        settings.riverThreshold = (await GetInputValue("river_threshold")).GetValue<float>();
        settings.riverIntensity = (await GetInputValue("river_intensity")).GetValue<float>();

        settings.erosionType = FluvialErosionType.MFD;

        return settings;
    }

    async public override Task<float> GetPredictedTime()
    {
        ConnectorBehaviour heightmapInput = GetInputConnection("heightmap");
        if (!heightmapInput.IsConnected())
            return 0f;

        Vector2Int terrainSize = await heightmapInput.multipleConnectedTo[0].node.GetTerrainSize();

        int steps = (await GetInputValue("steps")).GetValue<int>();

        float size = Mathf.Sqrt(terrainSize.x * terrainSize.y);
        float duration = (steps - 0.436f) * (Mathf.Pow(size, 2f) - 44.352f * size + 4049f) / 340000f;
        
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
