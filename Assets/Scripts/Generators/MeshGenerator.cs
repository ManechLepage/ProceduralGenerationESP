using UnityEngine;
using System.Collections.Generic;

public class MeshGenerator : MonoBehaviour
{
    public bool enableTest = true;
    public int smoothingLevel = 0;
    public float height = 50f;
    public Texture2D testTexture;
    public Vector2 testSize = new Vector2(16f, 16f);

    [Space]
    public GameObject meshPrefab;

    private GameObject testMeshGO;

    void Start()
    {
        if (enableTest)
        {
            testMeshGO = CreateMeshObject(transform);
            Mesh mesh = TextureToMesh(testTexture, height, testSize, smoothingLevel);
            UpdateMesh(testMeshGO, mesh, testSize);
        }
    }

    public Mesh TextureToMesh(Texture2D texture, float height=1f, Vector2 size=default, int smoothing=0)
    {
        List<List<float>> heightMap = GameManager.Instance.textureHelpers.TextureToHeightMap(texture, smoothing);
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

    public GameObject CreateMeshObject(Transform parent)
    {
        GameObject meshGO = Instantiate(meshPrefab, parent);
        meshGO.SetActive(true);
        return meshGO;
    }

    public void UpdateMesh(GameObject meshGO, Mesh mesh, Vector2 size)
    {
        meshGO.GetComponent<MeshFilter>().mesh = mesh;
        meshGO.transform.localScale = new Vector3(size.x, 1f, size.y);
    }
}
