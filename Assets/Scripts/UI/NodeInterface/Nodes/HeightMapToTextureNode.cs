using UnityEngine;
using System.Collections.Generic;

public class HeightMapToTextureNode : NodeBehaviour
{
    private TextureHelpers textureHelpers;

    void Awake()
    {
        textureHelpers = GetComponent<TextureHelpers>();
    }

    public override Variant OnFire()
    {
        Variant empty = new Variant();
        empty.dataType = DataType.Texture;
        empty.asTexture = null;

        if (!GetInputConnection("heightmap").IsConnected())
            return empty;
        
        List<List<float>> heightMap = GetInputValue("heightmap").GetValue<List<List<float>>>();
        if (heightMap == null || heightMap.Count == 0 || heightMap[0].Count == 0)
            return empty;
        
        Texture2D texture = textureHelpers.HeightMapToTexture(heightMap);
        return new Variant(texture);
    }
}
