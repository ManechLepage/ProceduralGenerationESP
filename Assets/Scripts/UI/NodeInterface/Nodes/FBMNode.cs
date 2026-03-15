using UnityEngine;
using System.Collections.Generic;

public class FBMNode : NodeBehaviour
{
    public FBMAlgorithm fbmAlgorithm;

    public override Variant Fire()
    {
        float scale = GetInputValue("scale").GetValue<float>();
        int octaves = GetInputValue("octaves").GetValue<int>();
        float lacunarity = GetInputValue("lacunarity").GetValue<float>();
        float persistence = GetInputValue("persistance").GetValue<float>();
        int seed = GetInputValue("seed").GetValue<int>();
        Vector2 offset = GetInputValue("offset").GetValue<Vector2>();

        Vector2Int terrainSize = GraphManager.Instance.GetTerrainSize();

        FBMSettings settings = new FBMSettings();
        settings.seed = seed;
        settings.scale = scale;
        settings.octaves = octaves;
        settings.lacunarity = lacunarity;
        settings.persistence = persistence;
        settings.offset = offset;

        List<List<float>> heightMap = fbmAlgorithm.GetHeightMapThreading(terrainSize, settings);

        return new Variant(heightMap);
    }
}
