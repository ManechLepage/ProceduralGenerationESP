using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class SaveTextureNode : NodeBehaviour
{
    public string saveFolder = "Textures/Imported/";
    public TextMeshProUGUI pathText;
    private TextureHelpers textureHelpers;

    void Awake()
    {
        textureHelpers = GetComponent<TextureHelpers>();
    }

    public override Variant OnFire()
    {
        return new Variant();
    }

    public void Save()
    {
        if (!GetInputConnection("texture").IsConnected())
            return;

        Texture2D texture = GetInputValue("texture", onlyIfModified: true).GetValue<Texture2D>();
        if (texture == null)
            return;

        string path = "Assets/" + saveFolder + GetInputValue("path").GetValue<string>();

        if (!path.EndsWith(".exr"))
            path += ".exr";
        
        try
        {
            textureHelpers.SaveTexture(texture, path, refreshAssetDatabase: true, makeReadable: true);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to save texture: " + e.Message);
            pathText.color = Color.red;
            return;
        }
        
        pathText.color = Color.white;
        Debug.Log("Texture saved to: " + path);
    }

    public Texture2D ConvertToEXR(Texture2D texture)
    {
        if (texture.format == TextureFormat.RGBAFloat)
            return texture;

        Texture2D exrTexture = new Texture2D(texture.width, texture.height, TextureFormat.RGBAFloat, false);
        Color[] pixels = texture.GetPixels();
        exrTexture.SetPixels(pixels);
        return exrTexture;
    }
}
