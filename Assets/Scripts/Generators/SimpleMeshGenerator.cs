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
            Mesh mesh = TextureToMesh(testTexture, 50f, testSize);
            ShowMesh(mesh);
        }
    }

    public Mesh TextureToMesh(Texture2D texture, float height=1f, Vector2 size=default)
    {
        if (size == default)
            size = new Vector2(1f, 1f);

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        float sizeXPerPixel = size.x / texture.width;
        float sizeYPerPixel = size.y / texture.height;

        for (int x = 0; x < texture.width + 1; x++)
        {
            for (int y = 0; y < texture.height + 1; y++)
            {
                float pixelHeight = texture.GetPixel(x, y).grayscale * height;
                vertices.Add(new Vector3(x * sizeXPerPixel, -pixelHeight, y * sizeYPerPixel));

                if (y < texture.height && x < texture.width)
                {
                    int i = x * texture.width + x + y;

                    // Premier triangle
                    triangles.Add(i);
                    triangles.Add(i + texture.width + 1);
                    triangles.Add(i + texture.width + 2);

                    // Deuxième triangle
                    triangles.Add(i);
                    triangles.Add(i + texture.width + 2);
                    triangles.Add(i + 1);
                }
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        return mesh;
    }

    public void ShowMesh(Mesh mesh)
    {
        testMeshGO.GetComponent<MeshFilter>().mesh = mesh;
        testMeshGO.transform.localScale = new Vector3(testSize.x, 1f, testSize.y);
    }
}
