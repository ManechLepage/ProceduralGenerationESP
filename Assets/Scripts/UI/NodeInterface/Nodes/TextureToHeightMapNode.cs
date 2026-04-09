using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

public class TextureToHeightMapNode : NodeBehaviour
{
    private TextureHelpers textureHelpers;

    void Awake()
    {
        textureHelpers = GetComponent<TextureHelpers>();
    }

    async public override Task<Variant> OnFire()
    {
        if (!GetInputConnection("texture").IsConnected())
        {
            return new Variant(new List<List<float>>());
        }

        Texture2D texture = (await GetInputValue("texture")).GetValue<Texture2D>();

        if (texture == null)
        {
            return new Variant(new List<List<float>>());
        }

        List<List<float>> heightMap = textureHelpers.TextureToHeightMap(texture);
        return new Variant(heightMap);
    }
}
