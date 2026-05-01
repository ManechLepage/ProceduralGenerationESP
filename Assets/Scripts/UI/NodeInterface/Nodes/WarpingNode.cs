using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

public class WarpingNode : NodeBehaviour
{
    private WarpingAlgorithm warpingAlgorithm;

    void Awake()
    {
        warpingAlgorithm = GetComponent<WarpingAlgorithm>();
    }

    async public override Task<Variant> OnFire()
    {
        int seed = (await GetInputValue("seed")).GetValue<int>();
        float intensity = (await GetInputValue("intensity")).GetValue<float>();
        float scale = (await GetInputValue("scale")).GetValue<float>();
        float flowScale = (await GetInputValue("flow_scale")).GetValue<float>();
        float noiseScale = (await GetInputValue("noise_scale")).GetValue<float>();
        Vector2 offset = (await GetInputValue("offset")).GetValue<Vector2>();

        Vector2Int terrainSize = await GraphManager.Instance.GetTerrainSize();

        WarpingSettings settings = new WarpingSettings();
        settings.intensity = intensity;
        settings.seed = seed;
        settings.scale = scale;
        settings.flowScale = flowScale;
        settings.noiseScale = noiseScale;
        settings.offset = offset;

        if (TerrainManager.Instance.enabledChunks)
        {
            settings.scale *= TerrainManager.Instance.GetCurrentChunkScale();

            terrainSize = TerrainManager.Instance.GetCurrentChunkSize();
            settings.globalOffset += TerrainManager.Instance.GetCurrentChunkOffset();
        }

        List<List<Vector2>> domainMap = warpingAlgorithm.GetWarpedDomainMap(terrainSize, settings);

        return new Variant(domainMap);
    }
}
