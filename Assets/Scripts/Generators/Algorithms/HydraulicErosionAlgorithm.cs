using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class HydraulicErosionAlgorithm : MonoBehaviour
{
    public void ApplyErosionStep(List<List<float>> heightMap, float dropSize, HydraulicErosionSettings settings, float delayPerStep=0.25f, Action<float, float> onProgress=null)
    {
        int width = heightMap.Count;
        int height = heightMap[0].Count;

        Vector2 position = new Vector2(UnityEngine.Random.Range(0, width - 1), UnityEngine.Random.Range(0, height - 1));
        Vector2 direction = Vector2.zero;

        float speed = 1f;
        float water = dropSize;
        float sediment = 0f;

        float inertia = 0.05f;
        
        for (int i = 0; i < settings.maxStepsPerDrop; i++)
        {
            Vector2Int gridPosition = new Vector2Int(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y));
            Vector2 dropOffset = position - gridPosition;

            Tuple<Vector2, float> gradientAndNewHeight = GetGradientAndHeight(heightMap, position);
            Vector2 gradient = gradientAndNewHeight.Item1;

            float currentHeight = gradientAndNewHeight.Item2;

            Vector2 windFactor = Vector2.zero;
            if (settings.windEnabled)
            {
                windFactor = settings.windDirection.normalized * settings.windStrength;
            }

            Vector2 externalForces = -gradient + windFactor / 100f;
            direction = direction * inertia + externalForces * (1f - inertia);
            if (direction.magnitude != 0)
                direction.Normalize();

            position += direction;

            if (position.x < 0 || position.x >= width - 1 || position.y < 0 || position.y >= height - 1 || (direction == Vector2.zero))
                break;
            
            float newHeight = GetGradientAndHeight(heightMap, position, calculateGradient: false).Item2;
            float heightDelta = newHeight - currentHeight;

            float capacity = Mathf.Max(-heightDelta * speed * water * settings.intensity, 0.01f);

            if (sediment > capacity || heightDelta > 0)
            {
                float deposition;
                if (heightDelta > 0) deposition = Mathf.Min(sediment, heightDelta);
                else deposition = (sediment - capacity) * 0.3f;
                sediment -= deposition;
                
                // Bilinear interpolation for the 4 neighboring points
                heightMap[gridPosition.x][gridPosition.y] += deposition * (1f - dropOffset.x) * (1f - dropOffset.y);
                heightMap[gridPosition.x + 1][gridPosition.y] += deposition * dropOffset.x * (1f - dropOffset.y);
                heightMap[gridPosition.x][gridPosition.y + 1] += deposition * (1f - dropOffset.x) * dropOffset.y;
                heightMap[gridPosition.x + 1][gridPosition.y + 1] += deposition * dropOffset.x * dropOffset.y;
            }
            else
            {
                float erosion = Mathf.Min((capacity - sediment) * 0.3f, -heightDelta);
                sediment += erosion;
                ModifyTerrain(width, height, heightMap, gridPosition, -erosion, settings.radius);
            }

            speed = Mathf.Sqrt(speed * speed + heightDelta * 2f);
            water *= 0.99f; // Evaporation
        }
    }

    Tuple<Vector2, float> GetGradientAndHeight(List<List<float>> heightMap, Vector2 position, bool calculateGradient=true)
    {
        Vector2Int gridPosition = new Vector2Int(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y));
        Vector2 dropOffset = position - gridPosition;

        float h00 = heightMap[gridPosition.x][gridPosition.y];
        float h01 = heightMap[gridPosition.x][gridPosition.y + 1];
        float h10 = heightMap[gridPosition.x + 1][gridPosition.y];
        float h11 = heightMap[gridPosition.x + 1][gridPosition.y + 1];

        Vector2 gradient = Vector2.zero;
        if (calculateGradient)
        {
            gradient = new Vector2(
                (h10 - h00) * (1f - dropOffset.y) + (h11 - h01) * dropOffset.y,
                (h01 - h00) * (1f - dropOffset.x) + (h11 - h10) * dropOffset.x
            );
        }

        float height = h00 * (1f - dropOffset.x) * (1f - dropOffset.y) + h10 * dropOffset.x * (1f - dropOffset.y) + h01 * (1f - dropOffset.x) * dropOffset.y + h11 * dropOffset.x * dropOffset.y;

        return new Tuple<Vector2, float>(gradient, height);
    }

    public IEnumerator ApplyErosion(List<List<float>> heightMap, HydraulicErosionSettings settings, Action<float, float> onProgress=null)
    {
        for (int i = 1; i < settings.steps + 1; i++)
        {
            float currentDropSize = ProcessDropSize(settings.waterQuantity, i, settings.steps);
            ApplyErosionStep(heightMap, currentDropSize, settings, 0.1f, onProgress);

            if (i % 1000 == 0)
            {
                Debug.Log($"Erosion step {i}/{settings.steps}");
                onProgress?.Invoke(i, settings.steps);
                yield return new WaitForSeconds(0.01f);
            }
        }

        yield return null;
    }

    public void ErosionProcess(List<List<float>> heightMap, HydraulicErosionSettings settings, Action<float, float> onProgress=null)
    {
        StartCoroutine(ApplyErosion(heightMap, settings, onProgress));
    }

    public float ProcessDropSize(float dropSize, float current, float total)
    {
        float progress = current / total;
        return dropSize / (progress + 1f);
    }

    private void ModifyTerrain(int width, int height, List<List<float>> heightMap, Vector2Int pos, float amount, float radius)
    {
        int rInt = Mathf.CeilToInt(radius);
        int startX = Mathf.Max(0, pos.x - rInt);
        int startY = Mathf.Max(0, pos.y - rInt);
        int endX   = Mathf.Min(width - 1, pos.x + rInt);
        int endY   = Mathf.Min(height - 1, pos.y + rInt);

        var cells = new List<(int x, int y, float w)>();
        float weightSum = 0f;

        for (int x = startX; x <= endX; x++)
        for (int y = startY; y <= endY; y++)
        {
            float dx = x - pos.x;
            float dy = y - pos.y;
            float sqr = dx*dx + dy*dy;
            if (sqr < radius * radius)
            {
                float dist = Mathf.Sqrt(sqr);
                float w = 1f - (dist / radius);
                if (w > 0f) { cells.Add((x,y,w)); weightSum += w; }
            }
        }

        if (weightSum <= 0f) return;

        float invSum = 1f / weightSum;
        foreach (var c in cells)
        {
            float influence = c.w * invSum;
            heightMap[c.x][c.y] += amount * influence;
        }
    }
}


[System.Serializable]
public class HydraulicErosionSettings
{
    public int steps = 1000;
    public float waterQuantity = 1f;
    public float intensity = 1f;
    public float radius = 2f;
    public int maxStepsPerDrop = 100;

    [Header("Wind")]
    public bool windEnabled = false;
    public Vector2 windDirection = new Vector2(1f, 0f);
    public float windStrength = 1f;
}
