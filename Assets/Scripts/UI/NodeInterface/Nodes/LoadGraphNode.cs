using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Threading.Tasks;

public class LoadGraphNode : NodeBehaviour
{
    public string loadFolder = "Resources/";
    public TextMeshProUGUI pathText;
    public LoadGraphManager loadGraphManager;

    void Awake()
    {
        loadGraphManager = GetComponent<LoadGraphManager>();
    }

    public override Task<Variant> OnFire()
    {
        return Task.FromResult(new Variant());
    }

    async public void Load()
    {
        string path = "Assets/" + loadFolder + (await GetInputValue("path")).GetValue<string>();

        if (!path.EndsWith(".json"))
            path += ".json";

        try
        {
            loadGraphManager.LoadGraph(path);
        }
        catch (System.Exception)
        {
            Debug.Log("Failed to load graph.");
            pathText.color = Color.red;
            return;
        }
        
        pathText.color = Color.white;
        Debug.Log($"Loaded the graph from: {path}");
    }
}
