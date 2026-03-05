using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class ThermalErosionAlgorithm : MonoBehaviour
{
    public void ApplyErosionStep(List<List<float>> heightMap, List<List<float>> bedrockMap, List<List<float>> sedimentMap, ThermalErosionSettings settings, float pixelDistance, Action<float, float> onProgress=null)
    {
        int width = heightMap.Count;
        int height = heightMap[0].Count;

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Vector2Int currentPos = new Vector2Int(i, j);
                float currentHeight = settings.sedimentMap ? bedrockMap[i][j] + sedimentMap[i][j] : heightMap[i][j];

                List<Vector2Int> neighbors = GetNeighbors(heightMap, currentPos);

                float randomFactor = UnityEngine.Random.Range(1f - settings.randomness, 1f + settings.randomness) / 50f;

                if (settings.sedimentMap)
                {
                    float bedrockSlope = GetBedrockSlope(bedrockMap, currentPos, neighbors, pixelDistance);
                    if (bedrockSlope + randomFactor > settings.talusAngle)
                    {
                        float productionAmount = bedrockSlope * settings.talusProduction / 10f;

                        bedrockMap[i][j] -= productionAmount;
                        sedimentMap[i][j] += productionAmount;
                    }
                }

                foreach (Vector2Int neighbor in neighbors)
                {
                    float neighborHeight = settings.sedimentMap ? bedrockMap[neighbor.x][neighbor.y] + sedimentMap[neighbor.x][neighbor.y] : heightMap[neighbor.x][neighbor.y];
                    float heightDifference = heightMap[i][j] - neighborHeight;
                    float diff = heightDifference / pixelDistance;

                    if (diff + randomFactor > settings.talusAngle)
                    {
                        float erosionAmount;
                        if (settings.sedimentMap)
                            erosionAmount = Mathf.Min(diff * settings.intensity / 10f, sedimentMap[i][j]);
                        else
                            erosionAmount = diff * settings.intensity / 10f;

                        if (settings.sedimentMap)
                        {
                            sedimentMap[i][j] -= erosionAmount;
                            sedimentMap[neighbor.x][neighbor.y] += erosionAmount;
                        }
                        
                        heightMap[i][j] -= erosionAmount;
                        heightMap[neighbor.x][neighbor.y] += erosionAmount;
                    }
                }
            }
        }
    }

    public List<Vector2Int> GetNeighbors(List<List<float>> heightMap, Vector2Int position)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();

        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0) continue;

                Vector2Int neighborPos = new Vector2Int(
                    position.x + i,
                    position.y + j
                );

                if (neighborPos.x < 0 || neighborPos.x >= heightMap.Count || neighborPos.y < 0 || neighborPos.y >= heightMap[0].Count)
                    continue;

                neighbors.Add(neighborPos);
            }
        }

        return neighbors;
    }

    public float GetBedrockSlope(List<List<float>> heightMap, Vector2Int position, List<Vector2Int> neighbors, float pixelDistance)
    {
        float minHeight = float.MaxValue;
        Vector2Int lowestNeighbor = new Vector2Int(-1, -1);

        foreach (Vector2Int neighbor in neighbors)
        {
            float neighborHeight = heightMap[neighbor.x][neighbor.y];
            if (neighborHeight < minHeight)
            {
                minHeight = neighborHeight;
                lowestNeighbor = neighbor;
            }
        }

        float heightDifference = heightMap[position.x][position.y] - heightMap[lowestNeighbor.x][lowestNeighbor.y];
        return heightDifference / pixelDistance;
    }

    public IEnumerator ApplyErosion(List<List<float>> heightMap, ThermalErosionSettings settings, float pixelDistance, Action<float, float> onProgress=null)
    {
        List<List<float>> sedimentMap = new List<List<float>>();
        List<List<float>> bedrockMap = new List<List<float>>();

        if (settings.sedimentMap)
        {
            for (int i = 0; i < heightMap.Count; i++)
            {
                sedimentMap.Add(new List<float>());
                for (int j = 0; j < heightMap[0].Count; j++)
                {
                    sedimentMap[i].Add(0f);
                }
            }

            for (int i = 0; i < heightMap.Count; i++)
            {
                bedrockMap.Add(new List<float>());
                for (int j = 0; j < heightMap[0].Count; j++)
                {
                    bedrockMap[i].Add(heightMap[i][j]);
                }
            }
        }

        for (int i = 1; i < settings.steps + 1; i++)
        {
            ApplyErosionStep(heightMap, bedrockMap, sedimentMap, settings, pixelDistance, onProgress);

            if (i % 2 == 0)
            {
                Debug.Log($"Erosion step {i}/{settings.steps}");
                onProgress?.Invoke(i, settings.steps);
                yield return new WaitForSeconds(0.01f);
            }
        }

        yield return null;
    }

    public void ErosionProcess(List<List<float>> heightMap, ThermalErosionSettings settings, float pixelDistance, Action<float, float> onProgress=null)
    {
        StartCoroutine(ApplyErosion(heightMap, settings, pixelDistance, onProgress));
    }
}

[System.Serializable]
public class ThermalErosionSettings
{
    public int steps = 50;
    public float intensity = 0.5f;
    public float talusProduction = 0.5f;
    public float talusAngle = 0.5f;
    public float randomness = 0.1f;
    public bool sedimentMap = true;
}
