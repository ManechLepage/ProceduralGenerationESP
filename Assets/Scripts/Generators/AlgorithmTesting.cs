using UnityEngine;
using System.Collections.Generic;

public class AlgorithmTesting : MonoBehaviour
{
    public bool isEnabled = false;
    public Vector2 pixelSize = new Vector2(256, 256);
    public Vector2 physicalSize = new Vector2(16f, 16f);
    public float height = 50f;

    [Header("Settings")]
    public AlgorithmType algorithmType = AlgorithmType.FBM;
    public FBMSettings fbmSettings;
    public VoronoiSettings voronoiSettings;

    [Header("Animation")]
    public bool autoUpdate = false;
    public float updateInterval = 0.1f;
    private float accumulatedTime = 0f;

    private FBMSettings lastFBMSettings;
    private VoronoiSettings lastVoronoiSettings;
    private float lastHeight;

    private GameObject meshGO;

    void Start()
    {
        lastFBMSettings = fbmSettings.GetCopy();
        lastVoronoiSettings = voronoiSettings.GetCopy();
        lastHeight = height;
        if (isEnabled)
        {
            meshGO = GameManager.Instance.meshGenerator.CreateMeshObject(transform);
            if (algorithmType == AlgorithmType.FBM)
                GenerateFBM();
            else
                GenerateVoronoi();
        }
    }

    void Update()
    {
        if (isEnabled)
        {
            
            accumulatedTime += Time.deltaTime;
            if (autoUpdate && accumulatedTime >= updateInterval && (!fbmSettings.SameSettings(lastFBMSettings) || !voronoiSettings.SameSettings(lastVoronoiSettings) || lastHeight != height))
            {
                accumulatedTime = 0f;
                if (algorithmType == AlgorithmType.FBM)
                {
                    GenerateFBM();
                    lastFBMSettings = fbmSettings.GetCopy();
                }
                else
                {
                    GenerateVoronoi();
                    lastVoronoiSettings = voronoiSettings.GetCopy();

                }

                lastHeight = height;
            }
            else if (Input.GetKeyDown(KeyCode.F))
            {
                if (algorithmType == AlgorithmType.FBM)
                    GenerateFBM();
                else
                    GenerateVoronoi();
            }
        }
    }

    public void GenerateFBM()
    {
        List<List<float>> fbmHeightMap = GameManager.Instance.fbmAlgorithm.GetHeightMap(pixelSize, fbmSettings);
        Mesh mesh = GameManager.Instance.meshGenerator.HeightMapToMesh(fbmHeightMap, height / fbmSettings.scale, physicalSize, false);
        
        UpdateMesh(mesh);
        SaveHeightMap(fbmHeightMap, "fbm_heightmap.exr");
    }

    public void GenerateVoronoi()
    {
        List<List<float>> voronoiHeightMap = GameManager.Instance.voronoiAlgorithm.GetHeightMap(pixelSize, voronoiSettings);
        Mesh mesh = GameManager.Instance.meshGenerator.HeightMapToMesh(voronoiHeightMap, height / voronoiSettings.scale, physicalSize, false);
        
        UpdateMesh(mesh);
        SaveHeightMap(voronoiHeightMap, "voronoi_heightmap.exr");
    }

    void UpdateMesh(Mesh mesh)
    {
        if (meshGO == null)
            meshGO = GameManager.Instance.meshGenerator.CreateMeshObject(transform);
        
        GameManager.Instance.meshGenerator.UpdateMesh(meshGO, mesh, physicalSize);
    }

    void SaveHeightMap(List<List<float>> heightMap, string path)
    {
        Texture2D texture = GameManager.Instance.textureHelpers.HeightMapToTexture(heightMap);
        GameManager.Instance.textureHelpers.SaveTexture(texture, Application.dataPath + "/Textures/Previews/" + path);
    }
}
