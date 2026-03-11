using UnityEngine;
using System.Collections.Generic;

public class DomainWarping : MonoBehaviour
{
    /*
    Ce fichier teste la technique de domain warping pour générer des terrains plus variés et intéressants à partir de bruit de base.
    Pour le moment, il est le seul fichier qui implémente cet algorithmes mais dans le futur proche il sera disponible ailleurs aussi.
     - R: régénérer le terrain
    */
    
    public NoiseGenerator noiseGenerator;
    public MeshGenerator meshGenerator;
    public TextureHelpers textureHelpers;
    public bool isEnabled = false;

    [Header("Warping Settings")]
    public Vector2 size = new Vector2(256, 256);
    public int octaves = 4;
    public float scale = 20f;
    public float persistence = 0.5f;
    public float lacunarity = 2f;
    [Space]
    public Vector2 warpValues1 = new Vector2(5.2f, 1.3f);
    public Vector2 warpValues2 = new Vector2(1.7f, 9.2f);
    public Vector2 warpValues3 = new Vector2(8.3f, 2.8f);
    public float warpStrength = 4f;

    [Header("Render Settings")]
    public Vector2 terrainSize = new Vector2(16f, 16f);
    public float terrainHeight = 50f;
    [Space]
    public bool autoUpdate = false;
    public float updateInterval = 1f;
    private float accumulatedTime = 0f;

    private GameObject meshGO;

    public void Start()
    {
        if (isEnabled)
        {
            GenerateAndShow();
        }
    }

    public void Update()
    {
        /*
        Régénération du terrain ainsi que l'update automatique si ce paramètre est activé.
         - R: régénérer le terrain
         - Auto Update: si activé, le terrain se régénère automatiquement à intervalle régulier, créant une animation de warping.
        */

        if (Input.GetKeyDown(KeyCode.R))
        {
            if (isEnabled)
                GenerateAndShow();
        }
        
        if (!autoUpdate)
            return;

        accumulatedTime += Time.deltaTime;
        if (accumulatedTime >= updateInterval)
        {
            accumulatedTime = 0f;
            
            warpValues1.x -= updateInterval * 0.01f;
            warpValues1.y += updateInterval * 0.015f;

            warpValues2.x += updateInterval * 0.012f;
            warpValues2.y -= updateInterval * 0.008f;

            warpValues3.x -= updateInterval * 0.009f;
            warpValues3.y += updateInterval * 0.011f;

            if (isEnabled)
                GenerateAndShow();
        }
    }

    public void GenerateAndShow()
    {
        /*
        Générer et afficher le terrain warping.
        */

        List<List<float>> warpedNoiseHeightMap = GenerateWarpedNoise(size);

        Texture2D warpedTexture = textureHelpers.HeightMapToTexture(warpedNoiseHeightMap);
        textureHelpers.SaveTexture(warpedTexture, "Assets/Textures/Warping/WarpedTexture.exr");

        if (meshGO == null)
            meshGO = GameManager.Instance.meshGenerator.CreateMeshObject(transform);

        Mesh mesh = meshGenerator.HeightMapToMesh(warpedNoiseHeightMap, terrainHeight, terrainSize, false);

        if (meshGO == null)
            meshGO = GameManager.Instance.meshGenerator.CreateMeshObject(transform);
        meshGenerator.UpdateMesh(meshGO, mesh, terrainSize);
    }

    public List<List<float>> GenerateWarpedNoise(Vector2 size)
    {
        /*
        Générer un heightmap à partir des valeurs de warping.
        */

        List<List<float>> heightMap = new List<List<float>>();

        for (int x = 0; x < size.x; x++)
        {
            heightMap.Add(new List<float>());
            for (int y = 0; y < size.y; y++)
            {
                float xCoord = (float)x / size.x;
                float yCoord = (float)y / size.y;

                float warpedValue = GetWarpedValue(xCoord, yCoord);
                heightMap[x].Add(warpedValue);
            }
        }

        return heightMap;
    }

    public float GetWarpedValue(float x, float y)
    {
        /*
        Cet algorithme repose sur la notion de composée de fonction.
        On utilise le FBM (Fractal Brownian Motion) pour générer lui-même ses valeurs de position,
        et ensuite on refais le processus une autres fois.

        Chaque fois, on garde la position de départ que l'on additionne d'un décalage
        dicté par le FBM précédent.
        */

        Vector2 warp1 = new Vector2(
            GetNoiseValue(x, y),
            GetNoiseValue(x + warpValues1.x, y + warpValues1.y)
        );

        Vector2 warp2 = new Vector2(
            GetNoiseValue(x + warpStrength * warp1.x + warpValues2.x, y + warpStrength * warp1.y + warpValues2.y),
            GetNoiseValue(x + warpStrength * warp1.x + warpValues3.x, y + warpStrength * warp1.y + warpValues3.y)
        );

        return GetNoiseValue(
            x + warpStrength * warp2.x,
            y + warpStrength * warp2.y
        ) * 1.5f;
    }

    public float GetNoiseValue(float x, float y)
    {
        /*
        Récupérer la valeur de FBM pour les coordonnées données.
        */

        return noiseGenerator.GetNoiseValue(x, y, octaves, scale, persistence, lacunarity, false, null);
    }
}
