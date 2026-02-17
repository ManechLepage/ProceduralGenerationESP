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
    public float debugScaleFactor = 1f;

    [Space]
    public GameObject chunkPrefab;
    public GameObject chunkParent;

    [HideInInspector]
    public Func<Vector2, Vector2, List<List<float>>> heightMapFunction;  // Arguments: Vector2 size, Vector2 offset

    [HideInInspector]
    public List<List<Chunk>> chunks = new List<List<Chunk>>();

    public IEnumerator InitializeChunks(Vector2 position, float scaleFactor = 1f)
    {
        int chunksRadius = Mathf.CeilToInt(loadDistance / chunkPhysicalSize.x);

        Vector3 initialPosition = new Vector3(position.x, 0f, position.y) - new Vector3(0.5f * chunkPhysicalSize.x, 0f, 0.5f * chunkPhysicalSize.y);

        for (int i = -chunksRadius; i <= chunksRadius; i++)
        {
            chunks.Add(new List<Chunk>());
            for (int j = -chunksRadius; j <= chunksRadius; j++)
            {
                Vector2 offset = new Vector2(i, j);
                Chunk chunk = CreateChunk(offset, scaleFactor);

                Vector3 positionOffset = new Vector3(i * chunkPhysicalSize.x, 0f, j * chunkPhysicalSize.y);
                chunk.meshGO.transform.position = positionOffset + initialPosition;

                chunks[chunks.Count - 1].Add(chunk);

                //Debug.Log($"Chunk {i} {j}, offset: {offset}, size: {chunkSize}");
            }
        }

        yield return new WaitForSeconds(0);
    }

    public IEnumerator UpdateLoadedChunks(Vector2 position)
    {
        yield return new WaitForSeconds(0);
    }

    public Chunk CreateChunk(Vector2 offset, float scaleFactor = 1f)
    {
        List<List<float>> heightMap = heightMapFunction(chunkSize, offset);
        Mesh mesh = GameManager.Instance.meshGenerator.HeightMapToMesh(heightMap, height / scaleFactor, chunkSize);
        GameObject chunkGO = GameManager.Instance.meshGenerator.CreateMeshObject(chunkParent.transform);
        GameManager.Instance.meshGenerator.UpdateMesh(chunkGO, mesh, chunkPhysicalSize / chunkSize);

        Chunk chunk = new Chunk
        {
            offset = offset,
            meshGO = chunkGO
        };

        return chunk;
    }

    public void ReloadChunks(Vector2 position, MonoBehaviour runner)
    {
        ClearChunks();
        runner.StartCoroutine(InitializeChunks(position));
    }

    public void ClearChunks()
    {
        foreach (List<Chunk> chunkRow in chunks)
        {
            foreach (Chunk chunk in chunkRow)
            {
                if (chunk.meshGO != null)
                {
                    DeleteChunk(chunk);
                }
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
}

[System.Serializable]
public class Chunk
{
    public Vector2 offset;
    public GameObject meshGO;
}
