using UnityEngine;
using System.Collections.Generic;

public class DecorationManager : MonoBehaviour
{
    [Header("Settings")]
    public bool enabled = true;
    public DecorationSettings settings;

    [Header("Decoration Variants")]
    public List<GameObject> treeVariants;

    private List<GameObject> placedDecorations = new List<GameObject>();

    public static DecorationManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PopulateMesh(Mesh mesh, Vector3 meshScale, DecorationSettings settings = null)
    {
        if (settings == null)
            settings = this.settings;
        
        Vector2Int meshSize = new Vector2Int((int)Mathf.Sqrt(mesh.vertexCount), (int)Mathf.Sqrt(mesh.vertexCount));
        for (int y = 0; y < meshSize.y; y++)
        for (int x = 0; x < meshSize.x; x++)
        {
            Vector2Int position = new Vector2Int(x, y);

            if (Random.value < settings.density)
            {
                float slope = CalculateSlope(mesh, position, meshSize);
                
                if (slope > settings.maxSlope)
                    continue;
                
                Vector3 worldPosition = mesh.vertices[y * meshSize.x + x];
                worldPosition = Vector3.Scale(worldPosition, meshScale);

                GameObject decorationPrefab = treeVariants[Random.Range(0, treeVariants.Count)];
                GameObject decorationInstance = PlaceDecoration(worldPosition, decorationPrefab);
                placedDecorations.Add(decorationInstance);

                float scale = Random.Range(settings.minScale, settings.maxScale);
                decorationInstance.transform.localScale = new Vector3(scale, scale, scale);
            }
        }
    }

    public GameObject PlaceDecoration(Vector3 worldPosition, GameObject decorationPrefab)
    {
        // The worldPosition represents the bottom of the decoration, we need to adjust it to place the decoration correctly
        Vector3 adjustedPosition = worldPosition + new Vector3(0, decorationPrefab.transform.localScale.y / 2f, 0);
        GameObject decorationInstance = Instantiate(decorationPrefab, adjustedPosition, Quaternion.identity, transform);
        return decorationInstance;
    }

    public void ClearDecorations()
    {
        foreach (GameObject decoration in placedDecorations)
        {
            Destroy(decoration);
        }
        placedDecorations.Clear();
    }

    public float CalculateSlope(Mesh mesh, Vector2Int position, Vector2Int meshSize)
    {
        // Assume the mesh is a square of points separated by 1 unit in x and z, the points varies in y (height)
        // We will calculate the slope at each point by looking at the height difference with its neighbors

        int positionIndex = position.y * meshSize.x + position.x;
        Vector3[] vertices = mesh.vertices;

        float currentHeight = vertices[positionIndex].y;

        int topNeighborIndex = Mathf.Clamp(position.y + 1, 0, meshSize.y - 1) * meshSize.x + position.x;
        int leftNeighborIndex = position.y * meshSize.x + Mathf.Clamp(position.x - 1, 0, meshSize.x - 1);
        int rightNeighborIndex = position.y * meshSize.x + Mathf.Min(position.x + 1, meshSize.x - 1);
        int bottomNeighborIndex = Mathf.Clamp(position.y - 1, 0, meshSize.y - 1) * meshSize.x + position.x;

        float topHeight = vertices[topNeighborIndex].y;
        float bottomHeight = vertices[bottomNeighborIndex].y;
        float leftHeight = vertices[leftNeighborIndex].y;
        float rightHeight = vertices[rightNeighborIndex].y;

        float slopeX = Mathf.Atan2(rightHeight - leftHeight, 2f) * Mathf.Rad2Deg;
        float slopeZ = Mathf.Atan2(topHeight - bottomHeight, 2f) / Mathf.Rad2Deg;

        float slope = Mathf.Sqrt(slopeX * slopeX + slopeZ * slopeZ);

        return slope;
    }
}

[System.Serializable]
public class DecorationSettings
{
    public float density = 0.1f;
    public float minScale = 0.8f;
    public float maxScale = 1.2f;
    public float maxSlope = 30f;
}
