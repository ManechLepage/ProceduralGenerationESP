using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Threading.Tasks;

public class SaveGraphNode : NodeBehaviour
{
    public string saveFolder = "Resources/";
    public TextMeshProUGUI pathText;
    public SaveGraphManager saveGraphManager;
    
    void Awake()
    {
        saveGraphManager = GetComponent<SaveGraphManager>();
    }

    public override Task<Variant> OnFire()
    {
        return Task.FromResult(new Variant());
    }

    async public Task<string> GetPath()
    {
        string path = "Assets/" + saveFolder + (await GetInputValue("path")).GetValue<string>();

        if (!path.EndsWith(".json"))
            path += ".json";
        
        return path;
    }

    async public void Save()
    {
        string path = await GetPath();

        saveGraphManager.SaveGraph(path);
        UnityEditor.AssetDatabase.Refresh();

        Debug.Log($"Saved the graph to: {path}");
    }
}
