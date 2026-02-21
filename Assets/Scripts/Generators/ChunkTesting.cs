using UnityEngine;
using System.Collections.Generic;

public class ChunkTesting : MonoBehaviour
{
    public bool isEnabled = true;
    public ChunkLoader chunkLoader;

    public AlgorithmType algorithmType = AlgorithmType.FBM;
    public FBMSettings fbmSettings;
    public VoronoiSettings voronoiSettings;

    [Space]
    public GameObject mainCamera;
    private Vector2Int lastGridOrigin = Vector2Int.zero;

    private AlgorithmType lastAlgorithmType;

    void Start()
    {
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
        lastGridOrigin = chunkLoader.SnapToChunk(GetCameraPosition());
        chunkLoader.chunkOffset = lastGridOrigin;
        chunkLoader.ReloadChunks(
            chunkLoader.PositionToChunk(lastGridOrigin),
            this
        );
    }

    public List<List<float>> HeightMapFunction(Vector2 size, Vector2 offset)
    {
        List<List<float>> heightMap;
        if (algorithmType == AlgorithmType.FBM)
        {
            fbmSettings.offset += offset;
            heightMap = GameManager.Instance.fbmAlgorithm.GetHeightMap(size, fbmSettings);
            fbmSettings.offset -= offset;
        }
        else
        {
            voronoiSettings.offset += offset;
            heightMap = GameManager.Instance.voronoiAlgorithm.GetHeightMap(size, voronoiSettings);
            voronoiSettings.offset -= offset;
        }

        return heightMap;
    }

    Vector2 GetCameraPosition()
    {
        return new Vector2(mainCamera.transform.position.x, mainCamera.transform.position.z);
    }
}
