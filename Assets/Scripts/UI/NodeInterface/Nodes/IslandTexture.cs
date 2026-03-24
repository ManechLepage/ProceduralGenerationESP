using UnityEngine;
using System.Collections.Generic;

public class IslandTexture : NodeBehaviour
{
    [SerializeField] private FBMAlgorithm fbm;
    [SerializeField] private PreviewBehaviour preview;
    public override Variant OnFire()
    {
        AnimationCurve noiseCurve = GetInputValue("Noise Curve").GetValue<AnimationCurve>();
        AnimationCurve distanceCurve = GetInputValue("Distance Curve").GetValue<AnimationCurve>();
        int seed = GetInputValue("seed").GetValue<int>();
        List<List<Vector2>> warp = GetInputValue("warp").GetValue<List<List<Vector2>>>();
        float scale = GetInputValue("scale").GetValue<float>();
        float noiseIntensity = GetInputValue("Noise Intensity").GetValue<float>();

        FBMSettings settings = new FBMSettings();
        settings.seed = seed;
        settings.scale = scale;
        settings.curve = noiseCurve;
        Vector2Int size = GraphManager.Instance.GetTerrainSize();

        Debug.Log("Generating island texture with seed: " + seed);
        
        return new Variant(GenerateHeightmap(size, settings, warp, noiseIntensity, distanceCurve));
    }

    public List<List<float>> GenerateHeightmap(Vector2Int size, FBMSettings settings, List<List<Vector2>> warp, float noiseIntensity, AnimationCurve distanceCurve)
    {
        List<List<float>> heightmap = new List<List<float>>();
        Vector2Int terrainSize = GraphManager.Instance.GetTerrainSize();
        float maxDistance = terrainSize.magnitude / 2.0f;
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
                float value = distance / maxNoiseValues[angleIndex];
                value = distanceCurve.Evaluate(value);
                row.Add(value);
            }
            heightmap.Add(row);
        }
        preview.ApplyHeightMap(heightmap);
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
}
