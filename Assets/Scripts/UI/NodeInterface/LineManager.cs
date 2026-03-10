using UnityEngine;

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

    void Update()
    {
        lineRenderer.points[0] = input.transform.position;
        if (isLinked)
            lineRenderer.points[1] = output.transform.position;
        else
            lineRenderer.points[1] = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }
}
