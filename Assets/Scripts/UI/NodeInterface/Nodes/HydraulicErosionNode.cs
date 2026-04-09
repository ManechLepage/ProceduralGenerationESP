using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Threading.Tasks;

public class HydraulicErosionNode : NodeBehaviour
{
    public HydraulicErosionAlgorithm hydraulicErosionAlgorithm;

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

        ShowLoadingIcon(true);
        await RunErosionCoroutine(heightMapCopy, settings);
        // Task.Run(() => hydraulicErosionAlgorithm.ApplyInstantErosion(heightMapCopy, settings)).Wait();
        ShowLoadingIcon(false);
        

        return new Variant(heightMapCopy);
    }

    Task RunErosionCoroutine(List<List<float>> heightMap, HydraulicErosionSettings settings)
    {
        var taskCompletionSource = new TaskCompletionSource<bool>();
        
        GraphManager.Instance.StartCoroutine(hydraulicErosionAlgorithm.ApplyErosion(heightMap, settings, (current, total) => {
            if (current >= total)
                taskCompletionSource.TrySetResult(true);
        }));
        
        return taskCompletionSource.Task;
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
}
