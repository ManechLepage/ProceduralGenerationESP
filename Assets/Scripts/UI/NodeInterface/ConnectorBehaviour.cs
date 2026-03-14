using UnityEngine;
using UnityEngine.UI;

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
    private bool dragToRemove = false;

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
        if (type == Type.Output && GraphManager.Instance.currentLine == null && !isConnected())
        {
            currentLine = Instantiate(linePrefab, transform.position, Quaternion.identity);
            currentLine.GetComponent<LineManager>().isLinked = false;
            currentLine.GetComponent<LineManager>().input = this;
            currentLine.transform.SetParent(transform);

            GraphManager.Instance.currentLine = currentLine.GetComponent<LineManager>();

            DisableAllConnections();
        }
        else if (type == Type.Input && isConnected())
        {
            dragToRemove = true;
            currentLine.GetComponent<LineManager>().isRemoving = true;

            DisableAllConnections(fromInput: true);
        }
    }

    void Update()
    {
        if (currentLine != null && !isConnected())
        {
            if (Input.GetMouseButtonUp(0))
            {
                bool didConnect = TryConnect(Input.mousePosition);

                if (!didConnect)
                    ReleaseConnection();
                
                EnableAllConnections();
            }
        }

        if (dragToRemove)
        {
            if (Input.GetMouseButtonUp(0))
            {
                dragToRemove = false;
                currentLine.GetComponent<LineManager>().isRemoving = false;

                if (!TryConnect(Input.mousePosition, fromInput: true) && !MouseInConnector(Input.mousePosition, this))
                {
                    ReleaseConnection();
                    connectedTo.connectedTo = null;
                    connectedTo = null;
                }

                EnableAllConnections(fromInput: true);
            }
        }
    }

    bool TryConnect(Vector2 mousePosition, bool fromInput = false)
    {
        foreach (ConnectorBehaviour connector in GraphManager.Instance.GetAllConnectors())
        {
            if (!fromInput && (connector == this || connector.node == this.node || connector.type == this.type || connector.dataType != this.dataType))
                continue;
            
            if (fromInput && (connector == this || connectedTo.node == connector.node || connector.type != this.type || connector.dataType != this.dataType))
                continue;
            
            if (connector.currentLine != null)
                continue;

            if (MouseInConnector(mousePosition, connector))
            {
                currentLine.GetComponent<LineManager>().output = connector;
                currentLine.GetComponent<LineManager>().isLinked = true;
                GraphManager.Instance.currentLine = null;

                if (!fromInput)
                {
                    connectedTo = connector;
                    connector.connectedTo = this;
                    connector.currentLine = currentLine;
                }
                else
                {
                    connectedTo.connectedTo = connector;
                    connector.connectedTo = connectedTo;
                    connector.currentLine = currentLine;

                    connectedTo = null;
                    currentLine = null;
                }

                return true;
            }
        }
        return false;
    }

    bool MouseInConnector(Vector2 mousePosition, ConnectorBehaviour connector)
    {
        RectTransform rectTransform = connector.GetComponent<RectTransform>();
        Vector2 localMousePosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, mousePosition, null, out localMousePosition);
        return rectTransform.rect.Contains(localMousePosition);
    }

    public void DisableAllConnections(bool fromInput = false)
    {
        foreach (ConnectorBehaviour connector in GraphManager.Instance.GetAllConnectors())
        {
            if ((!fromInput && connector == this) || (fromInput && connectedTo == connector))
                continue;
            
            if (!fromInput && (connector.node == this.node || connector.type == this.type || connector.dataType != this.dataType))
                connector.Disable();
            if (fromInput && (connector.type != this.type || connector.dataType != this.dataType || (connectedTo != null && connectedTo.node == connector.node)))
                connector.Disable();
            if (connector.currentLine != null)
                connector.Disable();
        }
    }

    public void EnableAllConnections(bool fromInput = false)
    {
        foreach (ConnectorBehaviour connector in GraphManager.Instance.GetAllConnectors())
        {
            connector.Enable();
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

    public void Disable()
    {
        Color color = GetComponent<Image>().color;
        if (color != null)        {
            color = Color.gray;
            color.a = GetComponent<Image>().color.a;
            GetComponent<Image>().color = color;
        }
    }

    public void Enable()
    {
        Color color = GetComponent<Image>().color;
        if (color != null)        {
            color = Color.white;
            color.a = GetComponent<Image>().color.a;
            GetComponent<Image>().color = color;
        }
    }
}
