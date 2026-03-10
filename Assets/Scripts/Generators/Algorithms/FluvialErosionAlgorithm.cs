using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum FluvialErosionType
{
    D8,
    MFD,
    DInfinite
}

public class FluvialErosionAlgorithm : MonoBehaviour
{

    void Awake()
    {
        //if (AlgorithmRegistry.Instance != null)
        AlgorithmRegistry.Instance.Register("FEA");
    }
    Vector2Int[] directions = new Vector2Int[]
    {
        new Vector2Int(1, 0),   
        new Vector2Int(1, 1),   
        new Vector2Int(0, 1),   
        new Vector2Int(-1, 1),  
        new Vector2Int(-1, 0),  
        new Vector2Int(-1, -1), 
        new Vector2Int(0, -1),  
        new Vector2Int(1, -1)   
    };

    public void ApplyErosion(List<List<float>> heightMap, FluvialErosionSettings settings)
    {
        FillSinks(heightMap);
        List<FlowTarget>[,] flowTargets = CalculateFlowTargets(heightMap, settings);
        float[,] waterMap = new float[heightMap.Count, heightMap[0].Count];

        for (int i = 0; i < heightMap.Count; i++)
        {
            for (int j = 0; j < heightMap[0].Count; j++)
            {
                waterMap[i, j] = settings.waterQuantity;
            }
        }

        FlowAccumulation(heightMap, flowTargets, waterMap);
        ErodeHeightMap(heightMap, flowTargets, waterMap, settings);
    }

