using UnityEngine;

public class ConnectorBehaviour : MonoBehaviour
{
    public string connectionName;
    public Type type;
    public DataType dataType;
    
    public GameObject linePrefab;

    [HideInInspector] public ConnectorBehaviour connectedTo;
    [HideInInspector] public NodeBehaviour node;
    
    private GameObject currentLine;

    public void StartConnection()
    {
        if (type == Type.Input)
            return;
        currentLine = Instantiate(linePrefab, transform.position, Quaternion.identity);
        currentLine.GetComponent<LineManager>().isLinked = false;
        currentLine.GetComponent<LineManager>().input = this;
        currentLine.transform.SetParent(transform);
    }

    void Update()
    {
        if (currentLine != null)
        {
            if (!Input.GetMouseButton(0))
            {
                ReleaseConnection();
            }
        }
    }

    public void ReleaseConnection()
    {
        if (currentLine != null)
        {
            Destroy(currentLine);
            currentLine = null;
        }
    }

    public void Link(ConnectorBehaviour other)
    {
        GameObject line = Instantiate(linePrefab, transform.position, Quaternion.identity);
        line.GetComponent<LineManager>().isLinked = true;
        line.GetComponent<LineManager>().input = this;
        line.GetComponent<LineManager>().output = other;
        line.transform.SetParent(transform);
    }
}

public enum Type{
    Input,
    Output
}

public enum DataType
{
    Int,
    Float,
    String,
    Vector2,
    Vector3,
    HeightMap,
    Texture
}
