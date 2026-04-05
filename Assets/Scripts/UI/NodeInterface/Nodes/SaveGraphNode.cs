using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class SaveGraphNode : NodeBehaviour
{
    public string saveFolder = "Resources/";
    public TextMeshProUGUI pathText;
    public SaveGraphManager saveGraphManager;

    void Awake()
    {
        saveGraphManager = GetComponent<SaveGraphManager>();
    }

    public override Variant OnFire()
    {
        return new Variant();
    }

    public void Save()
    {
        string path = "Assets/" + saveFolder + GetInputValue("path").GetValue<string>();

        if (!path.EndsWith(".json"))
            path += ".json";
        
        saveGraphManager.SaveGraph(path);
        UnityEditor.AssetDatabase.Refresh();

        Debug.Log($"Saved the graph to: {path}");
    }
}
