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
    private MeshColorSettings defaultColorSettings = new MeshColorSettings
    {
        isEnabled = false,
        slopeGradient = new Gradient()
    };

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

    public Mesh HeightMapToMesh(List<List<float>> heightMap, float height=1f, Vector2 size=default, bool borderNormals=false, MeshColorSettings colorSettings = default)
    {
        if (size == default)
            size = new Vector2(1f, 1f);
        
        if (colorSettings == default)
            colorSettings = defaultColorSettings;

        // fill the mesh with the data from the texture2d, using the greyscale as height (and multiply by height)
        // the final mesh size should be size.

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Color> colors = new List<Color>();
        List<Vector3> normals = new List<Vector3>();

        int startX = borderNormals ? 1 : 0;
        int startY = borderNormals ? 1 : 0;
        int endX = borderNormals ? heightMap[0].Count : heightMap[0].Count + 1;
        int endY = borderNormals ? heightMap.Count : heightMap.Count + 1;

        int verticesPerRow = endX - startX;
        int rows = endY - startY;

        float sizeXPerPixel = size.x / (verticesPerRow - 1f);
        float sizeYPerPixel = size.y / (rows - 1f);

        for (int y = startY; y < endY; y++)
        {
            for (int x = startX; x < endX; x++)
            {
                float pixelHeight = SampleHeightMap(heightMap, x, y, height);
                vertices.Add(new Vector3(x * sizeXPerPixel, pixelHeight, y * sizeYPerPixel));

                // Compute normals
                float hL = SampleHeightMap(heightMap, x - 1, y, height);
                float hR = SampleHeightMap(heightMap, x + 1, y, height);
                float hD = SampleHeightMap(heightMap, x, y - 1, height);
                float hU = SampleHeightMap(heightMap, x, y + 1, height);

                Vector3 normal = new Vector3(hL - hR, 2f, hD - hU).normalized;

                normals.Add(normal);

                int localX = x - startX;
                int localY = y - startY;

                if (localX < verticesPerRow - 1 && localY < rows - 1)
                {
                    int i = localY * verticesPerRow + localX;

                    // First triangle
                    triangles.Add(i);
                    triangles.Add(i + verticesPerRow);
                    triangles.Add(i + verticesPerRow + 1);

                    // Second triangle
                    triangles.Add(i);
                    triangles.Add(i + verticesPerRow + 1);
                    triangles.Add(i + 1);
                }

                if (colorSettings.isEnabled)
                {
                    float slope = Vector3.Angle(normal, Vector3.up) / 90f;

                    colors.Add(colorSettings.slopeGradient.Evaluate(slope));
                }
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();

        mesh.normals = normals.ToArray();

        if (colorSettings.isEnabled)
            mesh.colors = colors.ToArray();

        return mesh;
    }

    float SampleHeightMap(List<List<float>> heightMap, int x, int y, float height)
    {
        x = Mathf.Clamp(x, 0, heightMap[0].Count - 1);
        y = Mathf.Clamp(y, 0, heightMap.Count - 1);

        return heightMap[y][x] * height;
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


[System.Serializable]
public class MeshColorSettings
{
    public bool isEnabled = false;
    public Gradient slopeGradient;
    public Gradient tempGradient;
    public Gradient tempTempGradient;
}
