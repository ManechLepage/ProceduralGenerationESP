using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

public class PathToTextureNode : NodeBehaviour
{
    public RawImage preview;
    public TextMeshProUGUI pathText;
    public string pathRoot = "Textures/Imported/";

    private string lastText = "";

    void Update()
    {
        if (lastText != pathText.text)
        {
            lastText = pathText.text;
            UpdateTextAndPreview(lastText);
        }
    }

    public void UpdateTextAndPreview(string text)
    {
        string texturePath = Path.Combine(Application.dataPath, pathRoot + text + ".exr");
        texturePath = Path.GetFullPath(texturePath);
        texturePath = texturePath.Trim().Replace("\u200B", "");

        Texture2D texture = null;
        if (File.Exists(texturePath))
        {
            byte[] fileData = File.ReadAllBytes(texturePath);
            texture = new Texture2D(2, 2);
            texture.LoadImage(fileData);
        }

        if (texture != null)
        {
            preview.texture = texture;
            pathText.color = Color.green;
        }
        else
        {
            preview.texture = null;
            pathText.color = Color.red;
        }
    }
}
