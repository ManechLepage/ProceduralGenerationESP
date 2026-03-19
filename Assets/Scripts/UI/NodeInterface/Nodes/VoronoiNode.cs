using UnityEngine;
using System.Collections.Generic;

public class VoronoiNode : NodeBehaviour
{
    [Header("Preview Settings")]
    public Vector2Int previewSize = new Vector2Int(64, 64);

    [Space]
    public VoronoiAlgorithm voronoiAlgorithm;
    public PreviewBehaviour preview;

    public override void Start()
    {
        base.Start();

        UpdatePreview();
    }

    public override Variant Fire()
    {
        VoronoiSettings settings = GetSettings();
        List<List<Vector2>> domainMap = GetDomainMap();
        Vector2Int terrainSize = GraphManager.Instance.GetTerrainSize();

        List<List<float>> heightMap = voronoiAlgorithm.GetHeightMapThreading(terrainSize, settings, domainMap);

        return new Variant(heightMap);
    }

    public override void InputUpdated(ConnectorBehaviour connector)
    {
        base.InputUpdated(connector);

        UpdatePreview();
    }

    public void UpdatePreview()
    {
        VoronoiSettings settings = GetSettings();
        List<List<Vector2>> domainMap = GetDomainMap();

        List<List<float>> heightMap = voronoiAlgorithm.GetHeightMapThreading(previewSize, settings, domainMap);

        preview.ApplyHeightMap(heightMap);
    }

    public VoronoiSettings GetSettings()
    {
        VoronoiSettings settings = new VoronoiSettings();

        settings.seed = GetInputValue("seed").GetValue<int>();
        settings.scale = GetInputValue("scale").GetValue<float>();
        settings.variation = GetInputValue("variation").GetValue<float>();

        Vector2 neighborhoodSize_ = GetInputValue("neighborhood_size").GetValue<Vector2>();
        settings.neighborhoodSize = new Vector2Int(Mathf.RoundToInt(neighborhoodSize_.x), Mathf.RoundToInt(neighborhoodSize_.y));

        settings.offset = GetInputValue("offset").GetValue<Vector2>();

        return settings;
    }

    public List<List<Vector2>> GetDomainMap()
    {
        if (GetInputConnection("domainmap").IsConnected())
            return GetInputValue("domainmap").GetValue<List<List<Vector2>>>();
        else
            return new List<List<Vector2>>();
    }
}
