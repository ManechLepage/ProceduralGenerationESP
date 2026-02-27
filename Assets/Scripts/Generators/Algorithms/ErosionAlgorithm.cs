using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class ErosionAlgorithm : MonoBehaviour
{
    private Vector2Int[] directions = new Vector2Int[]
    {
        new Vector2Int(0, 1), // N
        new Vector2Int(1, 1), // NE
        new Vector2Int(1, 0), // E
        new Vector2Int(1, -1), // SE
        new Vector2Int(0, -1), // S
        new Vector2Int(-1, -1), // SW
        new Vector2Int(-1, 0), // W
        new Vector2Int(-1, 1) // NW
    };

    public void ApplyErosionStep(List<List<float>> heightMap, float dropSize, float intensity, int maxSteps=100)
    {
        int width = heightMap.Count;
        int height = heightMap[0].Count;

        List<Tuple<int, int, float>> dropModifications = new List<Tuple<int, int, float>>();

        Vector2Int dropPositionInt = new Vector2Int(UnityEngine.Random.Range(0, width), UnityEngine.Random.Range(0, height));
        Vector2 dropSpeed = Vector2.zero;
        float dropSediment = 0f;
        
        for (int i = 0; i < maxSteps; i++)
        {
            float lastHeight = heightMap[dropPositionInt.x][dropPositionInt.y];
            Vector2 gradient = CalculateGradient(heightMap, dropPositionInt.x, dropPositionInt.y);

            Vector2 combinedDirection = new Vector2(
                (gradient.x + dropSpeed.x) * 0.5f,
                (gradient.y + dropSpeed.y) * 0.5f
            );

            Vector2Int newPositionInt = MoveDrop(heightMap, dropPositionInt, combinedDirection);

            float newHeight = heightMap[newPositionInt.x][newPositionInt.y];

            float dy = newHeight - lastHeight;
            float dx = dropPositionInt.x - newPositionInt.x;
            float dz = dropPositionInt.y - newPositionInt.y;
            float slopeX = dx == 0f ? 0f : dy / dx;
            float slopeY = dz == 0f ? 0f : dy / dz;

            Vector2 slope = new Vector2(slopeX, slopeY);
            float slopeValue = slope.magnitude;

            dropSpeed += slope * 0.1f;

            float sedimentOutcome = dy > 0f ? -slopeValue : slopeValue;
            dropSediment -= sedimentOutcome;
            if (dropSediment < 0f)
            {
                sedimentOutcome -= dropSediment;
                dropSediment = 0f;
            }
    
            float radius = dropSize * 3f;
            float amount = sedimentOutcome / 25f * intensity;

            //Debug.Log($"Drop moved from {dropPositionInt} to {newPositionInt}, height difference {dy}, with slope {slope.x}, {slope.y}, sediment outcome {sedimentOutcome}, drop sediment {dropSediment}, amount {amount}");

            ModifyTerrain(width, height, dropModifications, dropPositionInt, -amount, radius);

            dropSize -= 0.1f * slopeValue;
            if (dropSize <= 0f)
                break;
            
            dropPositionInt = newPositionInt;
        }

        foreach (var mod in dropModifications)
        {
            heightMap[mod.Item1][mod.Item2] += mod.Item3;
            if (heightMap[mod.Item1][mod.Item2] < 0f)
                heightMap[mod.Item1][mod.Item2] = 0f;
        }
    }

    public void ApplyErosion(List<List<float>> heightMap, ErosionSettings settings)
    {
        for (int i = 1; i < settings.steps + 1; i++)
        {
            ApplyErosionStep(heightMap, settings.dropSize, settings.intensity, settings.maxStepsPerDrop);
            if (i % 100 == 0)
                Debug.Log($"Erosion step {i}/{settings.steps}");
        }
    }

    private void ModifyTerrain(int width, int height, List<Tuple<int, int, float>> dropModifications, Vector2Int position, float amount, float radius)
    {
        int radiusInt = Mathf.CeilToInt(radius);
        int startX = Mathf.Max(0, position.x - radiusInt);
        int startY = Mathf.Max(0, position.y - radiusInt);
        int endX = Mathf.Min(width - 1, position.x + radiusInt);
        int endY = Mathf.Min(height - 1, position.y + radiusInt);

        for (int x = startX; x < endX; x++)
        {
            for (int y = startY; y < endY; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), position);
                if (distance < radiusInt)
                {
                    float influence = 1f - (distance / radiusInt);
                    dropModifications.Add(new Tuple<int, int, float>(x, y, amount * influence * (radius / radiusInt)));
                }
            }
        }
    }

    public Vector2Int MoveDrop(List<List<float>> heightMap, Vector2Int position, Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x);
        int closestDirectionIndex = Mathf.RoundToInt(angle / (Mathf.PI / 4f)) % 8;

        if (closestDirectionIndex < 0)
            closestDirectionIndex += 8;

        Vector2Int newPosition = position + directions[closestDirectionIndex];
        newPosition.x = Mathf.Max(0, Mathf.Min(heightMap.Count - 1, newPosition.x));
        newPosition.y = Mathf.Max(0, Mathf.Min(heightMap[0].Count - 1, newPosition.y));
        return newPosition;
    }

    public Vector2 CalculateGradient(List<List<float>> heightMap, int x, int y)
    {
        float hL = SampleHeightMap(heightMap, x - 1, y);
        float hR = SampleHeightMap(heightMap, x + 1, y);
        float hD = SampleHeightMap(heightMap, x, y - 1);
        float hU = SampleHeightMap(heightMap, x, y + 1);

        return new Vector2(hR - hL, hU - hD).normalized;
    }

    private float SampleHeightMap(List<List<float>> heightMap, int x, int y)
    {
        int width = heightMap.Count;
        int height = heightMap[0].Count;

        if (x < 0) x = 0;
        if (x >= width) x = width - 1;
        if (y < 0) y = 0;
        if (y >= height) y = height - 1;

        return heightMap[x][y];
    }
}


[System.Serializable]
public class ErosionSettings
{
    public int steps = 1000;
    public float dropSize = 1f;
    public float intensity = 1f;
    public int maxStepsPerDrop = 100;
}
