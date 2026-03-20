using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class HydraulicErosionNode : NodeBehaviour
{
    public HydraulicErosionAlgorithm hydraulicErosionAlgorithm;

    public override Variant OnFire()
    {
        if (!GetInputConnection("heightmap").IsConnected())
            return new Variant(new List<List<float>>());

        HydraulicErosionSettings settings = GetSettings();
        List<List<float>> heightMap = GetInputValue("heightmap").GetValue<List<List<float>>>();

        List<List<float>> heightMapCopy = new List<List<float>>(heightMap.Count);
        for (int i = 0; i < heightMap.Count; i++)
        {
            heightMapCopy.Add(new List<float>(heightMap[i]));
        }

        hydraulicErosionAlgorithm.ApplyInstantErosion(heightMapCopy, settings);

        return new Variant(heightMapCopy);
    }

    public HydraulicErosionSettings GetSettings()
    {
        HydraulicErosionSettings settings = new HydraulicErosionSettings();

        settings.steps = GetInputValue("steps").GetValue<int>();
        settings.waterQuantity = GetInputValue("water_quantity").GetValue<float>();
        settings.intensity = GetInputValue("intensity").GetValue<float>();
        settings.radius = GetInputValue("drop_radius").GetValue<float>();
        settings.maxStepsPerDrop = GetInputValue("max_steps_per_drop").GetValue<int>();

        return settings;
    }
}
