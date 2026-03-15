using UnityEngine;
using System.Collections.Generic;   

public class TerrainManager : MonoBehaviour
{
    [Header("General Settings")]
    public Vector2 previewSize = new Vector2(16f, 16f);
    public float terrainHeight = 50f;
    

    [Header("Color Settings")]
    public MeshColorSettings colorSettings;

    [Header("Environment Settings")]
    public GameObject sea;

    private List<List<float>> heightMap;
    private GameObject meshGO;
    private MasterNode masterNode;
    private float initialTerrainHeight;

    void Start()
    {
        initialTerrainHeight = terrainHeight;

        masterNode = GraphManager.Instance.masterNode as MasterNode;
        if (masterNode != null)
        {
            masterNode.onFire.AddListener(Generate);
        }
    }

    public void Generate()
    {
        /*
        Générer le terrain à partir du master node, puis mettre à jour le mesh.
        */

        heightMap = masterNode.GetInputValue("heightmap").GetValue<List<List<float>>>();
        terrainHeight = initialTerrainHeight * masterNode.GetInputValue("height").GetValue<float>();

        if (heightMap.Count > 0)
        {
            UpdateMesh();
        }
    }

    public void UpdateMesh()
    {
        /*
        Régénérer le mesh à partir du heightmap et l'appliquer au GameObject. Si le GameObject n'existe pas encore, le créer.
        */

        if (meshGO == null)
            meshGO = GameManager.Instance.meshGenerator.CreateMeshObject(transform, colorSettings.isEnabled);
        
        Mesh mesh = GameManager.Instance.meshGenerator.HeightMapToMesh(heightMap, terrainHeight, previewSize, false, colorSettings, lowBorders: true);
        GameManager.Instance.meshGenerator.UpdateMesh(meshGO, mesh, previewSize);
    }
}
