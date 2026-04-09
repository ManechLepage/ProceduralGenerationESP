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
    private bool paused_erosion = false;

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
        _running_erosion = true;
        await RunErosionCoroutine(heightMapCopy, settings);
        // Task.Run(() => hydraulicErosionAlgorithm.ApplyInstantErosion(heightMapCopy, settings)).Wait();
        ShowLoadingIcon(false);
        

        return new Variant(heightMapCopy);
    }

    void Update()
    {
        if (_running_erosion && Input.GetKeyDown(KeyCode.Space))
        {
            paused_erosion = !paused_erosion;
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
        var stopWatch = System.Diagnostics.Stopwatch.StartNew();
        float lastTime = 0f;

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
                // Reloader le UI chaque 100 itérations
                lastTime = t;
                yield return null;
            }

            while (paused_erosion)
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
}
