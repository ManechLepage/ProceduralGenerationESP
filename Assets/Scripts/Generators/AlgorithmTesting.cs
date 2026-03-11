using UnityEngine;
using System.Collections.Generic;

public class AlgorithmTesting : MonoBehaviour
{
    /*
    Fichier de test pour évaluer le fonctionnement des algorithmes implémentés en donnant le choix à l'utilisateur
    de divers paramètres de génération.
    Cette version permet également de voir les changements de paramètres en temps réel, ce qui est très utile pour le développement et l'ajustement des algorithmes.
    */

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
        /*
        Initialiser les paramètres de génération et créer le terrain si ce fichier est activé.
        */
        
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
        /*
        Mettre à jour le terrain en fonction des paramètres de génération.
        Contrôles :
         - F : Générer manuellement le terrain avec les paramètres actuels.
         - Auto Update : Générer automatiquement le terrain à intervalles réguliers si les paramètres ont changé.
        */
        
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
        /*
        Générer le terrain de Fractal Brownian Motion (FBM) en utilisant les paramètres dans l'inspecteur.
        Convertir le terrain en mesh pour ensuite l'afficher et le sauvegarder.
        */

        List<List<float>> fbmHeightMap = GameManager.Instance.fbmAlgorithm.GetHeightMap(pixelSize, fbmSettings);
        Mesh mesh = GameManager.Instance.meshGenerator.HeightMapToMesh(fbmHeightMap, height / fbmSettings.scale, physicalSize, false);
        
        UpdateMesh(mesh);
        SaveHeightMap(fbmHeightMap, "fbm_heightmap.exr");
    }

    public void GenerateVoronoi()
    {
        /*
        Comme la fonction précédenter, générer, afficher et sauvegarder le terrain de Voronoi en utilisant les paramètres dans l'inspecteur.
        */

        List<List<float>> voronoiHeightMap = GameManager.Instance.voronoiAlgorithm.GetHeightMap(pixelSize, voronoiSettings);
        Mesh mesh = GameManager.Instance.meshGenerator.HeightMapToMesh(voronoiHeightMap, height / voronoiSettings.scale, physicalSize, false);
        
        UpdateMesh(mesh);
        SaveHeightMap(voronoiHeightMap, "voronoi_heightmap.exr");
    }

    void UpdateMesh(Mesh mesh)
    {
        /*
        Mettre à jour le mesh du terrain.
         - 'mesh' : Le nouveau mesh à afficher.
        */

        if (meshGO == null)
            meshGO = GameManager.Instance.meshGenerator.CreateMeshObject(transform);
        
        GameManager.Instance.meshGenerator.UpdateMesh(meshGO, mesh, physicalSize);
    }

    void SaveHeightMap(List<List<float>> heightMap, string path)
    {
        /*
        Sauvegarder le heightmap en tant que texture EXR pour une visualisation et une analyse ultérieures.
         - 'heightMap' : La heightmap à sauvegarder.
         - 'path' : Le chemin de sauvegarde relatif à Assets/Textures/Previews/.
        */

        Texture2D texture = GameManager.Instance.textureHelpers.HeightMapToTexture(heightMap);
        GameManager.Instance.textureHelpers.SaveTexture(texture, Application.dataPath + "/Textures/Previews/" + path);
    }
}
