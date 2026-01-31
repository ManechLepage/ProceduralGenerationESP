using UnityEngine;
using System.Collections.Generic;

public class SimpleMeshGenerator : MonoBehaviour
{
    public bool enableTest = true;
    public Texture2D testTexture;
    public Vector2 testSize = new Vector2(16f, 16f);
    public GameObject testMeshGO;

    void Start()
    {
        if (enableTest)
        {
            Mesh mesh = TextureToMesh(testTexture, 50f, testSize, true);
            ShowMesh(mesh);
        }
    }

    public Mesh TextureToMesh(Texture2D texture, float height=1f, Vector2 size=default, bool smoothing=false)
    {
        List<List<float>> heightMap = GameManager.Instance.TextureHelpers.TextureToHeightMap(texture, smoothing);
        return HeightMapToMesh(heightMap, height, size);
    }

    public Mesh HeightMapToMesh(List<List<float>> heightMap, float height=1f, Vector2 size=default, bool colorMesh=false)
    {
        if (size == default)
            size = new Vector2(1f, 1f);

        // fill the mesh with the data from the texture2d, using the greyscale as height (and multiply by height)
        // the final mesh size should be size.

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Color> colors = new List<Color>();

        float sizeXPerPixel = size.x / heightMap[0].Count;
        float sizeYPerPixel = size.y / heightMap.Count;

        for (int y = 0; y < heightMap.Count + 1; y++)
        {
            for (int x = 0; x < heightMap[0].Count + 1; x++)
            {
                float mapHeight = heightMap[Mathf.Min(y, heightMap.Count - 1)][Mathf.Min(x, heightMap[0].Count - 1)];
                float pixelHeight = mapHeight * height;
                vertices.Add(new Vector3(x * sizeXPerPixel, pixelHeight, y * sizeYPerPixel));

                if (y < heightMap.Count && x < heightMap[0].Count)
                {
                    int i = y * (heightMap[0].Count + 1) + x;

                    // First triangle
                    triangles.Add(i);
                    triangles.Add(i + heightMap[0].Count + 1);
                    triangles.Add(i + heightMap[0].Count + 2);

                    // Second triangle
                    triangles.Add(i);
                    triangles.Add(i + heightMap[0].Count + 2);
                    triangles.Add(i + 1);
                }

                if (colorMesh)
                {
                    colors.Add(new Color(
                        Random.Range(0, 1f),
                        Random.Range(0, 1f),
                        Random.Range(0, 1f)
                    ));
                    Debug.Log("Pixel Color Added: " + colors[colors.Count - 1]);
                }
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();

        if (colorMesh)
            mesh.colors = colors.ToArray();
        
        mesh.RecalculateNormals();

        return mesh;
    }

    public void ShowMesh(Mesh mesh)
    {
        testMeshGO.GetComponent<MeshFilter>().mesh = mesh;
        testMeshGO.transform.localScale = new Vector3(testSize.x, 1f, testSize.y);
    }
}