    public void ErodeHeightMap(List<List<float>> heightMap, List<FlowTarget>[,] flowTargets, float[,] waterMap, FluvialErosionSettings settings)
    {
        for (int x = 0; x < heightMap.Count; x++)
        {
            for (int y = 0; y < heightMap[0].Count; y++)
            {
                //float slope = heightMap[x][y] - heightMap[x + flowDirections[x, y].x][y + flowDirections[x, y].y];
    
                float slope = 0f;
                foreach (FlowTarget cellFlowTarget in flowTargets[x, y])
                {
                    int targetX = x + cellFlowTarget.x;
                    int targetY = y + cellFlowTarget.y;
                    if (targetX < 0 || targetX >= heightMap.Count || targetY < 0 || targetY >= heightMap[0].Count)
                    {
                        continue;
                    }
                    slope += (heightMap[x][y] - heightMap[targetX][targetY]) * cellFlowTarget.weight;
                }

                slope = Mathf.Max(0f, slope);
                slope = Mathf.Sqrt(slope + 1f) - 1f;

                float erosionAmount = settings.erosionIntensity / 10f * Mathf.Log(1f + Mathf.Log(1f + waterMap[x, y])) * slope;

                if (waterMap[x, y] > settings.riverThreshold * 10f)
                {
                    erosionAmount *= settings.riverIntensity;
                }

                float minHeight = 0f;
                erosionAmount = Mathf.Min(erosionAmount, heightMap[x][y] - minHeight);
                if (float.IsNaN(erosionAmount) || float.IsInfinity(erosionAmount)) erosionAmount = 0f;
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

    public void FlowAccumulation(List<List<float>> heightMap, List<FlowTarget>[,] flowTargets, float[,] waterMap)
    {
        Vector2Int mapSize = new Vector2Int(waterMap.GetLength(0), waterMap.GetLength(1));
        Vector2Int[] processingOrder = GetSortedHeightMapCells(heightMap);

        foreach (Vector2Int cell in processingOrder)
        {
            List<FlowTarget> cellFlowTargets = flowTargets[cell.x, cell.y];
            foreach (FlowTarget target in cellFlowTargets)
            {
                Vector2Int flowDir = new Vector2Int(target.x, target.y);
                if (flowDir != Vector2Int.zero)
                {
                    int targetX = cell.x + flowDir.x;
                    int targetY = cell.y + flowDir.y;
                    if (targetX >= 0 && targetX < mapSize.x && targetY >= 0 && targetY < mapSize.y)
                    {
                        waterMap[targetX, targetY] += waterMap[cell.x, cell.y] * target.weight;
                    }
                }
            }
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

    public List<FlowTarget>[,] CalculateFlowTargets(List<List<float>> heightMap, FluvialErosionSettings settings)
    {
        List<FlowTarget>[,] flowTargets = new List<FlowTarget>[heightMap.Count, heightMap[0].Count];

        for (int x = 0; x < heightMap.Count; x++)
        {
            for (int y = 0; y < heightMap[0].Count; y++)
            {
                Vector2Int currentPos = new Vector2Int(x, y);
                float currentHeight = heightMap[x][y];
                
                int maxTargets = 1;
                if (settings.erosionType == FluvialErosionType.MFD)
                    maxTargets = 8;
                
                if (settings.erosionType != FluvialErosionType.DInfinite)
                {
                    List<Vector2Int> neighbors = GetNeighbors(heightMap, currentPos);
                    List<FlowTarget> cellFlowTargets = new List<FlowTarget>();

                    foreach (Vector2Int neighbor in neighbors)
                    {
                        Vector2Int direction = new Vector2Int(neighbor.x - x, neighbor.y - y);
                        float slope = currentHeight - heightMap[neighbor.x][neighbor.y];
                        if (direction.x != 0 && direction.y != 0)
                            slope /= Mathf.Sqrt(2f);

                        if (slope > 0)
                        {
                            slope = Mathf.Pow(slope, 1.1f);
                            cellFlowTargets.Add(new FlowTarget { x = direction.x, y = direction.y, weight = slope });
                        }
                    }

                    cellFlowTargets.Sort((a, b) => b.weight.CompareTo(a.weight));

                    if (cellFlowTargets.Count == 0)
                    {
                        flowTargets[x, y] = cellFlowTargets;
                        continue;
                    }

                    if (cellFlowTargets.Count > 1)
                    {
                        float totalHeightDiff = 0f;

                        if (cellFlowTargets.Count > maxTargets)
                        {
                            cellFlowTargets.RemoveRange(maxTargets, cellFlowTargets.Count - maxTargets);
                        }
                        
                        foreach (FlowTarget cellFlowTarget in cellFlowTargets)
                        {
                            totalHeightDiff += cellFlowTarget.weight;
                        }

                        float impactFactor = 1f / totalHeightDiff;
                        for (int i = 0; i < cellFlowTargets.Count; i++)
                        {
                            FlowTarget t = cellFlowTargets[i];
                            t.weight = impactFactor * t.weight;
                            cellFlowTargets[i] = t;
                        }
                    }
                    else
                    {
                        FlowTarget t = cellFlowTargets[0];
                        t.weight = 1f;
                        cellFlowTargets[0] = t;
                    }

                    flowTargets[x, y] = cellFlowTargets;
                }
                else
                {
                    Vector2 gradient = CalculateGradientAtPoint(heightMap, x, y);

                    if (gradient.magnitude < 1e-6f)
                    {
                        var steep = GetNeighbors(heightMap, currentPos, true);
                        if (steep.Count > 0)
                            flowTargets[x,y] = new List<FlowTarget> { new FlowTarget { x = steep[0].x - x, y = steep[0].y - y, weight = 1f } };
                        else
                            flowTargets[x,y] = new List<FlowTarget>();
                        continue;
                    }

                    float angle = Mathf.Atan2(gradient.y, gradient.x);
                    if (angle < 0)
                        angle += 2 * Mathf.PI;
                        
                    float sector = angle / (Mathf.PI / 4f);
                    int neighborIndex = Mathf.FloorToInt(sector) % 8;

                    Vector2Int neighbor1 = directions[neighborIndex];
                    Vector2Int neighbor2 = directions[(neighborIndex + 1) % 8];

                    Vector2Int neighbor1Pos = new Vector2Int(x + neighbor1.x, y + neighbor1.y);
                    Vector2Int neighbor2Pos = new Vector2Int(x + neighbor2.x, y + neighbor2.y);

                    bool validNeighbor1 = neighbor1Pos.x >= 0 && neighbor1Pos.x < heightMap.Count && neighbor1Pos.y >= 0 && neighbor1Pos.y < heightMap[0].Count;
                    bool validNeighbor2 = neighbor2Pos.x >= 0 && neighbor2Pos.x < heightMap.Count && neighbor2Pos.y >= 0 && neighbor2Pos.y < heightMap[0].Count;

                    float angle_i = neighborIndex * (Mathf.PI / 4f);
                    float t = (angle - angle_i) / (Mathf.PI / 4f);

                    float weight1;
                    float weight2;
    
                    if (validNeighbor1 && validNeighbor2)
                    {
                        weight1 = 1f - t;
                        weight2 = t;
                    }
                    else if (validNeighbor1)
                    {
                        weight1 = 1f;
                        weight2 = 0f;
                    }
                    else if (validNeighbor2)
                    {
                        weight1 = 0f;
                        weight2 = 1f;
                    }
                    else
                    {
                        weight1 = 0f;
                        weight2 = 0f;
                    }

                    List<FlowTarget> cellFlowTargets = new List<FlowTarget>();

                    if (validNeighbor1)
                        cellFlowTargets.Add(new FlowTarget { x = neighbor1.x, y = neighbor1.y, weight = weight1 });
                    
                    if (validNeighbor2)
                        cellFlowTargets.Add(new FlowTarget { x = neighbor2.x, y = neighbor2.y, weight = weight2 });

                    flowTargets[x, y] = cellFlowTargets;
                }
            }
        }
        return flowTargets;
    }

    public Vector2 CalculateGradientAtPoint(List<List<float>> heightMap, int x, int y)
{
    float hNW = SampleHeightMap(heightMap, x - 1, y + 1);
    float hN  = SampleHeightMap(heightMap, x,     y + 1);
    float hNE = SampleHeightMap(heightMap, x + 1, y + 1);
    float hW  = SampleHeightMap(heightMap, x - 1, y);
    float hC  = SampleHeightMap(heightMap, x,     y);
    float hE  = SampleHeightMap(heightMap, x + 1, y);
    float hSW = SampleHeightMap(heightMap, x - 1, y - 1);
    float hS  = SampleHeightMap(heightMap, x,     y - 1);
    float hSE = SampleHeightMap(heightMap, x + 1, y - 1);

    float gradientX = ((hNE + 2f*hE + hSE) - (hNW + 2f*hW + hSW)) / 8f;
    float gradientY = ((hSW + 2f*hS + hSE) - (hNW + 2f*hN + hNE)) / 8f;

    return new Vector2(gradientX, gradientY);
}

    public float SampleHeightMap(List<List<float>> heightMap, int x, int y)
    {
        x = Mathf.Max(0, Mathf.Min(x, heightMap.Count - 1));
        y = Mathf.Max(0, Mathf.Min(y, heightMap[0].Count - 1));
        return heightMap[x][y];
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
    public int x;
    public int y;
    public float weight;
}

[System.Serializable]
public class FluvialErosionSettings
{
    public float waterQuantity = 1f;
    public float erosionIntensity = 1f;
    public FluvialErosionType erosionType = FluvialErosionType.D8;
    public float riverThreshold = 1f;
    public float riverIntensity = 2f;
}
