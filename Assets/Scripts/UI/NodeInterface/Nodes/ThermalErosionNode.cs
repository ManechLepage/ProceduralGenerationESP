using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Collections;
using System;

public class ThermalErosionNode : NodeBehaviour
{
    public ThermalErosionAlgorithm thermalErosionAlgorithm;
    private bool _running_erosion = false;

    async public override Task<Variant> OnFire()
    {
        if (!GetInputConnection("heightmap").IsConnected())
            return new Variant(new List<List<float>>());

        ThermalErosionSettings settings = await GetSettings();
        List<List<float>> heightMap = (await GetInputValue("heightmap")).GetValue<List<List<float>>>();

        if (heightMap.Count == 0)
            return new Variant(heightMap);

        List<List<float>> heightMapCopy = new List<List<float>>(heightMap.Count);
        for (int i = 0; i < heightMap.Count; i++)
        {
            heightMapCopy.Add(new List<float>(heightMap[i]));
        }

        float pixelDistanceFactor = TerrainManager.Instance.previewSize.x / heightMapCopy.Count * 50f / TerrainManager.Instance.terrainHeight;
        
        if (IsFlagged())
            TerrainManager.Instance.PreviewHeightMap(heightMapCopy);

        ShowLoadingIcon(true);
        _running_erosion = true;
        StartStopwatch();
        // thermalErosionAlgorithm.ApplyInstantErosion(heightMapCopy, settings, pixelDistanceFactor);
        //await Task.Run(() => thermalErosionAlgorithm.ApplyInstantErosion(heightMapCopy, settings, pixelDistanceFactor));
        await RunErosionCoroutine(heightMapCopy, settings, pixelDistanceFactor);

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

    Task RunErosionCoroutine(List<List<float>> heightMap, ThermalErosionSettings settings, float pixelDistanceFactor)
    {
        var taskCompletionSource = new TaskCompletionSource<bool>();
        
        GraphManager.Instance.StartCoroutine(ApplyErosion(heightMap, settings, pixelDistanceFactor, (current, total) => {
            if (current >= total)
                taskCompletionSource.TrySetResult(true);
        }));
        
        return taskCompletionSource.Task;
    }

    public IEnumerator ApplyErosion(List<List<float>> heightMap, ThermalErosionSettings settings, float pixelDistanceFactor, Action<float, float> onProgress=null)
    {
        // Temporaire, copie de la fonction dans ThermalErosionAlgorithm

        List<List<float>> sedimentMap = new List<List<float>>();
        List<List<float>> bedrockMap = new List<List<float>>();

        if (settings.sedimentMap)
        {
            // Création des tableaux seulement s'ils sont nécessaires.

            for (int i = 0; i < heightMap.Count; i++)
            {
                sedimentMap.Add(new List<float>());
                for (int j = 0; j < heightMap[0].Count; j++)
                {
                    sedimentMap[i].Add(0f);
                }
            }

            for (int i = 0; i < heightMap.Count; i++)
            {
                bedrockMap.Add(new List<float>());
                for (int j = 0; j < heightMap[0].Count; j++)
                {
                    bedrockMap[i].Add(heightMap[i][j]);
                }
            }
        }

        for (int i = 1; i < settings.steps + 1; i++)
        {
            // Appliquer une étape d'érosion thermique
            thermalErosionAlgorithm.ApplyErosionStep(heightMap, bedrockMap, sedimentMap, settings, pixelDistanceFactor);

            Debug.Log($"Erosion step {i}/{settings.steps}");
            onProgress?.Invoke(i, settings.steps);

            if (IsFlagged())
                TerrainManager.Instance.PreviewHeightMap(heightMap);
            
            yield return null;

            while (IsGenerationPaused())
                yield return null;
        }
    }

    public async Task<ThermalErosionSettings> GetSettings()
    {
        ThermalErosionSettings settings = new ThermalErosionSettings();

        settings.intensity = (await GetInputValue("intensity")).GetValue<float>();
        settings.steps = (await GetInputValue("steps")).GetValue<int>();
        settings.talusAngle = (await GetInputValue("talus_angle")).GetValue<float>();
        settings.randomness = (await GetInputValue("randomness")).GetValue<float>();
        settings.sedimentMap = (await GetInputValue("sediment_map")).GetValue<bool>();
        settings.talusProduction = (await GetInputValue("talus_production")).GetValue<float>();

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
        float duration = (steps - 0.04426f) * (Mathf.Pow(size, 2f) - 8.91f * size + 302.5f) / 292500f;
        
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
