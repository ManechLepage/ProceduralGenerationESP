using UnityEngine;

public enum AlgorithmType
{
    FBM,
    Voronoi
}

public class GameManager : MonoBehaviour
{
    /*
    Code principal gérant les transitions entre les scènes.
    Le GameManager est toujours présent dans la hiérarchie, dans toutes les scènes.
    */

    public static GameManager Instance;

    [Header("Helpers")]
    public TextureHelpers textureHelpers;
    public AlgorithmHelpers algorithmHelpers;
    public MeshGenerator meshGenerator;

    [Header("Algorithms")]
    public FBMAlgorithm fbmAlgorithm;
    public VoronoiAlgorithm voronoiAlgorithm;

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
}
