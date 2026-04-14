using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Threading.Tasks;

public class ViewNode : NodeBehaviour
{
    [Header("Preview Settings")]
    public Vector2Int previewSize = new Vector2Int(64, 64);

    [Space]
    public PreviewBehaviour preview;

    public override void Start()
    {
        base.Start();
    }

    public override Task<Variant> OnFire()
    {
        return Task.FromResult(new Variant());
    }

    async public override void InputUpdated(ConnectorBehaviour connector)
    {
        base.InputUpdated(connector);

        if (connector.IsConnected())
        {
            List<List<float>> heightMap = (await connector.multipleConnectedTo[0].node.Fire(onlyIfModified: true)).GetValue<List<List<float>>>();
            UpdatePreview(heightMap);
        }
        else
        {
            ClearPreview();
        }
    }

    public void UpdatePreview(List<List<float>> heightMap)
    {
        if (heightMap.Count > 0)
            preview.ApplyHeightMap(heightMap);
    }

    public void ClearPreview()
    {
        preview.rawImage.texture = null;
    }
}
