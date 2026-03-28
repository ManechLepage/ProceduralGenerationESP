using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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
    Color,
    HeightMap,
    Texture,
    Bool,
    DomainMap,
    Preview,
    Curve,
    None
}

public class ConnectorBehaviour : MonoBehaviour
{
    public string connectionName;
    public Type type;
    public DataType dataType;
    public bool multipleOutputs = true;
    
    public GameObject linePrefab;
    
    [HideInInspector] public MultiInputBehaviour multiInput;

    [HideInInspector] public ConnectorBehaviour connectedTo;
    [HideInInspector] public List<ConnectorBehaviour> multipleConnectedTo = new List<ConnectorBehaviour>();
    [HideInInspector] public NodeBehaviour node;
    
    [HideInInspector] public GameObject currentLine;
    [HideInInspector] public List<GameObject> connectionLines = new List<GameObject>();
    private bool dragToRemove = false;
    private ConnectionColorUpdater connectionColorUpdater;

    void Awake()
    {
        multiInput = GetComponent<MultiInputBehaviour>();
        connectionColorUpdater = GetComponent<ConnectionColorUpdater>();
    }

    public bool IsConnected()
    {
        return connectedTo != null || multipleConnectedTo.Count > 0;
    }

    public void InputUpdated()
    {
        if (node != null)
            node.InputUpdated(this);
    }

    public GameObject CreateLineFromConnection()
    {
        GameObject newLine = Instantiate(linePrefab, transform.position, Quaternion.identity);
        newLine.GetComponent<LineManager>().isLinked = false;
        newLine.GetComponent<LineManager>().input = this;

        Color lighterLineColor = GetComponent<Image>().color;
        lighterLineColor.r *= 1.2f;
        lighterLineColor.g *= 1.2f;
        lighterLineColor.b *= 1.2f;
        lighterLineColor.a *= 0.6f;
        newLine.GetComponent<MaskableUILineRenderer>().color = lighterLineColor;

        newLine.transform.SetParent(GraphManager.Instance.lineParent.transform);

        return newLine;
    }
    
