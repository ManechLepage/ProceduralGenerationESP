using UnityEngine;
using System.Collections.Generic;

public class WarpingNode : NodeBehaviour
{
    private WarpingAlgorithm warpingAlgorithm;

    void Awake()
    {
        warpingAlgorithm = GetComponent<WarpingAlgorithm>();
    }

    public override Variant Fire()
    {
        float strength = GetInputValue("strength").GetValue<float>();

        Vector2Int terrainSize = GraphManager.Instance.GetTerrainSize();

        WarpingSettings settings = new WarpingSettings();
        settings.strength = strength;

        List<List<Vector2>> domainMap = warpingAlgorithm.GetWarpedDomainMap(terrainSize, settings);

        return new Variant(domainMap);
    }
}
