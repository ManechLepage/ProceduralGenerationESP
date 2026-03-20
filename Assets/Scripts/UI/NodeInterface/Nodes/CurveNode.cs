using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
public class CurveNode : NodeBehaviour
{
    public RectTransform previewContainer;
    public AnimationCurve curve;
    public GameObject lineGO;

    private MaskableUILineRenderer lineRenderer;

    void Awake()
    {
        lineRenderer = lineGO.GetComponent<MaskableUILineRenderer>();
        lineRenderer.color = Color.white;
        lineRenderer.thickness = 2f;
    }

    public override void Start()
    {
        base.Start();

        UpdateLine();
    }

    public override Variant OnFire()
    {
        return new Variant();
    }

    public override void InputUpdated(ConnectorBehaviour connector)
    {
        base.InputUpdated(connector);
    }

    void UpdateLine()
    {
        // for now only use the keys of the curve, but we could later add more points for a smoother curve.

        Vector2 bottomLeft = new Vector2(0f, 0f);
        Vector2 topRight = new Vector2(previewContainer.rect.width, previewContainer.rect.height);

        List<Vector2> points = new List<Vector2>();
        foreach (Keyframe key in curve.keys)
        {
            float x = Mathf.Lerp(bottomLeft.x, topRight.x, key.time);
            float y = Mathf.Lerp(bottomLeft.y, topRight.y, key.value);
            points.Add(new Vector2(x, y));
        }

        lineRenderer.points = points.ToArray();
        lineRenderer.SetVerticesDirty();
    }
}
