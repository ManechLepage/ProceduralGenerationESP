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

    public FBMSettings GetSettings()
    {
        FBMSettings settings = new FBMSettings();

        settings.seed = GetInputValue("seed").GetValue<int>();
        settings.scale = GetInputValue("scale").GetValue<float>();
        settings.octaves = GetInputValue("octaves").GetValue<int>();
        settings.lacunarity = GetInputValue("lacunarity").GetValue<float>();
        settings.persistence = GetInputValue("persistence").GetValue<float>();
        settings.offset = GetInputValue("offset").GetValue<Vector2>();

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
