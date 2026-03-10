using UnityEngine;
using UnityEngine.InputSystem;

public class LineManager : MonoBehaviour
{
    public bool isLinked;
    public ConnectorBehaviour input;
    public ConnectorBehaviour output;
    private UILineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = GetComponent<UILineRenderer>();
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
        if (isLinked)
            lineRenderer.points[1] = output.transform.position;
        else{
            lineRenderer.points[1] = GetMousePositionInContainer();
            Debug.Log(lineRenderer.points[1]);
        }

        lineRenderer.SetVerticesDirty();
    }
}
