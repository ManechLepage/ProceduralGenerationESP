using UnityEngine;
using System.Collections.Generic;

public class TextureHelpers : MonoBehaviour
{
    public List<List<float>> TextureToHeightMap(Texture2D texture)
    {
        List<List<float>> heightMap = new List<List<float>>();

        for (int y = 0; y < texture.height; y++)
        {
            List<float> row = new List<float>();
            for (int x = 0; x < texture.width; x++)
            {
                float pixelHeight = texture.GetPixel(x, y).grayscale;
                row.Add(pixelHeight);
            }
            heightMap.Add(row);
        }

        return heightMap;
    }

    public Texture2D HeightMapToTexture(List<List<float>> heightMap)
    {
        Texture2D texture = new Texture2D((int)heightMap.Count, (int)heightMap[0].Count);

        for (int x=0; x<texture.width; x++)
        {
            for (int y=0; y<texture.height; y++)
            {
                texture.SetPixel(x, y, new Color(heightMap[x][y], heightMap[x][y], heightMap[x][y]));
            }
        }
        texture.Apply();

        return texture;
    }

    public void SaveTexture(Texture2D texture, string path)
    {
        System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEditor.AssetDatabase.Refresh();
    }
}
