using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class FluvialErosionNode : NodeBehaviour
{
    public FluvialErosionAlgorithm fluvialErosionAlgorithm;

    public override Variant OnFire()
    {
        if (!GetInputConnection("heightmap").IsConnected())
            return new Variant(new List<List<float>>());

        FluvialErosionSettings settings = GetSettings();
        List<List<float>> heightMap = GetInputValue("heightmap").GetValue<List<List<float>>>();
        int steps = GetInputValue("steps").GetValue<int>();

        if (heightMap.Count == 0)
            return new Variant(heightMap);

        List<List<float>> heightMapCopy = new List<List<float>>(heightMap.Count);
        for (int i = 0; i < heightMap.Count; i++)
        {
            heightMapCopy.Add(new List<float>(heightMap[i]));
        }

        for (int step = 0; step < steps; step++)
        {
            fluvialErosionAlgorithm.ApplyErosion(heightMapCopy, settings);
        }

        return new Variant(heightMapCopy);
    }

    public FluvialErosionSettings GetSettings()
    {
        FluvialErosionSettings settings = new FluvialErosionSettings();

        settings.erosionIntensity = GetInputValue("intensity").GetValue<float>();
        settings.waterQuantity = GetInputValue("water_quantity").GetValue<float>();
        settings.riverThreshold = GetInputValue("river_threshold").GetValue<float>();
        settings.riverIntensity = GetInputValue("river_intensity").GetValue<float>();

        settings.erosionType = FluvialErosionType.MFD;

        return settings;
    }
}
