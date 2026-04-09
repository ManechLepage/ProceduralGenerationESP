using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Threading.Tasks;

public class LoadGraphNode : NodeBehaviour
{
    public string loadFolder = "Resources/";
    public TextMeshProUGUI pathText;
    public LoadGraphManager loadGraphManager;

    private string lastText = "";

    void Awake()
    {
        loadGraphManager = GetComponent<LoadGraphManager>();
    }

    public override Task<Variant> OnFire()
    {
        return Task.FromResult(new Variant());
    }

    async public void Update()
    {
        if (lastText != pathText.text)
        {
            lastText = pathText.text;
            await UpdateText(lastText);
        }
    }

    async public Task<string> GetPath()
    {
        string path = "Assets/" + loadFolder + (await GetInputValue("path")).GetValue<string>();

        if (!path.EndsWith(".json"))
            path += ".json";
        
        return path;
    }

    async public void Load()
    {
        string path = await GetPath();

        try
        {
            loadGraphManager.LoadGraph(path);
        }
        catch (System.Exception)
        {
            Debug.Log("Failed to load graph.");
            return;
        }

        Debug.Log($"Loaded the graph from: {path}");
    }

    async public Task UpdateText(string text)
    {
        string path = (await GetInputValue("path")).GetValue<string>();
        path = loadFolder.Replace("Resources/", "") + path;  // Enlever "Resources/" du chemin pour l'affichage

        // Regarder si le fichier existe dans les ressources
        var asset = Resources.Load<TextAsset>(path);

        if (asset != null)
            pathText.color = Color.green;
        else
            pathText.color = Color.red;
    }
}
