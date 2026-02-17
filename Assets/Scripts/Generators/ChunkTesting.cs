using UnityEngine;
using System.Collections.Generic;

public class ChunkTesting : MonoBehaviour
{
    public bool isEnabled = true;
    public ChunkLoader chunkLoader;
    public FBMSettings fbmSettings;

    [Space]
    public GameObject camera;
    private Vector2Int lastGridOrigin = Vector2Int.zero;

    void Start()
    {
        camera.transform.position = new Vector3(0f, 50f, 0f);

        chunkLoader.heightMapFunction = HeightMapFunction;
        lastGridOrigin = GetChunkOrigin(GetCameraPosition());

        if (isEnabled)
        {
            chunkLoader.chunkOffset = lastGridOrigin;
            chunkLoader.ReloadChunks(lastGridOrigin, this);
        }
    }

    void Update()
    {
        Vector2Int gridOrigin = GetChunkOrigin(GetCameraPosition());

        if (Input.GetKeyDown(KeyCode.R) || lastGridOrigin != gridOrigin)
        {
            if (isEnabled)
            {
                lastGridOrigin = gridOrigin;

                chunkLoader.chunkOffset = lastGridOrigin;
                chunkLoader.ReloadChunks(lastGridOrigin, this);
            }
        }
    }

    public List<List<float>> HeightMapFunction(Vector2 size, Vector2 offset)
    {
        fbmSettings.offset += offset;
        List<List<float>> heightMap = GameManager.Instance.fbmAlgorithm.GetHeightMap(size, fbmSettings);
        fbmSettings.offset -= offset;
        return heightMap;
    }

    Vector2 GetCameraPosition()
    {
        return new Vector2(camera.transform.position.x, camera.transform.position.z);
    }

    Vector2Int GetChunkOrigin(Vector2 position)
    {
        float chunkSizeX = chunkLoader.chunkPhysicalSize.x;
        float chunkSizeY = chunkLoader.chunkPhysicalSize.y;

        Vector2 centeredPosition = new Vector2(position.x - 0.5f * chunkSizeX, position.y - 0.5f * chunkSizeY);

        return new Vector2Int(
            (int)(Mathf.Round(centeredPosition.x / chunkSizeX) * chunkSizeX),
            (int)(Mathf.Round(centeredPosition.y / chunkSizeY) * chunkSizeY)
        );
    }
}
