using UnityEngine;
using System.Collections.Generic;

public class TextureToHeightMapNode : NodeBehaviour
{
    private TextureHelpers textureHelpers;

    void Awake()
    {
        textureHelpers = GetComponent<TextureHelpers>();
    }

    public override Variant OnFire()
    {
        if (!GetInputConnection("texture").IsConnected())
        {
            Debug.Log("TextureToHeightMapNode: Texture input not connected!");
            return new Variant(new List<List<float>>());
        }

        Texture2D texture = GetInputValue("texture").GetValue<Texture2D>();
        List<List<float>> heightMap = textureHelpers.TextureToHeightMap(texture);
        return new Variant(heightMap);
    }
}
