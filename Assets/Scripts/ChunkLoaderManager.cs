using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Threading.Tasks;

public class ChunkLoaderManager : MonoBehaviour
{

}

[System.Serializable]
public class ChunkLoader
{
    /*
    Cette classe permet de générer un terrain continu et infini en utilisant la méthode de chunks.
    Cette méthode permet de générer plusieurs mesh continus les uns à côté des autres dans un rayon autour de l'utilisateur et permet
    de faire disparaitre les chunks qui sont trop éloignés pour économiser des ressources. 
    */
    
    public float loadDistance = 32f;
    [Space]
    public Vector2Int chunkSize = new Vector2Int(32, 32);
    public Vector2 chunkPhysicalSize = new Vector2(16f, 16f);
    public float height = 50f;
    public Vector2Int chunkOffset = Vector2Int.zero;

    [Space]
    public MeshColorSettings colorSettings = new MeshColorSettings();

    [Space]
    public GameObject chunkPrefab;
    public GameObject chunkParent;

    [HideInInspector]
    public Func<Vector2, Vector2, float, Task<List<List<float>>>> heightMapFunction;  // Arguments: Vector2 size, Vector2 offset, float scale

    [HideInInspector]
    public Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();

    async public void InitializeChunks(Vector2Int position, float scaleFactor = 1f, bool circular = true)
    {
        /*
        Loader une première fois tous les chunks autour de l'utilisateur à partir de zero.
        */
        
        int chunksRadius = Mathf.CeilToInt(loadDistance / chunkPhysicalSize.x);

        for (int i = -chunksRadius; i <= chunksRadius; i++)
        {
            for (int j = -chunksRadius; j <= chunksRadius; j++)
            {
                if (circular && new Vector2(i, j).magnitude * chunkPhysicalSize.x > loadDistance)
                    continue;

                Vector2Int chunkPos = new Vector2Int(
                    i + position.x,
                    j + position.y
                );

                Chunk chunk = await LoadChunk(chunkPos, scaleFactor);
                if (chunk != null && !chunks.ContainsKey(chunkPos))
                    chunks.Add(chunkPos, chunk);
                // else
                //     Debug.Log($"Failed to load chunk at position {chunkPos}");
            }
        }
    }

    async public void UpdateLoadedChunks(Vector2Int position, float scaleFactor = 1f, bool circular = true)
    {
        /*
        Loader les chunks qui ne l'ont pas encore été et unloader ceux qui sont trop éloignés à partir de la position donnée.
         - 'position' : position centrale à partir de laquelle charger les chunks
         - 'scaleFactor' : facteur d'échelle pour la hauteur des chunks
         - 'circular' : si true, charger les chunks dans un rayon circulaire, sinon dans un carré
        */
        
        List<Chunk> loadedChunks = new List<Chunk>();

        int chunksRadius = Mathf.CeilToInt(loadDistance / chunkPhysicalSize.x);

        for (int i = -chunksRadius; i <= chunksRadius; i++)
        {
            for (int j = -chunksRadius; j <= chunksRadius; j++)
            {
                if (circular && new Vector2(i, j).magnitude * chunkPhysicalSize.x > loadDistance)
                    continue;

                Vector2Int chunkPos = new Vector2Int(
                    i + position.x,
                    j + position.y
                );

                if (!chunks.ContainsKey(chunkPos))
                {
                    Chunk newChunk = await LoadChunk(chunkPos, scaleFactor);
                    if (newChunk != null && !chunks.ContainsKey(chunkPos))
                    {
                        chunks.Add(chunkPos, newChunk);
                        loadedChunks.Add(newChunk);
                    }
                    else
                        Debug.Log($"Failed to load chunk at position {chunkPos}");
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

        // Effacer les chunks après avoir fait une boucle dedans pour éviter de modifier la collection pendant qu'on itère dessus
        foreach (Vector2Int chunkPos in chunksToRemove)
        {
            chunks.Remove(chunkPos);
        }
    }

    async public Task<Chunk> LoadChunk(Vector2Int position, float scaleFactor = 1f, bool animate = true)
    {
        /*
        Méthode pour loader un unique chunk à une certaine position.
         - 'position' : position du chunk à charger
         - 'scaleFactor' : facteur d'échelle pour la hauteur du chunk
         - 'animate' : si true, animer l'apparition du chunk (non implémenté)
         - return : le chunk chargé
        */
        
        Vector2 offset = new Vector2(
            position.y,
            position.x
        ) * chunkSize;
        
        float scale = chunkPhysicalSize.x / 32f;

        // Loader le heightmap correspondant à ce chunk
        List<List<float>> heightMap = await heightMapFunction(chunkSize + new Vector2Int(2, 2), offset - new Vector2(1, 1) * chunkSize, scale);

        if (heightMap == null || heightMap.Count == 0)
            return null;

        // Générer le mesh à partir du heightmap et créer le GameObject correspondant
        Mesh mesh = GameManager.Instance.meshGenerator.HeightMapToMesh(heightMap, height / scaleFactor, chunkSize, borderNormals: true, lowBorders: false, colorSettings: colorSettings);
        GameObject chunkGO = GameManager.Instance.meshGenerator.CreateMeshObject(chunkParent.transform, colorSettings.isEnabled);

        GameManager.Instance.meshGenerator.UpdateMesh(chunkGO, mesh, chunkPhysicalSize / chunkSize);

        // Changer la taille du chunk pour qu'il corresponde à la taille physique désirée
        chunkGO.transform.position = new Vector3(
            position.x * chunkPhysicalSize.x,
            0f,
            position.y * chunkPhysicalSize.y
        );

        chunkGO.name = $"Chunk ({position.x}, {position.y})";

        Chunk chunk = new Chunk
        {
            position = position,
            meshGO = chunkGO
        };

        return chunk;
    }

    public void ReloadChunks(Vector2Int position, MonoBehaviour runner)
    {
        /*
        Effacer tous les chunks et tout reloader
        */
        
        ClearChunks();
        InitializeChunks(position);
    }

    public void UpdateChunks(Vector2Int position, MonoBehaviour runner)
    {
        /*
        Mettre à jour les chunks chargés en fonction de la position donnée.
         - 'position' : position centrale à partir de laquelle charger les chunks
         - 'runner' : MonoBehaviour pour pouvoir lancer la coroutine d'update des chunks
         - return : void
        */
        
        UpdateLoadedChunks(position);
    }

    public void ClearChunks()
    {
        /*
        Détruire tous les chunks
        */
        
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
        /*
        Détruire le GameObject d'un chunk en particulier.
        */
        
        if (chunk.meshGO != null)
        {
            GameObject.Destroy(chunk.meshGO);
            chunk.meshGO = null;
        }
    }

    public Vector2Int SnapToChunk(Vector2 position)
    {
        /*
        Arrondir vers le bas une position donnée pour trouver la position du chunk correspondant.
         - 'position' : position à arrondir
         - return : position du chunk correspondant
        */
        
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
        /*
        Convertir une position donnée en position de chunk.
         - 'position' : position à convertir
         - return : position du chunk correspondant
        */

        return new Vector2Int(
            (int)(position.x / chunkPhysicalSize.x),
            (int)(position.y / chunkPhysicalSize.y)
        );
    }
}

[System.Serializable]
public class Chunk
{
    /*
    Structure pour stocker les informations d'un chunk, notamment sa position et le GameObject de son mesh.
    */
        
    public Vector2Int position;  // position du chunk dans la grille de chunks
    public GameObject meshGO;  // GameObject du mesh du chunk
}
