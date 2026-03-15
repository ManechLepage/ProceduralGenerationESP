using UnityEngine;
using System.Collections.Generic;

public class GraphManager : MonoBehaviour
{
    public static GraphManager Instance { get; private set;}
    public NodeBehaviour masterNode;
    public List<NodeBehaviour> nodes = new List<NodeBehaviour>();
    public GameObject nodeParent;
    [HideInInspector] public LineManager currentLine;
    public float currentZoom = 1f;

    public Vector2Int GetTerrainSize()
    {
        if (masterNode != null)
        {
            Vector2 size = masterNode.GetInputValue("size").GetValue<Vector2>();
            return new Vector2Int((int)size.x, (int)size.y);
        }
        return new Vector2Int(256, 256);
    }

    void Update()
    {
        if (false && Input.GetKeyDown(KeyCode.Return))
        {
            if (masterNode != null)
            {
                masterNode.Fire();
            }
        }
    }

    public List<ConnectorBehaviour> GetAllConnectors()
    {
        List<ConnectorBehaviour> connectors = new List<ConnectorBehaviour>();
        foreach (NodeBehaviour node in nodes)
        {
            connectors.AddRange(node.inputConnections);
            connectors.AddRange(node.outputConnections);
        }
        return connectors;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        if (!masterNode)
        {
            Debug.Log("Master node not assigned in GraphManager!");
        }

        foreach (Transform node in nodeParent.GetComponentInChildren<Transform>())
        {
            NodeBehaviour nodeBehaviour = node.GetComponent<NodeBehaviour>();
            if (nodeBehaviour != null)
                nodes.Add(nodeBehaviour);
        }
    }
}

public abstract class NodeBehaviour : MonoBehaviour
{
    public List<ConnectorBehaviour> inputConnections = new List<ConnectorBehaviour>();
    public List<ConnectorBehaviour> outputConnections = new List<ConnectorBehaviour>();

    public virtual void Start()
    {
        foreach (ConnectorBehaviour inputConnection in inputConnections)
            inputConnection.node = this;

        foreach (ConnectorBehaviour outputConnection in outputConnections)
            outputConnection.node = this;
    }

    public virtual Variant Fire() { return new Variant(); }

    public ConnectorBehaviour GetInputConnection(string name)
    {
        return inputConnections.Find(c => c.connectionName == name);
    }

    public ConnectorBehaviour GetOutputConnection(string name)
    {
        return outputConnections.Find(c => c.connectionName == name);
    }

    public Variant GetInputValue(string name)
    {
        ConnectorBehaviour connector = GetInputConnection(name);
        if (connector != null)
        {
            if (connector.IsConnected())
                return connector.connectedTo.node.Fire();
            else
            {
                if (connector.multiInput != null)
                    return connector.multiInput.GetVariant();
                else
                    Debug.Log($"Input connection '{name}' on node '{gameObject.name}' is not connected and has no default value");
            }
        }
        else
        {
            Debug.Log($"Input connection '{name}' not found or not connected on node '{gameObject.name}'");
        }
        return new Variant();
    }

    public void SetInputValue(string name, Variant value)
    {
        ConnectorBehaviour connector = GetInputConnection(name);
        if (connector != null)
        {
            if (connector.multiInput != null)
                connector.multiInput.SetVariant(value);
            else
                Debug.LogWarning($"Input connection '{name}' on node '{gameObject.name}' has no MultiInputBehaviour to set value");
        }
        else
        {
            Debug.LogWarning($"Input connection '{name}' not found on node '{gameObject.name}'");
        }
    }
}

[System.Serializable]
public class Variant
{
    public DataType dataType;

    public int asInt;
    public float asFloat;
    public string asString;
    public bool asBool;
    public Vector2 asVector2;
    public Vector3 asVector3;
    public Color asColor;
    public List<List<float>> asHeightMap;
    public Texture2D asTexture;

    public Variant()
    {
        dataType = DataType.Float;
        asFloat = 0f;
    }

    public Variant(int value) { dataType = DataType.Int; asInt = value; }
    public Variant(float value) { dataType = DataType.Float; asFloat = value; }
    public Variant(string value) { dataType = DataType.String; asString = value; }
    public Variant(bool value) { dataType = DataType.Bool; asBool = value; }
    public Variant(Vector2 value) { dataType = DataType.Vector2; asVector2 = value; }
    public Variant(Vector3 value) { dataType = DataType.Vector3; asVector3 = value; }
    public Variant(Color value) { dataType = DataType.Color; asColor = value; }
    public Variant(List<List<float>> value) { dataType = DataType.HeightMap; asHeightMap = value; }
    public Variant(Texture2D value) { dataType = DataType.Texture; asTexture = value; }

    public T GetValue<T>()
    {
        if (dataType == DataType.Int)
        {
            if (typeof(T) == typeof(int)) return (T)(object)asInt;
            if (typeof(T) == typeof(float)) return (T)(object)(float)asInt;
        }

        if (dataType == DataType.Float)
        {
            if (typeof(T) == typeof(float)) return (T)(object)asFloat;
            if (typeof(T) == typeof(int)) return (T)(object)Mathf.RoundToInt(asFloat);
        }
        
        if (typeof(T) == typeof(string) && dataType == DataType.String)
            return (T)(object)asString;
        
        if (typeof(T) == typeof(bool) && dataType == DataType.Bool)
            return (T)(object)asBool;

        if (typeof(T) == typeof(Vector2) && dataType == DataType.Vector2)
            return (T)(object)asVector2;
        
        if (typeof(T) == typeof(Vector3) && dataType == DataType.Vector3)
            return (T)(object)asVector3;
        
        if (typeof(T) == typeof(Color) && dataType == DataType.Color)
            return (T)(object)asColor;
        
        if (typeof(T) == typeof(List<List<float>>) && dataType == DataType.HeightMap)
            return (T)(object)asHeightMap;

        if (typeof(T) == typeof(Texture2D) && dataType == DataType.Texture)
            return (T)(object)asTexture;
        
        throw new System.InvalidCastException($"Variant {dataType} cannot convert to {typeof(T)}");
    }

    public void SetValue<T>(T value)
    {
        if (typeof(T) == typeof(int))
        {
            dataType = DataType.Int;
            asInt = (int)(object)value;
        }
        else if (typeof(T) == typeof(float))
        {
            dataType = DataType.Float;
            asFloat = (float)(object)value;
        }
        else if (typeof(T) == typeof(string))
        {
            dataType = DataType.String;
            asString = (string)(object)value;
        }
        else if (typeof(T) == typeof(bool))
        {
            dataType = DataType.Bool;
            asBool = (bool)(object)value;
        }
        else if (typeof(T) == typeof(Vector2))
        {
            dataType = DataType.Vector2;
            asVector2 = (Vector2)(object)value;
        }
        else if (typeof(T) == typeof(Vector3))
        {
            dataType = DataType.Vector3;
            asVector3 = (Vector3)(object)value;
        }
        else if (typeof(T) == typeof(Color))
        {
            dataType = DataType.Color;
            asColor = (Color)(object)value;
        }
        else if (typeof(T) == typeof(List<List<float>>))
        {
            dataType = DataType.HeightMap;
            asHeightMap = (List<List<float>>)(object)value;
        }
        else if (typeof(T) == typeof(Texture2D))
        {
            dataType = DataType.Texture;
            asTexture = (Texture2D)(object)value;
        }
        else
        {
            throw new System.InvalidCastException($"Unsupported type {typeof(T)} for Variant");
        }
    }
}
