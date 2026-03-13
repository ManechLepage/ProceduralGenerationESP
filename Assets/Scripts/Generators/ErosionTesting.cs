using UnityEngine;
using System.Collections.Generic;

public class ErosionTesting : MonoBehaviour
{
    /*
    Ce fichier s'occupe de tester les différents algorithmes, en particulier l'application d'érosion sur un terrain quelqconque.
    Il est possible de modifier le type de génération : FBM, Voronoi ou à partir d'une texture.

    Ensuite, l'application de l'érosion se fait comme suit :
     - E: érosion hydraulique
     - T: érosion thermique
     - F: érosion fluviale
    
    Autres contrôles :
     - R: réinitialiser le terrain
     - P: sauvegarder le terrain sous format EXR
     - I: activer/désactiver le mode île (rend le terrain plus plat vers les bords et affiche l'océan)
    
    Un mode 'île' est aussi disponible, qui rend le terrain plus plat vers les bords, donnant l'impression d'une île.
    De plus, ce mode affiche l'océan.
    */

    public bool isEnabled = true;

    [Header("Graphic Settings")]
    public Vector2Int textureSize = new Vector2Int(256, 256);
    public Vector2 previewSize = new Vector2(16f, 16f);
    public float terrainHeight = 50f;

    [Header("Algorithm Settings")]
    public AlgorithmType algorithmType = AlgorithmType.FBM;
    public bool island = false;
    public AnimationCurve islandFactor = AnimationCurve.Linear(0f, 1f, 1f, 0.1f);
    public float islandRandomness = 0.1f;
    // public float islandScale = 1f;
    // public float islandFlatness = 5f;
    public Texture2D heightMapTexture;
    public FBMSettings fbmSettings;
    public VoronoiSettings voronoiSettings;
    public HydraulicErosionSettings hydraulicErosionSettings;
    public ThermalErosionSettings thermalErosionSettings;
    public FluvialErosionSettings fluvialErosionSettings;

    [Header("Color Settings")]
    public MeshColorSettings colorSettings;

    [Header("Environment Settings")]
    public GameObject sea;

    private List<List<float>> heightMap;
    private GameObject meshGO;
    private bool didHydraulicErosion = false;
    private bool didThermalErosion = false;
    private bool didFluvialErosion = false;

    void Start()
    {
        if (isEnabled)
        {
            if (island)
                sea.SetActive(true);
            else
                sea.SetActive(false);
            
            // Initialiser le terrain de base
            GenerateBaseTerrain();
            UpdateMesh();
        }
    }

    void Update()
    {
        /*
        Gestion des inputs pour l'application de l'érosion.
        */

        if (!isEnabled) return;

        if (Input.GetKeyDown(KeyCode.E) && !didHydraulicErosion)
        {
            Debug.Log("Hydraulic Erosion started!");
            StartHydraulicErosion();
            UpdateMesh();
            //didHydraulicErosion = true;
        }

        if (Input.GetKeyDown(KeyCode.T) && !didThermalErosion)
        {
            Debug.Log("Thermal erosion started!");
            StartThermalErosion();
            UpdateMesh();
            //didThermalErosion = true;
        }
        
        if (Input.GetKeyDown(KeyCode.F) && !didFluvialErosion)
        {
            Debug.Log("Fluvial erosion started!");
            GameManager.Instance.fluvialErosionAlgorithm.ApplyErosion(heightMap, fluvialErosionSettings);
            UpdateMesh();
            //didFluvialErosion = true;
        }

        bool regenerate = Input.GetKeyDown(KeyCode.R);

        if (Input.GetKeyDown(KeyCode.I))
        {
            island = !island;
            regenerate = true;
        }

        if (regenerate)
        {
            if (island)
                sea.SetActive(true);
            else
                sea.SetActive(false);

            GameManager.Instance.hydraulicErosionAlgorithm.StopAllCoroutines();
            GameManager.Instance.thermalErosionAlgorithm.StopAllCoroutines();
            GenerateBaseTerrain();
            UpdateMesh();
            didHydraulicErosion = false;
            didThermalErosion = false;
            didFluvialErosion = false;
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            // Sauvegarde du terrain sous format EXR.
            Debug.Log("Saving terrain...");
            SaveTerrain();
        }
    }

    public void GenerateBaseTerrain()
    {
        /*
        Générer le terrain avec le mesh selon l'algorithme choisi : FBM, Voronoi ou à partir d'une texture.
        */

        if (algorithmType == AlgorithmType.Texture)
        {
            heightMap = GameManager.Instance.textureHelpers.TextureToHeightMap(heightMapTexture);
        }
        else
        {
            if (algorithmType == AlgorithmType.Voronoi)
            {
                heightMap = GameManager.Instance.voronoiAlgorithm.GetHeightMapThreading(textureSize, voronoiSettings);
            }
            else
            {
                heightMap = GameManager.Instance.fbmAlgorithm.GetHeightMapThreading(textureSize, fbmSettings);
            }
        }

        if (island)
        {
            // Appliquer une influence radiale pour donner l'impression d'une île.
            TransformToIsland(heightMap);
        }
    }

    public void StartHydraulicErosion()
    {
        GameManager.Instance.hydraulicErosionAlgorithm.ErosionProcess(heightMap, hydraulicErosionSettings, OnProgress);
    }

    public void StartThermalErosion()
    {
        float pixelDistanceFactor = previewSize.x / textureSize.x * 50f / terrainHeight;
        GameManager.Instance.thermalErosionAlgorithm.ErosionProcess(heightMap, thermalErosionSettings, pixelDistanceFactor, OnProgress);
    }

    public void OnProgress(float current, float total)
    {
        /*
        Fonction utilisée comme callback pour les processus d'érosion hydraulique et thermique pour
        pouvoir observer les changements en temps réel.
        */

        float progress = current / total;
        Debug.Log($"Erosion progress: {progress * 100f}%");
        UpdateMesh();
    }

    public void TransformToIsland(List<List<float>> heightMap)
    {
        /*
        Appliquer une fonction d'influence radiale pour rendre le terrain plus plat vers les bords, donnant l'impression d'une île.
        */

        int width = heightMap.Count;
        int height = heightMap[0].Count;
        Vector2 center = new Vector2(width / 2f, height / 2f);
        float maxDistance = width / 2f;
        // float intensityAtMax = 0.1f * islandScale;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Déterminer le facteur de hauteur selon l'emplacement des pixels par rapport au centre.
                float distanceToCenter = Vector2.Distance(new Vector2(x, y), center);
                // float islandFactor = 1f / (1f + (1f / intensityAtMax - 1f) * Mathf.Pow(distanceToCenter / maxDistance, islandFlatness));
                float islandCoefficient = islandFactor.Evaluate((distanceToCenter / maxDistance) + Random.Range(-islandRandomness, islandRandomness));
                heightMap[x][y] *= islandCoefficient;
            }
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

    public void SaveTerrain()
    {
        /*
        Souvegarder le heightmap sous format EXR pour pouvoir l'utiliser comme input de texture pour une future simulation
        */

        Texture2D heightMapTexture = GameManager.Instance.textureHelpers.HeightMapToTexture(heightMap);
        GameManager.Instance.textureHelpers.SaveTexture(heightMapTexture, "Assets/Textures/Erosion/HeightMapTexture.exr", makeReadable: true);
    }
}
