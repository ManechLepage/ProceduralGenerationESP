using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class FBMNode : NodeBehaviour
{
    [Header("Preview Settings")]
    public Vector2Int previewSize = new Vector2Int(64, 64);

    [Space]
    public FBMAlgorithm fbmAlgorithm;
    public PreviewBehaviour preview;

    public override void Start()
    {
        base.Start();

        UpdatePreview();
    }

    public override Variant OnFire()
    {
        FBMSettings settings = GetSettings();
        List<List<Vector2>> domainMap = GetDomainMap();
        Vector2Int terrainSize = GraphManager.Instance.GetTerrainSize();

        List<List<float>> heightMap = fbmAlgorithm.GetHeightMapThreading(terrainSize, settings, domainMap);

        UpdatePreviewWithHeightMap(heightMap);

        Variant output = new Variant(heightMap);

        return output;
    }

    public override void InputUpdated(ConnectorBehaviour connector)
    {
        base.InputUpdated(connector);

        UpdatePreview();
    }

    public void UpdatePreview()
    {
        FBMSettings settings = GetSettings();
        List<List<Vector2>> domainMap = GetDomainMap();
        List<List<float>> heightMap = fbmAlgorithm.GetHeightMapThreading(previewSize, settings, domainMap);

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

    public FBMSettings GetSettings()
    {
        FBMSettings settings = new FBMSettings();

        settings.seed = GetInputValue("seed").GetValue<int>();
        settings.scale = GetInputValue("scale").GetValue<float>();
        settings.octaves = GetInputValue("octaves").GetValue<int>();
        settings.lacunarity = GetInputValue("lacunarity").GetValue<float>();
        settings.persistence = GetInputValue("persistence").GetValue<float>();
        settings.offset = GetInputValue("offset").GetValue<Vector2>();
        
        bool ridged = GetInputValue("ridged").GetValue<bool>();
        settings.absolute = ridged;
        settings.inverted = ridged;

        settings.curve = GetAnimationCurve();

        return settings;
    }

    public List<List<Vector2>> GetDomainMap()
    {
        if (GetInputConnection("domainmap").IsConnected())
            return GetInputValue("domainmap").GetValue<List<List<Vector2>>>();
        else
            return new List<List<Vector2>>();
    }

    public AnimationCurve GetAnimationCurve()
    {
        if (GetInputConnection("curve").IsConnected())
            return GetInputValue("curve").GetValue<AnimationCurve>();
        else
            return AnimationCurve.Linear(0, 0, 1, 1);
    }
}
