using UnityEngine;
using UnityEngine.InputSystem;

public class LineManager : MonoBehaviour
{
    public bool isLinked;
    public bool isRemoving;
    public ConnectorBehaviour input;
    public ConnectorBehaviour output;
    private UILineRenderer lineRenderer;

    private float thickness = 3f;

    void Start()
    {
        lineRenderer = GetComponent<UILineRenderer>();
        thickness /= transform.localScale.x;
    }

    Vector2 GetMousePositionInContainer()
    {
        Vector2 screenPos = Mouse.current.position.ReadValue();
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform as RectTransform,
            screenPos,
            null,
            out Vector2 localPos
        );

        return localPos;
    }
    
    void Update()
    {
        if (isLinked && !isRemoving)
        {
            lineRenderer.points[1] = (output.transform.position - input.transform.position) / transform.lossyScale.x;
        }
        else
        {
            lineRenderer.points[1] = GetMousePositionInContainer();
        }

        lineRenderer.thickness = thickness;

        lineRenderer.SetVerticesDirty();
    }
}
