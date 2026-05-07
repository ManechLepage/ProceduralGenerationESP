using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Threading.Tasks;

public class SaveTextureNode : NodeBehaviour
{
    public string saveFolder = "Textures/Imported/";
    public TextMeshProUGUI pathText;
    private TextureHelpers textureHelpers;

    void Awake()
    {
        textureHelpers = GetComponent<TextureHelpers>();
    }

    public override Task<Variant> OnFire()
    {
        return Task.FromResult(new Variant());
    }

    async public void Save()
    {
        if (!GetInputConnection("texture").IsConnected())
            return;

        Texture2D texture = (await GetInputValue("texture", onlyIfModified: true)).GetValue<Texture2D>();
        if (texture == null)
            return;

        string path = "Assets/" + saveFolder + (await GetInputValue("path")).GetValue<string>();

        if (!path.EndsWith(".exr"))
            path += ".exr";
        
        try
        {
            if (texture.format != TextureFormat.RGBAFloat)
            {
                texture = ConvertToEXR(texture);
                Debug.Log("Converted texture to EXR format for saving.");
            }

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
        
        Texture2D exrTexture = new Texture2D(texture.width, texture.height, TextureFormat.RGBAFloat, false, true);
        for (int x=0; x<texture.width; x++)
        {
            for (int y=0; y<texture.height; y++)
            {
                float r = texture.GetPixel(x, y).r;
                exrTexture.SetPixel(x, y, new Color(r, r, r, 1));
            }
        }
        exrTexture.Apply();
        return exrTexture;
    }
}
