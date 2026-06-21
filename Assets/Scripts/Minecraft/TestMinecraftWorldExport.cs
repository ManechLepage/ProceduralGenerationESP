using UnityEngine;
using System.Collections.Generic;

public class TestMinecraftWorldExport : MonoBehaviour
{
    public MinecraftConverterSettings minecraftConverterSettings;
    public FBMSettings fbmSettings;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            CreateAndSaveTestWorld();
        }
    }

    public List<List<float>> GenerateHeightMap()
    {
        Vector2Int terrainSize = new Vector2Int(128, 128);
        return GameManager.Instance.fbmAlgorithm.GetHeightMapThreading(terrainSize, fbmSettings);
    }

    public void CreateAndSaveTestWorld(string path="Assets/Worlds/Test00")
    {
        Debug.Log("Saving test world...");

        List<List<float>> heightMap = GenerateHeightMap();

        MinecraftBlockStateConverter minecraftBlockStateConverter = new MinecraftBlockStateConverter();
        WorldExporter.ExportWorld(path, minecraftConverterSettings.size.x, minecraftConverterSettings.height, minecraftConverterSettings.size.y, 
            (chunkX, chunkZ) => minecraftBlockStateConverter.CreateChunkBlockState(heightMap, minecraftConverterSettings, chunkX, chunkZ), 
            worldMinY: -64);

        // WorldExporter.ExportTestWorld(path);

    #if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
    #endif

        Debug.Log("World saved!");
    }
}
