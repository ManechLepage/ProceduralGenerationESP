using UnityEngine;
using UnityEngine.UI;

// Code copié de https://github.com/Radishmouse22/UILineRenderer et modifié pour être compatible avec les masques de l'UI (RectMask2D)
[RequireComponent(typeof(CanvasRenderer))]
public class MaskableUILineRenderer : MaskableGraphic
{
    public Vector2[] points;
    public float thickness = 10f;
    public bool center = true;

    [Tooltip("Optional: if set, line segments will be clipped to this RectMask2D's rect.")]
    public RectMask2D clipMask;

    public override void Cull(Rect clipRect, bool validRect)
    {
        // Disable automatic culling from RectMask2D
        canvasRenderer.cull = false;
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (points == null || points.Length < 2)
            return;

        // compute mask rect in local space (same coord space we use for points)
        Rect maskRect = new Rect();
        bool useMask = false;
        Vector2 offset = center ? (rectTransform.sizeDelta / 2f) : Vector2.zero;

        if (clipMask != null)
        {
            var maskRT = clipMask.rectTransform;
            Vector3[] wc = new Vector3[4];
            maskRT.GetWorldCorners(wc);
            Vector2 min = (Vector2)rectTransform.InverseTransformPoint(wc[0]) - offset;
            Vector2 max = min;
            for (int i = 1; i < 4; i++)
            {
                Vector2 local = (Vector2)rectTransform.InverseTransformPoint(wc[i]) - offset;
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }
            maskRect = new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
            useMask = true;
        }

        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector2 a = points[i];
            Vector2 b = points[i + 1];

            if (useMask)
            {
                if (!ClipLineToRect(a, b, maskRect, out Vector2 ca, out Vector2 cb))
                    continue; // fully outside
                a = ca; b = cb;
            }

            CreateLineSegment(a, b, vh);

            int index = i * 5;
            vh.AddTriangle(index, index + 1, index + 3);
            vh.AddTriangle(index + 3, index + 2, index);

            if (i != 0)
            {
                vh.AddTriangle(index, index - 1, index - 3);
                vh.AddTriangle(index + 1, index - 1, index - 2);
            }
        }
    }

    // Cohen–Sutherland line clipping
    private const int INSIDE = 0;
    private const int LEFT = 1;
    private const int RIGHT = 2;
    private const int BOTTOM = 4;
    private const int TOP = 8;

    private int ComputeOutCode(Vector2 p, Rect r)
    {
        int code = INSIDE;
        if (p.x < r.xMin) code |= LEFT;
        else if (p.x > r.xMax) code |= RIGHT;
        if (p.y < r.yMin) code |= BOTTOM;
        else if (p.y > r.yMax) code |= TOP;
        return code;
    }

    private bool ClipLineToRect(Vector2 p0, Vector2 p1, Rect r, out Vector2 outA, out Vector2 outB)
    {
        outA = p0; outB = p1;
        int out0 = ComputeOutCode(p0, r);
        int out1 = ComputeOutCode(p1, r);

        while (true)
        {
            if ((out0 | out1) == 0)
            {
                // trivial accept
                outA = p0; outB = p1;
                return true;
            }
            else if ((out0 & out1) != 0)
            {
                // trivial reject
                return false;
            }
            else
            {
                int outcodeOut = out0 != 0 ? out0 : out1;
                float x = 0, y = 0;
                float dx = p1.x - p0.x;
                float dy = p1.y - p0.y;

                if ((outcodeOut & TOP) != 0)
                {
                    // intersect with y = r.yMax
                    if (Mathf.Abs(dy) < Mathf.Epsilon) { return false; }
                    float t = (r.yMax - p0.y) / dy;
                    x = p0.x + t * dx; y = r.yMax;
                }
                else if ((outcodeOut & BOTTOM) != 0)
                {
                    if (Mathf.Abs(dy) < Mathf.Epsilon) { return false; }
                    float t = (r.yMin - p0.y) / dy;
                    x = p0.x + t * dx; y = r.yMin;
                }
                else if ((outcodeOut & RIGHT) != 0)
                {
                    if (Mathf.Abs(dx) < Mathf.Epsilon) { return false; }
                    float t = (r.xMax - p0.x) / dx;
                    x = r.xMax; y = p0.y + t * dy;
                }
                else if ((outcodeOut & LEFT) != 0)
                {
                    if (Mathf.Abs(dx) < Mathf.Epsilon) { return false; }
                    float t = (r.xMin - p0.x) / dx;
                    x = r.xMin; y = p0.y + t * dy;
                }

                if (outcodeOut == out0)
                {
                    p0 = new Vector2(x, y);
                    out0 = ComputeOutCode(p0, r);
                }
                else
                {
                    p1 = new Vector2(x, y);
                    out1 = ComputeOutCode(p1, r);
                }
            }
        }
    }

    private void CreateLineSegment(Vector3 point1, Vector3 point2, VertexHelper vh)
    {
        Vector3 offset = center ? (rectTransform.sizeDelta / 2) : Vector2.zero;

        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;

        Quaternion point1Rotation = Quaternion.Euler(0, 0, RotatePointTowards(point1, point2) + 90);
        vertex.position = point1Rotation * new Vector3(-thickness / 2, 0);
        vertex.position += (Vector3)point1 - offset;
        vh.AddVert(vertex);
        vertex.position = point1Rotation * new Vector3(thickness / 2, 0);
        vertex.position += (Vector3)point1 - offset;
        vh.AddVert(vertex);

        Quaternion point2Rotation = Quaternion.Euler(0, 0, RotatePointTowards(point2, point1) - 90);
        vertex.position = point2Rotation * new Vector3(-thickness / 2, 0);
        vertex.position += (Vector3)point2 - offset;
        vh.AddVert(vertex);
        vertex.position = point2Rotation * new Vector3(thickness / 2, 0);
        vertex.position += (Vector3)point2 - offset;
        vh.AddVert(vertex);

        vertex.position = (Vector3)point2 - offset;
        vh.AddVert(vertex);
    }

    private float RotatePointTowards(Vector2 vertex, Vector2 target)
    {
        return (float)(Mathf.Atan2(target.y - vertex.y, target.x - vertex.x) * (180 / Mathf.PI));
    }
}