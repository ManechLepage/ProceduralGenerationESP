using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

public class DrawTextureNode : NodeBehaviour
{
    public RawImage preview;
    public TextMeshProUGUI sizeText;

    public override Variant OnFire()
    {
        if (preview.texture == null)
        {
            Variant empty = new Variant();
            empty.dataType = DataType.Texture;
            empty.asTexture = null;
            return empty;
        }

        return new Variant(EXRToTexture2D(preview.texture as Texture2D));
    }

    public void DidUpdateTexture()
    {
        Vector2Int size = new Vector2Int(0, 0);
        
        if (preview.texture != null)
        {
            size = new Vector2Int(preview.texture.width, preview.texture.height);
        }

        sizeText.text = $"{size.x} x {size.y}";

        InputUpdated(null);
    }

    public void OpenDrawInterface()
    {
        Vector2 floatSize = GetInputValue("size").GetValue<Vector2>();
        Vector2Int size = new Vector2Int((int)floatSize.x, (int)floatSize.y);

        // To prevent strange teleportations,
        GetComponent<UIDraggable>().CancelNextDrag();

        PaintManager.Instance.paintSize = size;
        PaintManager.Instance.previewImage = preview;
        PaintManager.Instance.onUpdatingPreview.AddListener(DidUpdateTexture);

        if (preview.texture != null)
            PaintManager.Instance.SetTexture(preview.texture as Texture2D);
        else
            PaintManager.Instance.InitializePainting();

        GraphManager.Instance.EnableDrawInterface();
        GraphManager.Instance.DisableGraphInterface();
    }

    public Texture2D EXRToTexture2D(Texture2D exrTexture)
    {
        Texture2D texture = new Texture2D(exrTexture.width, exrTexture.height, TextureFormat.RGBAFloat, false);
        texture.SetPixels(exrTexture.GetPixels());
        texture.Apply();
        return texture;
    }
}
