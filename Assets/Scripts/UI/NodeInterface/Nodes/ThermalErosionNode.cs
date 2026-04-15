using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Threading.Tasks;

public class ThermalErosionNode : NodeBehaviour
{
    public ThermalErosionAlgorithm thermalErosionAlgorithm;

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
        

        ShowLoadingIcon(true);
        // thermalErosionAlgorithm.ApplyInstantErosion(heightMapCopy, settings, pixelDistanceFactor);
        await Task.Run(() => thermalErosionAlgorithm.ApplyInstantErosion(heightMapCopy, settings, pixelDistanceFactor));
        ShowLoadingIcon(false);

        return new Variant(heightMapCopy);
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

    async public override Task<Vector2Int> GetTerrainSize()
    {
        ConnectorBehaviour heightmapInput = GetInputConnection("heightmap");
        if (heightmapInput.IsConnected())
            return await heightmapInput.multipleConnectedTo[0].node.GetTerrainSize();
        
        return Vector2Int.zero;
    }
}
