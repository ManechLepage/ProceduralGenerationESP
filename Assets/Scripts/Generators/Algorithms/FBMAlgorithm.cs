using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;

public class FBMAlgorithm : MonoBehaviour
{
    private FBMSettings baseSettings;

    void Awake()
    {
        baseSettings = new FBMSettings();
    }

    public float GetValue(float x, float y, FBMSettings settings = null)
    {
        settings = settings ?? baseSettings;

        float amplitude = 1;
        float frequency = 1;
        float noiseHeight = 0;

        for (int i = 0; i < settings.octaves; i++)
        {
            float xCoord = (x - settings.seed) * settings.scale * frequency;
            float yCoord = (y - settings.seed) * settings.scale * frequency;

            float sample = Mathf.PerlinNoise(xCoord, yCoord) * 2f - 1f;

            if (settings.absolute)
                sample = Mathf.Abs(sample);

            noiseHeight += sample * amplitude;

            amplitude *= settings.persistence;
            frequency *= settings.lacunarity;
        }

        if (!settings.absolute)
            noiseHeight = (noiseHeight + 1f) / 2f;
        
        noiseHeight = settings.curve.Evaluate(noiseHeight);
        return noiseHeight;
    }

    public List<List<float>> GetHeightMap(Vector2 size, FBMSettings settings = null)
    {
        settings = settings ?? baseSettings;

        List<List<float>> heightMap = new List<List<float>>();

        for (int x = 0; x < size.x; x++)
        {
            heightMap.Add(new List<float>());
            for (int y = 0; y < size.y; y++)
            {
                float xCoord = (float)(x + settings.offset.x) / size.x;
                float yCoord = (float)(y + settings.offset.y) / size.y;

                float noiseHeight = GetValue(xCoord, yCoord, settings);
                heightMap[heightMap.Count - 1].Add(noiseHeight);
            }
        }

        return heightMap;
    }

    public List<List<float>> GetHeightMapThreading(Vector2 size, FBMSettings settings = null)
    {
        settings = settings ?? baseSettings;

        int width = (int)size.x;
        int height = (int)size.y;
        int totalCells = width * height;

        NativeArray<float> results = new NativeArray<float>(totalCells, Allocator.TempJob);

        // Use AnimationCurve in jobs
        int resolution = 256;
        NativeArray<float> curveLUT =
            new NativeArray<float>(resolution, Allocator.TempJob);

        for (int i = 0; i < resolution; i++)
        {
            float t = i / (float)(resolution - 1);
            curveLUT[i] = settings.curve.Evaluate(t);
        }

        CalculateHeightJob job = new CalculateHeightJob
        {
            width = width,
            height = height,
            seed = settings.seed,
            scale = settings.scale,
            offset = (float2)settings.offset,
            octaves = settings.octaves,
            persistence = settings.persistence,
            lacunarity = settings.lacunarity,
            absolute = settings.absolute,
            curveLUT = curveLUT,
            results = results
        };

        JobHandle handle = job.Schedule(totalCells, 256);
        handle.Complete();

        List<List<float>> heightMap = CombineResults(results, size);

        results.Dispose();
        curveLUT.Dispose();

        return heightMap;
    }

    [BurstCompile]
    struct CalculateHeightJob : IJobParallelFor
    {
        [ReadOnly] public int width;
        [ReadOnly] public int height;
        [ReadOnly] public int seed;
        [ReadOnly] public float scale;
        [ReadOnly] public float2 offset;
        [ReadOnly] public int octaves;
        [ReadOnly] public float persistence;
        [ReadOnly] public float lacunarity;
        [ReadOnly] public bool absolute;
        [ReadOnly] public NativeArray<float> curveLUT;

        [WriteOnly] public NativeArray<float> results;

        public void Execute(int index)
        {
            int x = index % width;
            int y = index / width;

            float xCoord = (float)(x + offset.x) / width;
            float yCoord = (float)(y + offset.y) / height;

            results[index] = GetValueJob(xCoord, yCoord);
        }

        float GetValueJob(float x, float y)
        {
            float amplitude = 1;
            float frequency = 1;
            float noiseHeight = 0;

            for (int i = 0; i < octaves; i++)
            {
                float xCoord = (x - seed) * scale * frequency;
                float yCoord = (y - seed) * scale * frequency;

                float sample = Mathf.PerlinNoise(xCoord, yCoord) * 2f - 1f;

                if (absolute)
                    sample = Mathf.Abs(sample);

                noiseHeight += sample * amplitude;

                amplitude *= persistence;
                frequency *= lacunarity;
            }

            if (!absolute)
                noiseHeight = (noiseHeight + 1f) / 2f;
            
            //noiseHeight = settings.curve.Evaluate(noiseHeight);
            float t = math.saturate(noiseHeight);
            int idx = (int)(t * (curveLUT.Length - 1));
            float curvedNoiseHeight = curveLUT[idx];

            return curvedNoiseHeight;
        }
    }

    List<List<float>> CombineResults(NativeArray<float> results, Vector2 size)
    {
        List<List<float>> heightMap = new List<List<float>>();

        for (int x = 0; x < size.x; x++)
        {
            heightMap.Add(new List<float>());
            for (int y = 0; y < size.y; y++)
            {
                int index = y * (int)size.x + x;
                heightMap[x].Add(results[index]);
            }
        }

        return heightMap;
    }
}


[System.Serializable]
public class FBMSettings
{
    public int seed = 0;

    [Space]
    public float scale = 1;
    public int octaves = 4;
    public float persistence = 0.5f;
    public float lacunarity = 2f;
    public Vector2 offset = Vector2.zero;

    [Space]
    public bool absolute = false;

    [Space]
    public AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);

    public FBMSettings GetCopy()
    {
        return new FBMSettings
        {
            seed = this.seed,
            scale = this.scale,
            octaves = this.octaves,
            persistence = this.persistence,
            lacunarity = this.lacunarity,
            offset = this.offset,
            absolute = this.absolute,
            curve = new AnimationCurve(this.curve.keys)
        };
    }

    public bool SameSettings(FBMSettings other)
    {
        return 
            this.seed == other.seed &&
            this.scale == other.scale &&
            this.octaves == other.octaves &&
            Mathf.Approximately(this.persistence, other.persistence) &&
            Mathf.Approximately(this.lacunarity, other.lacunarity) &&
            this.offset == other.offset &&
            this.absolute == other.absolute &&
            GameManager.Instance.algorithmHelpers.EqualAnimationCurves(this.curve, other.curve);
    }
}