using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class ErosionAlgorithm : MonoBehaviour
{
    private Vector2Int[] directions = new Vector2Int[]
    {
        new Vector2Int(1, 0), // E
        new Vector2Int(1, 1), // NE
        new Vector2Int(0, 1), // N
        new Vector2Int(-1, 1), // NW
        new Vector2Int(-1, 0), // W
        new Vector2Int(-1, -1), // SW
        new Vector2Int(-1, -1), // S
        new Vector2Int(1, -1) // SE
    };

    public void ApplyErosionStep(List<List<float>> heightMap, float dropSize, float intensity, int maxSteps=50, float delayPerStep=0.25f, Action<float, float> onProgress=null)
    {
        int width = heightMap.Count;
        int height = heightMap[0].Count;

        List<List<float>> heightMapCopy = new List<List<float>>();
        for (int i = 0; i < heightMap.Count; i++)
        {
            heightMapCopy.Add(new List<float>(heightMap[i]));
        }

        //List<Tuple<int, int, float>> dropModifications = new List<Tuple<int, int, float>>();

        Vector2Int position = new Vector2Int(UnityEngine.Random.Range(0, width), UnityEngine.Random.Range(0, height));
        Vector2 flowSpeed = Vector2.zero;
        float sedimentCapacity = 0f;
        float sediment = 0f;
        float waterQuantity = dropSize;
        
        for (int i = 0; i < maxSteps; i++)
        {
            List<Vector2Int> neighborHeights = GetNeighbors(position.x, position.y, width, height);
            float minHeight = float.MaxValue;
            Vector2Int lowestNeighbor = position;
            foreach (Vector2Int neighborHeight in neighborHeights)
            {
                float neighborHeightValue = heightMapCopy[neighborHeight.x][neighborHeight.y];
                if (neighborHeightValue < minHeight)
                {
                    minHeight = neighborHeightValue;
                    lowestNeighbor = neighborHeight;
                }
            }

            Vector2Int movement = lowestNeighbor - position;

            //Vector2 gradient = -CalculateGradient(heightMapCopy, position.x, position.y);

            Vector2 direction = (movement + flowSpeed) / 2f; // Combine gradient and flow speed for movement direction

            Vector2Int newPosition = MoveDrop(heightMapCopy, position, direction);

            float startHeight = heightMapCopy[position.x][position.y];
            float newHeight = heightMapCopy[newPosition.x][newPosition.y];

            float heightDelta = newHeight - startHeight;
            float xDelta = newPosition.x - position.x;
            float yDelta = newPosition.y - position.y;

            float speedX = xDelta == 0f ? 0f : heightDelta / xDelta;
            float speedY = yDelta == 0f ? 0f : heightDelta / yDelta;

            Vector2 speedDelta = new Vector2(-speedX, -speedY);
            float slopeMagnitude = speedDelta.magnitude;
            flowSpeed += speedDelta * 5f; // Increase when downhill, decrease when uphill

            //Debug.Log($"Position: {position.x}, {position.y}, Direction: {direction.x}, {direction.y}, Speed: {flowSpeed.x}, {flowSpeed.y}, Height Delta: {heightDelta}, Speed Delta: {speedDelta.x}, {speedDelta.y}");

            sedimentCapacity = Mathf.Max(flowSpeed.magnitude / 5f * waterQuantity / 20f, 0.01f); // Capacity increases with speed and water quantity
            float deposition = 0f;

            if (heightDelta > 0f || sediment > sedimentCapacity)
            {
                float depositionSediment = Mathf.Min(sediment / 2f, Mathf.Abs(heightDelta));
                deposition += depositionSediment;
                sediment -= depositionSediment;
            }
            else
            {
                float erosion = (sedimentCapacity - sediment) / 5f * slopeMagnitude * 25f;
                erosion = Mathf.Min(erosion, -heightDelta); // Don't erode more than the height difference
                deposition -= erosion;
                sediment += erosion;
            }

            waterQuantity *= 0.975f; // Evaporation
            float radiusMultiplier = deposition < 0f ? 2f : 0;

            //Debug.Log($"Position: {position.x}, {position.y}, Gradient: {gradient.x}, {gradient.y}, Movement: {newPosition.x - position.x}, {newPosition.y - position.y}, Speed: {flowSpeed.x}, {flowSpeed.y}, Sediment: {sediment}, Capacity: {sedimentCapacity}, Deposition: {deposition}");

            //float deposition = newPosition == position ? 0f : intensity / 100f;

            ModifyTerrain(width, height, heightMap, newPosition, deposition, waterQuantity * radiusMultiplier);
            
            position = newPosition;
            //if (onProgress != null)
            //    onProgress?.Invoke(i + 1, maxSteps);
            //yield return new WaitForSeconds(delayPerStep);
        }

        //yield return null;
    }

    List<Vector2Int> GetNeighbors(int x, int y, int width, int height)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                int nx = x + dx;
                int ny = y + dy;

                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    neighbors.Add(new Vector2Int(nx, ny));
                }
            }
        }

        return neighbors;
    }

    public IEnumerator ApplyErosion(List<List<float>> heightMap, ErosionSettings settings, Action<float, float> onProgress=null)
    {
        for (int i = 1; i < settings.steps + 1; i++)
        {
            //yield return StartCoroutine(ApplyErosionStep(heightMap, settings.dropSize, settings.intensity, settings.maxStepsPerDrop, 0.1f, onProgress));
            ApplyErosionStep(heightMap, settings.dropSize, settings.intensity, settings.maxStepsPerDrop, 0.1f, onProgress);

            if (i % 50 == 0)
            {
                Debug.Log($"Erosion step {i}/{settings.steps}");
                onProgress?.Invoke(i, settings.steps);
                yield return new WaitForSeconds(0.1f);
            }
        }

        yield return null;
    }

    public void ErosionProcess(List<List<float>> heightMap, ErosionSettings settings, Action<float, float> onProgress=null)
    {
        StartCoroutine(ApplyErosion(heightMap, settings, onProgress));
    }

    private void ModifyTerrain(int width, int height, List<List<float>> heightMap, Vector2Int position, float amount, float radius)
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
                    //dropModifications.Add(new Tuple<int, int, float>(x, y, amount * influence * (radius / radiusInt)));
                    heightMap[x][y] += amount * influence * (radius / radiusInt);
                }
            }
        }
    }

    public Vector2Int MoveDrop(List<List<float>> heightMap, Vector2Int position, Vector2 direction)
    {
        //float angle = Mathf.Atan2(direction.y, direction.x);
        //int closestDirectionIndex = Mathf.RoundToInt(angle / (Mathf.PI / 4f)) % 8;

        //if (closestDirectionIndex < 0)
        //    closestDirectionIndex += 8;

        direction = direction.normalized;

        Vector2Int offset = new Vector2Int(
            Mathf.RoundToInt(direction.x),
            Mathf.RoundToInt(direction.y)
        );

        Vector2Int newPosition = position + offset;
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
