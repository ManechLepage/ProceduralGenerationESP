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

    public override Variant OnFire()
    {
        VoronoiSettings settings = GetSettings();
        List<List<Vector2>> domainMap = GetDomainMap();
        Vector2Int terrainSize = GraphManager.Instance.GetTerrainSize();

        List<List<float>> heightMap = voronoiAlgorithm.GetHeightMapThreading(terrainSize, settings, domainMap);

        UpdatePreviewWithHeightMap(heightMap);

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

    public void UpdatePreviewWithHeightMap(List<List<float>> heightMap)
    {
        Vector2Int heightMapSize = new Vector2Int(heightMap[0].Count, heightMap.Count);

        if (heightMapSize.x == 0) return;

        List<List<float>> scaledHeightMap = new List<List<float>>();

        float scaleX = (float)heightMapSize.x / previewSize.x;
        float scaleY = (float)heightMapSize.y / previewSize.y;

        for (int x = 0; x < previewSize.x; x++)
        {
            scaledHeightMap.Add(new List<float>());
            for (int y = 0; y < previewSize.y; y++)
            {
                int sourceX = Mathf.FloorToInt(x * scaleX);
                int sourceY = Mathf.FloorToInt(y * scaleY);
                scaledHeightMap[scaledHeightMap.Count - 1].Add(heightMap[sourceX][sourceY]);
            }
        }

        preview.ApplyHeightMap(scaledHeightMap);
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
