using UnityEngine;
using System.Collections.Generic;

public class GraphManager : MonoBehaviour
{
    
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
    public Vector2 asVector2;
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

        if (typeof(T) == typeof(Vector2) && dataType == DataType.Vector2)
            return (T)(object)asVector2;
        
        if (typeof(T) == typeof(List<List<float>>) && dataType == DataType.HeightMap)
            return (T)(object)asHeightMap;

        if (typeof(T) == typeof(Texture2D) && dataType == DataType.Texture)
            return (T)(object)asTexture;
        
        throw new System.InvalidCastException($"Variant {dataType} cannot convert to {typeof(T)}");
    }
}
