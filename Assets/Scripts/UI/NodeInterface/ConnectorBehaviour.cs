using UnityEngine;

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
    Texture,
    Bool
}

public class ConnectorBehaviour : MonoBehaviour
{
    public string connectionName;
    public Type type;
    public DataType dataType;
    
    public GameObject linePrefab;
    
    [HideInInspector] public MultiInputBehaviour multiInput;

    [HideInInspector] public ConnectorBehaviour connectedTo;
    [HideInInspector] public NodeBehaviour node;
    
    private GameObject currentLine;

    public bool isConnected()
    {
        return connectedTo != null;
    }

    public MultiInputBehaviour GetInputBehaviour()
    {
        return GetComponent<MultiInputBehaviour>();
    }
    
    public void ClickedConnection()
    {
        if (type == Type.Output && GraphManager.Instance.currentLine == null)
        {
            currentLine = Instantiate(linePrefab, transform.position, Quaternion.identity);
            currentLine.GetComponent<LineManager>().isLinked = false;
            currentLine.GetComponent<LineManager>().input = this;
            currentLine.transform.SetParent(transform);

            GraphManager.Instance.currentLine = currentLine.GetComponent<LineManager>();
        }
        else if (type == Type.Input && GraphManager.Instance.currentLine != null)
        {
            ConnectorBehaviour other = GraphManager.Instance.currentLine.input;
            if (other.type != this.type && other.dataType == this.dataType && other.node != this.node)
            {
                currentLine = GraphManager.Instance.currentLine.gameObject;
                LineManager lineManager = GraphManager.Instance.currentLine;
                lineManager.output = this;
                lineManager.isLinked = true;
                connectedTo = other;
                other.connectedTo = this;
                GraphManager.Instance.currentLine = null;
            }
        }
    }

    void Update()
    {
        if (currentLine != null)
        {
            if (Input.GetMouseButtonDown(1) && !isConnected())
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

    public bool TryLink(ConnectorBehaviour other)
    {
        if (other.type == this.type || other.dataType != this.dataType || other.node == this.node)
            return false;

        this.connectedTo = other;
        Debug.Log("Linked " + this.connectionName + " to " + other.connectionName);

        GameObject line = Instantiate(linePrefab, transform.position, Quaternion.identity);
        line.GetComponent<LineManager>().isLinked = true;
        line.GetComponent<LineManager>().input = this;
        line.GetComponent<LineManager>().output = other;
        line.transform.SetParent(transform);

        return true;
    }
}
