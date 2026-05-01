using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

public class ChunkTesting : MonoBehaviour
{
    /*
    Ce fichier sert à tester le ChunkLoader en générant du terrain infini.
    */

    public bool isEnabled = true;
    public AlgorithmType algorithmType = AlgorithmType.FBM;

    [Header("Chunk Settings")]
    public ChunkLoader chunkLoader;

    [Header("Algorithm Settings")]
    public FBMSettings fbmSettings;
    public VoronoiSettings voronoiSettings;

    [Header("Other References")]
    public GameObject mainCamera;

    private Vector2Int lastGridOrigin = Vector2Int.zero;

    private AlgorithmType lastAlgorithmType;

    void Start()
    {
        /*
        Initialiser le terrain et les paramètres.
        */

        lastAlgorithmType = algorithmType;

        mainCamera.transform.position = new Vector3(0f, 50f, 0f);

        chunkLoader.heightMapFunction = HeightMapFunction;
        lastGridOrigin = chunkLoader.SnapToChunk(GetCameraPosition());

        if (isEnabled)
        {
            chunkLoader.chunkOffset = lastGridOrigin;
            chunkLoader.UpdateChunks(
                chunkLoader.PositionToChunk(lastGridOrigin),
                this
            );
        }
    }

    void Update()
    {
        /*
        Si l'utilisateur a changé de chunk, on met à jour les chunks. Si l'utilisateur a changé d'algorithme, on recharge tous les chunks.
        */
        
        Vector2Int gridOrigin = chunkLoader.SnapToChunk(GetCameraPosition());

        if (lastAlgorithmType != algorithmType)
        {
            ReloadChunks();
            lastAlgorithmType = algorithmType;
        }
        else if (Input.GetKeyDown(KeyCode.R) || lastGridOrigin != gridOrigin)
        {
            if (isEnabled)
            {
                lastGridOrigin = gridOrigin;
                chunkLoader.chunkOffset = lastGridOrigin;
                chunkLoader.UpdateChunks(
                    chunkLoader.PositionToChunk(lastGridOrigin),
                    this
                );
            }
        }
    }

    public void ReloadChunks()
    {
        /*
        Recharger tous les chunks.
        */
        
        lastGridOrigin = chunkLoader.SnapToChunk(GetCameraPosition());
        chunkLoader.chunkOffset = lastGridOrigin;
        chunkLoader.ReloadChunks(
            chunkLoader.PositionToChunk(lastGridOrigin),
            this
        );
    }

    public Task<List<List<float>>> HeightMapFunction(Vector2 size, Vector2 offset, float scale=1f)
    {
        /*
        Fonction qui va être appelée par le ChunkLoader pour générer les chunks.
        On retourne une heightmap dépendant de l'algorithme choisi et des paramètres définis dans l'inspecteur.
        */
        
        List<List<float>> heightMap;
        if (algorithmType == AlgorithmType.FBM)
        {
            fbmSettings.offset += offset;
            fbmSettings.scale *= scale;
            heightMap = GameManager.Instance.fbmAlgorithm.GetHeightMapThreading(size, fbmSettings);
            fbmSettings.offset -= offset;
            fbmSettings.scale /= scale;
        }
        else
        {
            voronoiSettings.offset += offset;
            voronoiSettings.scale *= scale;
            heightMap = GameManager.Instance.voronoiAlgorithm.GetHeightMapThreading(size, voronoiSettings);
            voronoiSettings.offset -= offset;
            voronoiSettings.scale /= scale;
        }

        return Task.FromResult(heightMap);
    }

    Vector2 GetCameraPosition()
    {
        return new Vector2(mainCamera.transform.position.x, mainCamera.transform.position.z);
    }
}
