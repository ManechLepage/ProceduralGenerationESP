using UnityEngine;
using System.Collections.Generic;

public class WarpingNode : NodeBehaviour
{
    private WarpingAlgorithm warpingAlgorithm;

    void Awake()
    {
        warpingAlgorithm = GetComponent<WarpingAlgorithm>();
    }

    public override Variant OnFire()
    {
        int seed = GetInputValue("seed").GetValue<int>();
        float intensity = GetInputValue("intensity").GetValue<float>();
        float scale = GetInputValue("scale").GetValue<float>();
        float flowScale = GetInputValue("flow_scale").GetValue<float>();
        float noiseScale = GetInputValue("noise_scale").GetValue<float>();
        Vector2 offset = GetInputValue("offset").GetValue<Vector2>();

        Vector2Int terrainSize = GraphManager.Instance.GetTerrainSize();

        WarpingSettings settings = new WarpingSettings();
        settings.intensity = intensity;
        settings.seed = seed;
        settings.scale = scale;
        settings.flowScale = flowScale;
        settings.noiseScale = noiseScale;
        settings.offset = offset;

        List<List<Vector2>> domainMap = warpingAlgorithm.GetWarpedDomainMap(terrainSize, settings);

        return new Variant(domainMap);
    }
}
