using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum FluvialErosionType
{
    D8,
    DInfinite
}

public class FluvialErosionAlgorithm : MonoBehaviour
{
    public void ApplyErosion(List<List<float>> heightMap, FluvialErosionSettings settings)
    {
        FillSinks(heightMap);
        Vector2Int[,] flowDirections = CalculateFlowDirections(heightMap);
        float[,] waterMap = new float[heightMap.Count, heightMap[0].Count];

        for (int i = 0; i < heightMap.Count; i++)
        {
            for (int j = 0; j < heightMap[0].Count; j++)
            {
                waterMap[i, j] = settings.waterQuantity;
            }
        }

        FlowAccumulation(heightMap, flowDirections, waterMap);
        ErodeHeightMap(heightMap, flowDirections, waterMap, settings);
    }

    public void ErodeHeightMap(List<List<float>> heightMap, Vector2Int[,] flowDirections, float[,] waterMap, FluvialErosionSettings settings)
    {
        for (int x = 0; x < heightMap.Count; x++)
        {
            for (int y = 0; y < heightMap[0].Count; y++)
            {
                float slope = heightMap[x][y] - heightMap[x + flowDirections[x, y].x][y + flowDirections[x, y].y];
                slope = Mathf.Max(0f, slope);
                slope = Mathf.Sqrt(slope + 1f) - 1f;

                float erosionAmount = settings.erosionIntensity / 10f * Mathf.Log(1f + waterMap[x, y]) * slope;
                heightMap[x][y] -= erosionAmount;
            }
        }
    }

    public float GetSlope(List<List<float>> heightMap, int x, int y)
    {
        float currentHeight = heightMap[x][y];
        float maxSlope = 0f;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                int newX = x + dx;
                int newY = y + dy;

                if (newX >= 0 && newX < heightMap.Count && newY >= 0 && newY < heightMap[0].Count)
                {
                    float neighborHeight = heightMap[newX][newY];
                    float slope = Mathf.Abs(currentHeight - neighborHeight);
                    maxSlope = Mathf.Max(maxSlope, slope);
                }
            }
        }

        return maxSlope;
    }

    public void FlowAccumulation(List<List<float>> heightMap, Vector2Int[,] flowDirections, float[,] waterMap)
    {
        Vector2Int mapSize = new Vector2Int(waterMap.GetLength(0), waterMap.GetLength(1));
        Vector2Int[] processingOrder = GetSortedHeightMapCells(heightMap);

        foreach (Vector2Int cell in processingOrder)
        {
            Vector2Int flowDir = flowDirections[cell.x, cell.y];
            if (flowDir != Vector2Int.zero)
            {
                int targetX = cell.x + flowDir.x;
                int targetY = cell.y + flowDir.y;
                if (targetX >= 0 && targetX < flowDirections.GetLength(0) && targetY >= 0 && targetY < flowDirections.GetLength(1))
                {
                    waterMap[targetX, targetY] += waterMap[cell.x, cell.y];
                }
            }

            /*List<Vector2Int> neighbors = GetNeighbors(heightMap, cell);
            float cellHeight = heightMap[cell.x][cell.y];
            foreach (Vector2Int neighbor in neighbors)
            {
                float neighborHeight = heightMap[neighbor.x][neighbor.y];
                float heightDiff = cellHeight - neighborHeight;
                if (heightDiff > 0)
                {
                    float multiplier = 1f + heightDiff * 2f;
                    waterMap[neighbor.x, neighbor.y] += waterMap[cell.x, cell.y] * multiplier;
                }
            }*/
        }
    }

    public Vector2Int[] GetSortedHeightMapCells(List<List<float>> heightMap)
    {
        List<Vector2Int> cells = new List<Vector2Int>();

        for (int x = 0; x < heightMap.Count; x++)
        {
            for (int y = 0; y < heightMap[0].Count; y++)
            {
                cells.Add(new Vector2Int(x, y));
            }
        }

        cells.Sort((a, b) => heightMap[b.x][b.y].CompareTo(heightMap[a.x][a.y]));
        return cells.ToArray();
    }

    public Vector2Int[,] CalculateFlowDirections(List<List<float>> heightMap)
    {
        Vector2Int[,] flowDirections = new Vector2Int[heightMap.Count, heightMap[0].Count];

        for (int x = 0; x < heightMap.Count; x++)
        {
            for (int y = 0; y < heightMap[0].Count; y++)
            {
                Vector2Int currentPos = new Vector2Int(x, y);
                Vector2Int steepestNeighbor = GetNeighbors(heightMap, currentPos, onlySteepest: true)[0];
                flowDirections[x, y] = steepestNeighbor - currentPos;
            }
        }
        return flowDirections;
    }

    public void FillSinks(List<List<float>> heightMap)
    {
        for (int x = 0; x < heightMap.Count; x++)
        {
            for (int y = 0; y < heightMap[0].Count; y++)
            {
                Vector2Int currentPos = new Vector2Int(x, y);
                List<Vector2Int> neighbors = GetNeighbors(heightMap, currentPos);

                float currentHeight = heightMap[x][y];
                bool isSink = true;
                float lowestNeighborHeight = float.MaxValue;

                foreach (Vector2Int neighbor in neighbors)
                {
                    if (heightMap[neighbor.x][neighbor.y] < currentHeight)
                    {
                        isSink = false;
                        break;
                    }
                    lowestNeighborHeight = Mathf.Min(lowestNeighborHeight, heightMap[neighbor.x][neighbor.y]);
                }

                if (isSink)
                {
                    heightMap[x][y] = lowestNeighborHeight;
                }
            }
        }
    }

    public List<Vector2Int> GetNeighbors(List<List<float>> heightMap, Vector2Int pos, bool onlySteepest = false)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();

        float steepestNeighborSlope = -1f;
        Vector2Int steepestNeighbor = new Vector2Int(-1, -1);

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                int newX = pos.x + dx;
                int newY = pos.y + dy;

                if (newX >= 0 && newX < heightMap.Count && newY >= 0 && newY < heightMap[0].Count)
                {
                    if (!onlySteepest)
                    {
                        neighbors.Add(new Vector2Int(newX, newY));
                    }
                    else
                    {
                        float neighborHeight = heightMap[newX][newY];
                        float slope = (heightMap[pos.x][pos.y] - neighborHeight) / Mathf.Sqrt(dx * dx + dy * dy);
                        if (slope > steepestNeighborSlope)
                        {
                            steepestNeighborSlope = slope;
                            steepestNeighbor = new Vector2Int(newX, newY);
                        }
                    }
                }
            }
        }

        if (onlySteepest && steepestNeighbor.x != -1)
        {
            neighbors.Add(steepestNeighbor);
        }

        return neighbors;
    }
}

public struct FlowTarget
{
    int x;
    int y;
    float weight;
}

[System.Serializable]
public class FluvialErosionSettings
{
    public float waterQuantity = 1f;
    public float erosionIntensity = 1f;
    public FluvialErosionType fluvialErosionType = FluvialErosionType.D8;
}