    public void ClickedConnection()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        if (type == Type.Output && (!IsConnected() || multipleOutputs))
        {
            currentLine = CreateLineFromConnection();
            connectionLines.Add(currentLine);
            GraphManager.Instance.currentLine = currentLine.GetComponent<LineManager>();

            DisableAllConnections();
        }
        else if (type == Type.Input && IsConnected())
        {
            dragToRemove = true;
            currentLine = connectionLines[0];
            currentLine.GetComponent<LineManager>().isRemoving = true;

            DisableAllConnections(fromInput: true);
        }
    }

    void Update()
    {
        if (currentLine != null && type == Type.Output)
        {
            if (Input.GetMouseButtonUp(0))
            {
                bool didConnect = TryConnect(Input.mousePosition);

                if (!didConnect)
                    ReleaseConnection();
                else if (multiInput != null)
                    multiInput.DisableInputs();
                
                currentLine = null;
                // Debug.Log("[From output] Current line set to null because it has been connected on an input or placed in air.");

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
                    // Debug.Log("[From input] Not connected to another connector, removing connection.");

                    if (multiInput != null)
                        multiInput.EnableInputs();

                    ReleaseConnection();

                    connectedTo.multipleConnectedTo.Remove(this);

                    connectedTo.connectedTo = null;
                    this.multipleConnectedTo.Remove(connectedTo);

                    connectedTo = null;

                    InputUpdated();
                }

                currentLine = null;
                // Debug.Log("[From input] Current Line set to null because it has been connected on an input or placed in air.");

                EnableAllConnections(fromInput: true);
            }
        }
    }

    bool CompatibleTypes(DataType a, DataType b)
    {
        if (a == b)
            return true;

        if ((a == DataType.Int && b == DataType.Float) || (a == DataType.Float && b == DataType.Int))
            return true;

        return false;
    }

    bool TryConnect(Vector2 mousePosition, bool fromInput = false)
    {
        foreach (ConnectorBehaviour connector in GraphManager.Instance.GetAllConnectors())
        {
            if (!fromInput && (connector == this || !CompatibleConnectors(this, connector)))
                continue;
            
            if (fromInput && (connector == this || !CompatibleFromInput(this, connector)))
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
                    if (connector.multiInput != null)
                        connector.multiInput.DisableInputs();

                    connectedTo = connector;
                    multipleConnectedTo.Add(connector);

                    connector.connectedTo = this;
                    connector.multipleConnectedTo.Add(this);

                    connector.currentLine = currentLine;
                    connector.connectionLines.Add(currentLine);

                    connector.InputUpdated();
                }
                else
                {
                    if (connector.multiInput != null)
                        connector.multiInput.DisableInputs();
                    if (multiInput != null)
                        multiInput.EnableInputs();

                    connectedTo.connectedTo = connector;
                    connectedTo.multipleConnectedTo.Remove(this);
                    connectedTo.multipleConnectedTo.Add(connector);

                    connector.connectedTo = connectedTo;
                    connector.multipleConnectedTo.Add(connectedTo);

                    connector.currentLine = currentLine;
                    connector.connectionLines.Add(currentLine);

                    connector.InputUpdated();
                    InputUpdated();

                    multipleConnectedTo.Remove(connectedTo);
                    connectionLines.Remove(currentLine);

                    connectedTo = null;
                    currentLine = null;
                }

                return true;
            }
        }
        return false;
    }

    public bool MouseInConnector(Vector2 mousePosition, ConnectorBehaviour connector)
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
            if ((!fromInput && connector == this) || (fromInput && (connectedTo == connector || connector == this)))
                continue;
            
            if (!fromInput && !CompatibleConnectors(this, connector))
                connector.Disable();
            
            if (fromInput && !CompatibleFromInput(this, connector))
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

    bool CompatibleConnectors(ConnectorBehaviour a, ConnectorBehaviour b)
    {
        if (a.type == b.type)
            return false;

        if (!CompatibleTypes(a.dataType, b.dataType))
            return false;
        
        if (a.node == b.node)
            return false;
        
        if (a.type == Type.Input && a.IsConnected() || b.type == Type.Input && b.IsConnected())
            return false;

        return true;
    }

    bool CompatibleFromInput(ConnectorBehaviour current, ConnectorBehaviour target)
    {
        if (target.type != Type.Input)
            return false;

        if (!CompatibleTypes(current.dataType, target.dataType))
            return false;
        
        if (current.connectedTo != null && current.connectedTo.node == target.node)
            return false;

        return true;
    }

    public void ReleaseConnection()
    {
        if (currentLine != null)
        {
            connectionLines.Remove(currentLine);
            if (connectedTo != null)
                connectedTo.connectionLines.Remove(currentLine);
            
            Destroy(currentLine);
            currentLine = null;
        }
    }

    public void RemoveConnections()
    {
        if (IsConnected())
        {
            foreach (ConnectorBehaviour _connectedTo in multipleConnectedTo)
            {
                GameObject line;
                if (type == Type.Input)
                {
                    // Only one connection
                    line = connectionLines[0];
                }
                else
                {
                    // Multiple lines, so check the input connected to find the right line
                    line = _connectedTo.connectionLines[0];
                }

                connectionLines.Remove(line);
                currentLine = null;

                _connectedTo.connectionLines.Remove(line);
                _connectedTo.currentLine = null;

                Destroy(line);

                _connectedTo.multipleConnectedTo.Remove(this);
                _connectedTo.connectedTo = null;
            }

            multipleConnectedTo = new List<ConnectorBehaviour>();
            connectedTo = null;
        }
    }

    public void Disable()
    {
        if (connectionColorUpdater == null)
        {
            Color color = GetComponent<Image>().color;
            if (color != null)        {
                color = Color.gray;
                color.a = GetComponent<Image>().color.a;
                GetComponent<Image>().color = color;
            }
        }
        else
        {
            connectionColorUpdater.Disable();
        }
    }

    public void Enable()
    {
        if (connectionColorUpdater == null)
        {
            Color color = GetComponent<Image>().color;
            if (color != null)        {
                color = Color.white;
                color.a = GetComponent<Image>().color.a;
                GetComponent<Image>().color = color;
            }
        }
        else
        {
            connectionColorUpdater.Enable();
        }
    }
}
