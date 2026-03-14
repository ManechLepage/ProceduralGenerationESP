using UnityEngine;
using System.Collections.Generic;

public class GraphManager : MonoBehaviour
{
    public static GraphManager Instance { get; private set;}
    public List<NodeBehaviour> nodes = new List<NodeBehaviour>();
    public LineManager currentLine;

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

    public ConnectorBehaviour GetInputConnection(string name)
    {
        return inputConnections.Find(c => c.connectionName == name);
    }

    public ConnectorBehaviour GetOutputConnection(string name)
    {
        return outputConnections.Find(c => c.connectionName == name);
    }

    public Variant Fire() { return new Variant(); }
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
    public List<List<float>> asHeightMap;
    public Texture2D asTexture;

    public T GetValue<T>()
    {
        if (typeof(T) == typeof(int) && dataType == DataType.Int)
            return (T)(object)asInt;

        if (typeof(T) == typeof(float) && dataType == DataType.Float)
            return (T)(object)asFloat;

        if (typeof(T) == typeof(string) && dataType == DataType.String)
            return (T)(object)asString;
        
        if (typeof(T) == typeof(bool) && dataType == DataType.Bool)
            return (T)(object)asBool;

        if (typeof(T) == typeof(Vector2) && dataType == DataType.Vector2)
            return (T)(object)asVector2;
        
        if (typeof(T) == typeof(Vector3) && dataType == DataType.Vector3)
            return (T)(object)asVector3;
        
        if (typeof(T) == typeof(List<List<float>>) && dataType == DataType.HeightMap)
            return (T)(object)asHeightMap;

        if (typeof(T) == typeof(Texture2D) && dataType == DataType.Texture)
            return (T)(object)asTexture;
        
        throw new System.InvalidCastException($"Variant {dataType} cannot convert to {typeof(T)}");
    }
}
