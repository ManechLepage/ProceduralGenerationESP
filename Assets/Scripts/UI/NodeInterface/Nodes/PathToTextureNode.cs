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

    public override Variant Fire()
    {
        UpdateTextAndPreview(pathText.text);
        Texture2D texture = GetPathTexture();
        
        if (texture != null)
        {
            return new Variant(texture);
        }
        else
        {
            return new Variant(new Texture2D(2, 2));
        }
    }

    void Update()
    {
        if (lastText != pathText.text)
        {
            lastText = pathText.text;
            UpdateTextAndPreview(lastText);
        }
    }

    string GetFullPath(string text)
    {
        string texturePath = Path.Combine(Application.dataPath, pathRoot + text + ".exr");
        return Path.GetFullPath(texturePath).Trim().Replace("\u200B", "");
    }

    Texture2D GetPathTexture()
    {
        string texturePath = GetFullPath(pathText.text);

        if (File.Exists(texturePath))
        {
            byte[] fileData = File.ReadAllBytes(texturePath);
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(fileData);
            return texture;
        }
        else
        {
            return null;
        }
    }

    public void UpdateTextAndPreview(string text)
    {
        Texture2D texture = GetPathTexture();

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
