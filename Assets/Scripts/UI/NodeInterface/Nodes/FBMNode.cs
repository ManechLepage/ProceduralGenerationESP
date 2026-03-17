using UnityEngine;
using System.Collections.Generic;

public class FBMNode : NodeBehaviour
{
    public FBMAlgorithm fbmAlgorithm;

    public override Variant Fire()
    {
        float scale = GetInputValue("scale").GetValue<float>();
        int octaves = GetInputValue("octaves").GetValue<int>();
        float lacunarity = GetInputValue("lacunarity").GetValue<float>();
        float persistence = GetInputValue("persistance").GetValue<float>();
        int seed = GetInputValue("seed").GetValue<int>();
        Vector2 offset = GetInputValue("offset").GetValue<Vector2>();

        List<List<Vector2>> domainMap;
        if (GetInputConnection("domainmap").IsConnected())
            domainMap = GetInputValue("domainmap").GetValue<List<List<Vector2>>>();
        else
            domainMap = new List<List<Vector2>>();

        Vector2Int terrainSize = GraphManager.Instance.GetTerrainSize();

        FBMSettings settings = new FBMSettings();
        settings.seed = seed;
        settings.scale = scale;
        settings.octaves = octaves;
        settings.lacunarity = lacunarity;
        settings.persistence = persistence;
        settings.offset = offset;

        if (domainMap.Count == 0 && Input.GetKey(KeyCode.P))
        {
            // Temporary, made by ChatGPT.
            domainMap = new List<List<Vector2>>();

            float warpStrength = 100f;
            float flowScale = settings.scale * 0.8f / 50f;
            float noiseScale = settings.scale * 2f / 50f;

            for (int x = 0; x < terrainSize.x; x++)
            {
                List<Vector2> column = new List<Vector2>();

                for (int y = 0; y < terrainSize.y; y++)
                {
                    // base noise used to generate a direction field
                    float n = Mathf.PerlinNoise(x * flowScale + settings.offset.x,
                                                y * flowScale + settings.offset.y);

                    // convert noise to angle -> creates a chaotic flow field
                    float angle = n * Mathf.PI * 2f;

                    Vector2 flow = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                    // secondary noise adds distortion to the flow
                    float distortion = Mathf.PerlinNoise(
                        (x + 200f) * noiseScale + settings.offset.y,
                        (y + 200f) * noiseScale + settings.offset.x
                    ) * 2f - 1f;

                    flow += new Vector2(-flow.y, flow.x) * distortion;

                    Vector2 domainValue = new Vector2(
                        x + flow.x * warpStrength,
                        y + flow.y * warpStrength
                    );

                    column.Add(domainValue);
                }

                domainMap.Add(column);
            }
        }

        List<List<float>> heightMap = fbmAlgorithm.GetHeightMapThreading(terrainSize, settings, domainMap);

        return new Variant(heightMap);
    }
}
