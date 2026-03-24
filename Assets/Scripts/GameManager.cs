using UnityEngine;

/*
Quelques types définis pour pouvoir différencier les algorithmes et les types d'érosion
dans l'inspecteur et dans le code.
*/

public enum AlgorithmType
{
    FBM,
    Voronoi,
    Texture
}

public enum ErosionType
{
    Hydraulic,
    Thermal,
    Fluvial
}

public class GameManager : MonoBehaviour
{
    /*
    Code principal gérant les transitions entre les scènes.
    Le GameManager est toujours présent dans la hiérarchie, dans toutes les scènes.
    */

    public static GameManager Instance;

    [Header("Public Settings")]
    public bool openedUI = false;
    public bool enabledStatistics = false;

    [Header("Panels")]
    public GameObject statisticsPanel;

    [Header("Helpers")]
    public TextureHelpers textureHelpers;
    public AlgorithmHelpers algorithmHelpers;
    public MeshGenerator meshGenerator;

    [Header("Algorithms")]
    public FBMAlgorithm fbmAlgorithm;
    public VoronoiAlgorithm voronoiAlgorithm;
    public HydraulicErosionAlgorithm hydraulicErosionAlgorithm;
    public ThermalErosionAlgorithm thermalErosionAlgorithm;
    public FluvialErosionAlgorithm fluvialErosionAlgorithm;

    void Awake()
    {
        if (Instance == null)
        {
            // S'assurer que cette instance n'est pas supprimée entre les scènes
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }

    void Start()
    {
        if (openedUI) CloseStatisticsPanel();
        else OpenStatisticsPanel();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P) && !openedUI)
        {
            enabledStatistics = !enabledStatistics;
            if (enabledStatistics)
                OpenStatisticsPanel();
            else
                CloseStatisticsPanel();
        }
    }

    public void OpenStatisticsPanel()
    {
        if (statisticsPanel == null) return;
        statisticsPanel.SetActive(true);
    }

    public void CloseStatisticsPanel()
    {
        if (statisticsPanel == null) return;
        statisticsPanel.SetActive(false);
    }

    public void DidCloseUI()
    {
        openedUI = false;
        if (enabledStatistics)
            OpenStatisticsPanel();
    }
    public void DidOpenUI()
    {
        openedUI = true;
        CloseStatisticsPanel();
    }
}
