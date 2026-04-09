using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Threading.Tasks;

public class FBMNode : NodeBehaviour
{
    [Header("Preview Settings")]
    public Vector2Int previewSize = new Vector2Int(64, 64);

    [Space]
    public FBMAlgorithm fbmAlgorithm;
    public PreviewBehaviour preview;

    async public override void Start()
    {
        base.Start();

        await UpdatePreview();
    }

    async public override Task<Variant> OnFire()
    {
        FBMSettings settings = await GetSettings();
        List<List<Vector2>> domainMap = await GetDomainMap();
        Vector2Int terrainSize = await GraphManager.Instance.GetTerrainSize();

        List<List<float>> heightMap = fbmAlgorithm.GetHeightMapThreading(terrainSize, settings, domainMap);

        UpdatePreviewWithHeightMap(heightMap);

        Variant output = new Variant(heightMap);

        return output;
    }

    async public override void InputUpdated(ConnectorBehaviour connector)
    {
        base.InputUpdated(connector);

        await UpdatePreview();
    }

    public async Task UpdatePreview()
    {
        FBMSettings settings = await GetSettings();
        List<List<Vector2>> domainMap = await GetDomainMap();

        // Make the offset consistent with the preview size and terrain size
        Vector2Int targetSize = await GraphManager.Instance.GetTerrainSize();
        settings.offset = new Vector2(
            settings.offset.x * previewSize.x / targetSize.x,
            settings.offset.y * previewSize.y / targetSize.y
        );

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

    public async Task<FBMSettings> GetSettings()
    {
        FBMSettings settings = new FBMSettings();

        settings.seed = (await GetInputValue("seed")).GetValue<int>();
        settings.scale = (await GetInputValue("scale")).GetValue<float>();
        settings.octaves = (await GetInputValue("octaves")).GetValue<int>();
        settings.lacunarity = (await GetInputValue("lacunarity")).GetValue<float>();
        settings.persistence = (await GetInputValue("persistence")).GetValue<float>();
        settings.offset = (await GetInputValue("offset")).GetValue<Vector2>();
        
        bool ridged = (await GetInputValue("ridged")).GetValue<bool>();
        settings.absolute = ridged;
        settings.inverted = ridged;

        settings.curve = await GetAnimationCurve();

        return settings;
    }

    public async Task<List<List<Vector2>>> GetDomainMap()
    {
        if (GetInputConnection("domainmap").IsConnected())
            return (await GetInputValue("domainmap")).GetValue<List<List<Vector2>>>();
        else
            return new List<List<Vector2>>();
    }

    public async Task<AnimationCurve> GetAnimationCurve()
    {
        if (GetInputConnection("curve").IsConnected())
            return (await GetInputValue("curve")).GetValue<AnimationCurve>();
        else
            return AnimationCurve.Linear(0, 0, 1, 1);
    }

    public override float GetPredictedTime()
    {
        return 1f;
    }
}
