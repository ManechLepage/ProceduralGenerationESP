using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PreviewBehaviour : MonoBehaviour
{
    public RawImage rawImage;

    public void ApplyHeightMap(List<List<float>> heightMap)
    {
        if (heightMap == null || heightMap.Count == 0 || heightMap[0].Count == 0)
        {
            Debug.LogWarning("PreviewBehaviour: Invalid heightmap provided.");
            return;
        }

        int width = heightMap[0].Count;
        int height = heightMap.Count;

        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float value = heightMap[y][x];
                texture.SetPixel(x, y, new Color(value, value, value));
            }
        }
        texture.Apply();
        rawImage.texture = texture;
    }
}
