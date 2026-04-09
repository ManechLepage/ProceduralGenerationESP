using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine.UI;
using System.Threading.Tasks;

public class FluvialErosionNode : NodeBehaviour
{
    public FluvialErosionAlgorithm fluvialErosionAlgorithm;

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

        ShowLoadingIcon(true);
        /*for (int step = 1; step < steps + 1; step++)
        {
            fluvialErosionAlgorithm.ApplyErosion(heightMap, settings);
        }*/
        await RunErosionCoroutine(heightMapCopy, settings, steps);
        ShowLoadingIcon(false);

        return new Variant(heightMapCopy);
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
}
