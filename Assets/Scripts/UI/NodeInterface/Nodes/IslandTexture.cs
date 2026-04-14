using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

public class IslandTexture : NodeBehaviour
{
    [Header("Preview Settings")]
    public Vector2Int previewSize = new Vector2Int(64, 64);

    [Space]
    [SerializeField] private FBMAlgorithm fbm;
    [SerializeField] private PreviewBehaviour preview;

    async public override void Start()
    {
        base.Start();

        await UpdatePreview();
    }
    
    async public override Task<Variant> OnFire()
    {
        AnimationCurve distanceCurve = await GetDistanceCurve();
        List<List<Vector2>> warp = await GetWarp();
        float noiseIntensity = (await GetInputValue("Noise Intensity")).GetValue<float>();
        FBMSettings settings = await GetSettings();
        Vector2Int size = await GraphManager.Instance.GetTerrainSize();

        Debug.Log("Generating island texture with seed: " + settings.seed);

        List<List<float>> heightmap = await GenerateHeightmap(size, settings, warp, noiseIntensity, distanceCurve);
        UpdatePreviewWithHeightMap(heightmap);
        
        return new Variant(heightmap);
    }

    public override async void InputUpdated(ConnectorBehaviour connector)
    {
        base.InputUpdated(connector);
        await UpdatePreview();
    }

    public async Task UpdatePreview()
    {
        AnimationCurve distanceCurve = await GetDistanceCurve();
        List<List<Vector2>> warp = await GetWarp();
        float noiseIntensity = (await GetInputValue("Noise Intensity")).GetValue<float>();
        FBMSettings settings = await GetSettings();

        List<List<float>> heightmap = await GenerateHeightmap(previewSize, settings, warp, noiseIntensity, distanceCurve);

        preview.ApplyHeightMap(heightmap);
    }

    public async Task<List<List<float>>> GenerateHeightmap(Vector2Int size, FBMSettings settings, List<List<Vector2>> warp, float noiseIntensity, AnimationCurve distanceCurve)
    {
        List<List<float>> heightmap = new List<List<float>>();
        Vector2Int terrainSize = await GraphManager.Instance.GetTerrainSize();
        float maxDistance = Mathf.Max(terrainSize.x, terrainSize.y) / 2.0f;
        List<float> maxNoiseValues = new List<float>();
        for (int theta = 0; theta < 360; theta++)
        {
            float maxNoiseDistance = maxDistance + SampleNoise(theta, 2.0f, settings, warp) * noiseIntensity;
            maxNoiseValues.Add(maxNoiseDistance);
        }

        for (int y = 0; y < terrainSize.y; y++)
        {
            List<float> row = new List<float>();
            for (int x = 0; x < terrainSize.x; x++)
            {
                float angle = Mathf.Atan2(y - terrainSize.y / 2.0f, x - terrainSize.x / 2.0f) * Mathf.Rad2Deg;
                if (angle < 0) angle += 360;
                int angleIndex = Mathf.FloorToInt(angle);
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(terrainSize.x / 2.0f, terrainSize.y / 2.0f));
                float value = 1f - distance / maxNoiseValues[angleIndex];
                value = distanceCurve.Evaluate(value);
                row.Add(value);
            }
            heightmap.Add(row);
        }
        
        return heightmap;
    }

    public float SampleNoise(float angle, float radius, FBMSettings settings, List<List<Vector2>> warp)
    {
        float x = radius * Mathf.Cos(Mathf.Deg2Rad * angle);
        float y = radius * Mathf.Sin(Mathf.Deg2Rad * angle);

        Vector2 point = new Vector2(x, y);
        // point += warp[x][y];
        return fbm.GetValue(point.x, point.y, settings);
    }

    async public Task<FBMSettings> GetSettings()
    {
        FBMSettings settings = new FBMSettings();
        settings.seed = (await GetInputValue("seed")).GetValue<int>();
        settings.scale = (await GetInputValue("scale")).GetValue<float>();
        settings.curve = await GetNoiseCurve();
        return settings;
    }

    async public Task<AnimationCurve> GetNoiseCurve()
    {
        if (GetInputConnection("Noise Curve").IsConnected())
            return (await GetInputValue("Noise Curve")).GetValue<AnimationCurve>();
        else
            return AnimationCurve.Linear(0, 0, 1, 1);
    }

    async public Task<AnimationCurve> GetDistanceCurve()
    {
        if (GetInputConnection("Distance Curve").IsConnected())
            return (await GetInputValue("Distance Curve")).GetValue<AnimationCurve>();
        else
            return AnimationCurve.Linear(0, 0, 1, 1);
    }

    async public Task<List<List<Vector2>>> GetWarp()
    {
        if (GetInputConnection("warp").IsConnected())
            return (await GetInputValue("warp")).GetValue<List<List<Vector2>>>();
        else
            return new List<List<Vector2>>();
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
}
