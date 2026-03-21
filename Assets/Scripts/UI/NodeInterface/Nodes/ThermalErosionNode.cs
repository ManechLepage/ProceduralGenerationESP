using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class ThermalErosionNode : NodeBehaviour
{
    public ThermalErosionAlgorithm thermalErosionAlgorithm;

    public override Variant OnFire()
    {
        if (!GetInputConnection("heightmap").IsConnected())
            return new Variant(new List<List<float>>());

        ThermalErosionSettings settings = GetSettings();
        List<List<float>> heightMap = GetInputValue("heightmap").GetValue<List<List<float>>>();

        if (heightMap.Count == 0)
            return new Variant(heightMap);

        List<List<float>> heightMapCopy = new List<List<float>>(heightMap.Count);
        for (int i = 0; i < heightMap.Count; i++)
        {
            heightMapCopy.Add(new List<float>(heightMap[i]));
        }

        float pixelDistanceFactor = TerrainManager.Instance.previewSize.x / heightMapCopy.Count * 50f / TerrainManager.Instance.terrainHeight;
        thermalErosionAlgorithm.ApplyInstantErosion(heightMapCopy, settings, pixelDistanceFactor);

        return new Variant(heightMapCopy);
    }

    public ThermalErosionSettings GetSettings()
    {
        ThermalErosionSettings settings = new ThermalErosionSettings();

        settings.intensity = GetInputValue("intensity").GetValue<float>();
        settings.steps = GetInputValue("steps").GetValue<int>();
        settings.talusAngle = GetInputValue("talus_angle").GetValue<float>();
        settings.randomness = GetInputValue("randomness").GetValue<float>();
        settings.sedimentMap = GetInputValue("sediment_map").GetValue<bool>();
        settings.talusProduction = GetInputValue("talus_production").GetValue<float>();

        return settings;
    }
}
