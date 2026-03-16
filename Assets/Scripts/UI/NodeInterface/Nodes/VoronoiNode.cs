using UnityEngine;
using System.Collections.Generic;

public class VoronoiNode : NodeBehaviour
{
    public VoronoiAlgorithm voronoiAlgorithm;

    public override Variant Fire()
    {
        float scale = GetInputValue("scale").GetValue<float>();
        float variation = GetInputValue("variation").GetValue<float>();
        int seed = GetInputValue("seed").GetValue<int>();

        Vector2 neighborhoodSize_ = GetInputValue("neighborhood_size").GetValue<Vector2>();
        Vector2Int neighborhoodSize = new Vector2Int(Mathf.RoundToInt(neighborhoodSize_.x), Mathf.RoundToInt(neighborhoodSize_.y));

        Vector2 offset = GetInputValue("offset").GetValue<Vector2>();

        Vector2Int terrainSize = GraphManager.Instance.GetTerrainSize();

        VoronoiSettings settings = new VoronoiSettings();
        settings.seed = seed;
        settings.scale = scale;
        settings.variation = variation;
        settings.neighborhoodSize = neighborhoodSize;
        settings.offset = offset;

        List<List<float>> heightMap = voronoiAlgorithm.GetHeightMapThreading(terrainSize, settings);

        return new Variant(heightMap);
    }
}
