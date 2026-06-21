using UnityEngine;

public class TestMinecraftWorldExport : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            CreateAndSaveTestWorld();
        }
    }

    public void CreateAndSaveTestWorld(string path="Assets/Worlds/Test")
    {
        Debug.Log("Saving test world...");

        WorldExporter.ExportTestWorld(path);

    #if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
    #endif

        Debug.Log("World saved!");
    }
}
