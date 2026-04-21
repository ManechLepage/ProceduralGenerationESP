using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

public class VoronoiNode : NodeBehaviour
{
    [Header("Preview Settings")]
    public Vector2Int previewSize = new Vector2Int(64, 64);

    [Space]
    public VoronoiAlgorithm voronoiAlgorithm;
    public PreviewBehaviour preview;

    async public override void Start()
    {
        base.Start();

        await UpdatePreview();
    }

    async public override Task<Variant> OnFire()
    {
        VoronoiSettings settings = await GetSettings();
        List<List<Vector2>> domainMap = await GetDomainMap();
        Vector2Int terrainSize = await GetTerrainSize();
        AnimationCurve curve = await GetAnimationCurve();

        ShowLoadingIcon(true);

        List<List<float>> heightMap = voronoiAlgorithm.GetHeightMapThreading(terrainSize, settings, domainMap);

        if (curve != null)
            ApplyCurve(heightMap, curve);

        UpdatePreviewWithHeightMap(heightMap);

        if (IsFlagged())
        {
            TerrainManager.Instance.PreviewHeightMap(heightMap);
            PauseGeneration();
            await WaitForUnpause();
        }

        ShowLoadingIcon(false);

        return new Variant(heightMap);
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && IsFlagged())
        {
            UnpauseGeneration();
        }
    }

    async public override void InputUpdated(ConnectorBehaviour connector)
    {
        base.InputUpdated(connector);

        await UpdatePreview();
    }

    async public Task UpdatePreview()
    {
        VoronoiSettings settings = await GetSettings();
        List<List<Vector2>> domainMap = await GetDomainMap();
        AnimationCurve curve = await GetAnimationCurve();

        List<List<float>> heightMap = voronoiAlgorithm.GetHeightMapThreading(previewSize, settings, domainMap);

        if (curve != null)
            ApplyCurve(heightMap, curve);

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

    public void ApplyCurve(List<List<float>> heightMap, AnimationCurve curve)
    {
        for (int x = 0; x < heightMap.Count; x++)
        {
            for (int y = 0; y < heightMap[x].Count; y++)
            {
                heightMap[x][y] = curve.Evaluate(heightMap[x][y]);
            }
        }
    }

    async public Task<VoronoiSettings> GetSettings()
    {
        VoronoiSettings settings = new VoronoiSettings();

        settings.seed = (await GetInputValue("seed")).GetValue<int>();
        settings.scale = (await GetInputValue("scale")).GetValue<float>();
        settings.variation = (await GetInputValue("variation")).GetValue<float>();

        Vector2 neighborhoodSize_ = (await GetInputValue("neighborhood_size")).GetValue<Vector2>();
        settings.neighborhoodSize = new Vector2Int(Mathf.RoundToInt(neighborhoodSize_.x), Mathf.RoundToInt(neighborhoodSize_.y));

        settings.offset = (await GetInputValue("offset")).GetValue<Vector2>();

        return settings;
    }

    async public Task<List<List<Vector2>>> GetDomainMap()
    {
        if (GetInputConnection("domainmap").IsConnected())
            return (await GetInputValue("domainmap")).GetValue<List<List<Vector2>>>();
        else
            return new List<List<Vector2>>();
    }

    async public Task<AnimationCurve> GetAnimationCurve()
    {
        if (GetInputConnection("curve").IsConnected())
            return (await GetInputValue("curve")).GetValue<AnimationCurve>();
        else
            return null;
    }

    async public override Task<float> GetPredictedTime()
    {
        Vector2Int terrainSize = await GetTerrainSize();

        Vector2 neighborhood_size_ = (await GetInputValue("neighborhood_size")).GetValue<Vector2>();
        float neighborhood_size = (neighborhood_size_.x + neighborhood_size_.y) / 2f;

        float size = Mathf.Sqrt(terrainSize.x * terrainSize.y);
        float duration = (Mathf.Pow(size, 2f) + 66f * size - 17720f) * (Mathf.Pow(neighborhood_size, 2f) + 3.93f * neighborhood_size + 43.15f) / 1_200_000_000f;
        
        return duration;
    }

    async public override Task<Vector2Int> GetTerrainSize()
    {
        return await GraphManager.Instance.GetTerrainSize();
    }
}
