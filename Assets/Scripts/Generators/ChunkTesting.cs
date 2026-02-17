using UnityEngine;
using System.Collections.Generic;

public class ChunkTesting : MonoBehaviour
{
    public bool isEnabled = true;
    public ChunkLoader chunkLoader;
    public FBMSettings fbmSettings;

    [Space]
    public GameObject camera;

    void Start()
    {
        chunkLoader.heightMapFunction = HeightMapFunction;

        if (isEnabled)
        {
            Vector2 gridOrigin = GetChunkOrigin(GetCameraPosition());
            chunkLoader.ReloadChunks(gridOrigin, this);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (isEnabled)
            {
                Vector2 gridOrigin = GetChunkOrigin(GetCameraPosition());
                chunkLoader.ReloadChunks(gridOrigin, this);
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

    Vector2 GetChunkOrigin(Vector2 position)
    {
        float chunkSizeX = chunkLoader.chunkPhysicalSize.x;
        float chunkSizeY = chunkLoader.chunkPhysicalSize.y;
        return new Vector2(
            Mathf.Round(position.x / chunkSizeX) * chunkSizeX,
            Mathf.Round(position.y / chunkSizeY) * chunkSizeY
        );
    }
}
