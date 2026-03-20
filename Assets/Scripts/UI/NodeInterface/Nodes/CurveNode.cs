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
        return new Variant(curve);
    }

    public override void InputUpdated(ConnectorBehaviour connector)
    {
        base.InputUpdated(connector);
    }

    void UpdateLine()
    {
        Vector2 bottomLeft = new Vector2(0f, 0f);
        Vector2 topRight = new Vector2(
            lineGO.GetComponent<RectTransform>().rect.width,
            lineGO.GetComponent<RectTransform>().rect.height
        );

        int resolution = 100;
        float step = 1f / (resolution - 1);

        List<Vector2> points = new List<Vector2>();
        for (int i = 0; i < resolution; i++)
        {
            float t = i * step;
            float value = curve.Evaluate(t);

            float x = Mathf.Lerp(bottomLeft.x, topRight.x, t);
            float y = Mathf.Lerp(bottomLeft.y, topRight.y, value);

            points.Add(new Vector2(x, y));
        }

        lineRenderer.points = points.ToArray();
        lineRenderer.SetVerticesDirty();
    }
}
