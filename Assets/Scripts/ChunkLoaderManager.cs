using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

public class ChunkLoaderManager : MonoBehaviour
{

}

[System.Serializable]
public class ChunkLoader
{
    public float loadDistance = 32f;

    [Space]
    public Vector2Int chunkSize = new Vector2Int(32, 32);
    public Vector2 chunkPhysicalSize = new Vector2(16f, 16f);
    public float height = 50f;
    public Vector2Int chunkOffset = Vector2Int.zero;

    [Space]
    public GameObject chunkPrefab;
    public GameObject chunkParent;

    [HideInInspector]
    public Func<Vector2, Vector2, List<List<float>>> heightMapFunction;  // Arguments: Vector2 size, Vector2 offset

    [HideInInspector]
    public Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();

    public IEnumerator InitializeChunks(Vector2Int position, float scaleFactor = 1f)
    {
        int chunksRadius = Mathf.CeilToInt(loadDistance / chunkPhysicalSize.x);

        for (int i = -chunksRadius; i <= chunksRadius; i++)
        {
            for (int j = -chunksRadius; j <= chunksRadius; j++)
            {
                Vector2Int chunkPos = new Vector2Int(
                    i + position.x,
                    j + position.y
                );

                Chunk chunk = LoadChunk(chunkPos, scaleFactor);
                chunks.Add(chunkPos, chunk);
            }
        }

        yield return new WaitForSeconds(0);
    }

    public IEnumerator UpdateLoadedChunks(Vector2Int position, float scaleFactor = 1f)
    {
        List<Chunk> loadedChunks = new List<Chunk>();

        int chunksRadius = Mathf.CeilToInt(loadDistance / chunkPhysicalSize.x);

        for (int i = -chunksRadius; i <= chunksRadius; i++)
        {
            for (int j = -chunksRadius; j <= chunksRadius; j++)
            {
                Vector2Int chunkPos = new Vector2Int(
                    i + position.x,
                    j + position.y
                );

                if (!chunks.ContainsKey(chunkPos))
                {
                    Chunk newChunk = LoadChunk(chunkPos, scaleFactor);
                    chunks.Add(chunkPos, newChunk);
                    loadedChunks.Add(newChunk);
                }
                else
                {
                    loadedChunks.Add(chunks[chunkPos]);
                }
            }
        }

        List<Vector2Int> chunksToRemove = new List<Vector2Int>();

        foreach (Chunk chunk in chunks.Values)
        {
            if (!loadedChunks.Contains(chunk))
            {
                DeleteChunk(chunk);
                chunksToRemove.Add(chunk.position);
            }
        }

        foreach (Vector2Int chunkPos in chunksToRemove)
        {
            chunks.Remove(chunkPos);
        }

        yield return new WaitForSeconds(0);
    }

    public Chunk LoadChunk(Vector2Int position, float scaleFactor = 1f)
    {
        Vector2 offset = new Vector2(
            position.y,
            position.x
        ) * chunkSize;
        
        List<List<float>> heightMap = heightMapFunction(chunkSize + new Vector2Int(1, 1), offset);
        Mesh mesh = GameManager.Instance.meshGenerator.HeightMapToMesh(heightMap, height / scaleFactor, chunkSize);
        GameObject chunkGO = GameManager.Instance.meshGenerator.CreateMeshObject(chunkParent.transform);
        GameManager.Instance.meshGenerator.UpdateMesh(chunkGO, mesh, chunkPhysicalSize / chunkSize);
        chunkGO.transform.position = new Vector3(
            position.x * chunkPhysicalSize.x,
            0f,
            position.y * chunkPhysicalSize.y
        );

        Chunk chunk = new Chunk
        {
            position = position,
            meshGO = chunkGO
        };

        return chunk;
    }

    public void ReloadChunks(Vector2Int position, MonoBehaviour runner)
    {
        ClearChunks();
        runner.StartCoroutine(InitializeChunks(position));
    }

    public void UpdateChunks(Vector2Int position, MonoBehaviour runner)
    {
        runner.StartCoroutine(UpdateLoadedChunks(position));
    }

    public void ClearChunks()
    {
        foreach (Chunk chunk in chunks.Values)
        {
            if (chunk.meshGO != null)
            {
                DeleteChunk(chunk);
            }
        }

        chunks.Clear();
    }

    public void DeleteChunk(Chunk chunk)
    {
        if (chunk.meshGO != null)
        {
            GameObject.Destroy(chunk.meshGO);
            chunk.meshGO = null;
        }
    }

    public Vector2Int SnapToChunk(Vector2 position)
    {
        float chunkSizeX = chunkPhysicalSize.x;
        float chunkSizeY = chunkPhysicalSize.y;

        Vector2 centeredPosition = new Vector2(position.x - 0.5f * chunkSizeX, position.y - 0.5f * chunkSizeY);

        return new Vector2Int(
            (int)(Mathf.Round(centeredPosition.x / chunkSizeX) * chunkSizeX),
            (int)(Mathf.Round(centeredPosition.y / chunkSizeY) * chunkSizeY)
        );
    }

    public Vector2Int PositionToChunk(Vector2 position)
    {
        return new Vector2Int(
            (int)(position.x / chunkPhysicalSize.x),
            (int)(position.y / chunkPhysicalSize.y)
        );
    }
}

[System.Serializable]
public class Chunk
{
    public Vector2Int position;
    public GameObject meshGO;
}
