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
        return multipleConnectedTo.Count > 0;
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
                    if (multiInput != null)
                        multiInput.EnableInputs();

                    ReleaseConnection();

                    multipleConnectedTo[0].multipleConnectedTo.Remove(this);
                    this.multipleConnectedTo.Remove(multipleConnectedTo[0]);

                    InputUpdated();
                }

                currentLine = null;

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

                    multipleConnectedTo.Add(connector);

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

                    multipleConnectedTo[0].multipleConnectedTo.Remove(this);
                    multipleConnectedTo[0].multipleConnectedTo.Add(connector);

                    connector.multipleConnectedTo.Add(multipleConnectedTo[0]);

                    connector.currentLine = currentLine;
                    connector.connectionLines.Add(currentLine);

                    connector.InputUpdated();
                    InputUpdated();

                    multipleConnectedTo.Remove(multipleConnectedTo[0]);
                    connectionLines.Remove(currentLine);
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
            if ((!fromInput && connector == this) || (fromInput && (multipleConnectedTo[0] == connector || connector == this)))
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
        
        if (current.multipleConnectedTo[0] != null && current.multipleConnectedTo[0].node == target.node)
            return false;

        return true;
    }

    public void ReleaseConnection()
    {
        if (currentLine != null)
        {
            connectionLines.Remove(currentLine);
            LineManager lineManager = currentLine.GetComponent<LineManager>();
            ConnectorBehaviour connectedTo = lineManager.output != this ? lineManager.output : lineManager.input;
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
            foreach (ConnectorBehaviour connectedTo in multipleConnectedTo)
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
                    line = connectedTo.connectionLines[0];
                }

                connectionLines.Remove(line);
                currentLine = null;

                connectedTo.connectionLines.Remove(line);
                connectedTo.currentLine = null;

                Destroy(line);

                connectedTo.multipleConnectedTo.Remove(this);
            }

            multipleConnectedTo = new List<ConnectorBehaviour>();
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
