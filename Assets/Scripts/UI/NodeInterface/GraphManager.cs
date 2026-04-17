using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Threading.Tasks;
using TMPro;

public enum NodeTimeType
{
    Other,
    Terrain,
    Erosion
}

public class GraphManager : MonoBehaviour
{
    public static GraphManager Instance { get; private set;}
    public NodeBehaviour masterNode;
    public List<NodeBehaviour> nodes = new List<NodeBehaviour>();
    public GameObject nodeParent;
    public GameObject lineParent;
    public RectMask2D rectMask;
    [HideInInspector] public LineManager currentLine;
    public float currentZoom = 1f;
    public Vector2 currentOffset = Vector2.zero;

    [Space]
    public GameObject graphInterface;
    public GameObject drawInterface;

    [Space]
    public GameObject nextButton;
    public TextMeshProUGUI nextFlagText;

    async public Task<Vector2Int> GetTerrainSize()
    {
        if (masterNode != null)
        {
            Vector2 size = (await masterNode.GetInputValue("size")).GetValue<Vector2>();
            return new Vector2Int((int)size.x, (int)size.y);
        }
        return new Vector2Int(256, 256);
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

    public void FireAllRanomNodes()
    {
        foreach (NodeBehaviour node in nodes)
        {
            if (node.hasRandom)
                _ = node.Fire(onlyIfModified: true);
        }
    }

    public void NextButtonPressed()
    {
        foreach (NodeBehaviour node in nodes)
        {
            if (node.IsLoading() && node.IsFlagged())
                node.UnpauseGeneration();
        }
    }

    public void PauseButtonToggled(bool isOn)
    {
        
    }

    public bool IsLoadingFlaggedNode()
    {
        foreach (NodeBehaviour node in nodes)
        {
            if (node.IsLoading() && node.IsFlagged())
            {
                nextFlagText.text = FormatPrefabName(node.prefabName);
                return true;
            }
        }
        return false;
    }

    string FormatPrefabName(string prefabName)
    {
        string finalName = "";
        foreach (char c in prefabName)
        {
            if (char.IsUpper(c) && finalName.Length > 0)
                finalName += " ";
            finalName += c;
        }
        finalName = finalName.Replace("Node", "");
        return finalName;
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

        int id = 0;
        foreach (Transform node in nodeParent.GetComponentInChildren<Transform>())
        {
            NodeBehaviour nodeBehaviour = node.GetComponent<NodeBehaviour>();
            if (nodeBehaviour != null)
            {
                nodeBehaviour.id = id;
                id++;

                nodes.Add(nodeBehaviour);
            }
        }
    }

    void Update()
    {
        if (IsLoadingFlaggedNode())
            nextButton.SetActive(true);
        else
            nextButton.SetActive(false);
    }

    public GameObject CreateNode(GameObject nodePrefab, Vector3 position = default(Vector3), int id = default(int))
    {
        if (nodePrefab == null) { return null; }
        GameObject newNode = Instantiate(nodePrefab, position, Quaternion.identity);
        newNode.transform.SetParent(nodeParent.transform);
        newNode.transform.localScale *= currentZoom * 1.4f;

        NodeBehaviour nodeBehaviour = newNode.GetComponent<NodeBehaviour>();
        if (nodeBehaviour != null)
        {
            nodeBehaviour.id = id != default(int) ? id : GetHighestNodeID() + 1;
            nodes.Add(nodeBehaviour);
        }

        return newNode;
    }

    public void LinkConnections(ConnectorBehaviour outputConnector, ConnectorBehaviour inputConnector, bool callInputUpdated = true)
    {
        GameObject currentLine = outputConnector.CreateLineFromConnection();
        outputConnector.connectionLines.Add(currentLine);
        inputConnector.connectionLines.Add(currentLine);

        outputConnector.multipleConnectedTo.Add(inputConnector);
        inputConnector.multipleConnectedTo.Add(outputConnector);

        currentLine.GetComponent<LineManager>().output = inputConnector;
        currentLine.GetComponent<LineManager>().isLinked = true;

        if (callInputUpdated)
            inputConnector.node.InputUpdated(inputConnector);
    }

    public int GetHighestNodeID()
    {
        int highestID = 0;
        foreach (NodeBehaviour node in nodes)
        {
            if (node.id > highestID)
                highestID = node.id;
        }
        return highestID;
    }

    public void DisableGraphInterface() { graphInterface.SetActive(false); }
    public void EnableGraphInterface() { graphInterface.SetActive(true); }
    public void DisableDrawInterface()
    {
        drawInterface.SetActive(false);
        if (PaintManager.Instance != null)
            PaintManager.Instance.WillDisable();
    }
    public void EnableDrawInterface() { drawInterface.SetActive(true); }
}

public abstract class NodeBehaviour : MonoBehaviour
{
    public int id = 0;
    public string prefabName = "Node";
    public List<ConnectorBehaviour> inputConnections = new List<ConnectorBehaviour>();
    public List<ConnectorBehaviour> outputConnections = new List<ConnectorBehaviour>();
    public GameObject loadingIcon;
    public Toggle flagToggle;
    public bool hasRandom = false;
    public NodeTimeType nodeTimeType = NodeTimeType.Other;

    private Variant lastOutput = new Variant();
    private bool modifiedSinceLastFire = true;
    private bool currentlyFiringOnlyIfModified = false;

    private bool paused_generation = false;

    async public Task<Variant> Fire(bool onlyIfModified = false)
    {
        if (onlyIfModified && !IsModifiedSinceLastFire())
        {
            var output = GetLastOutput();
            if (output.dataType != DataType.None)
                return output;
        }

        currentlyFiringOnlyIfModified = onlyIfModified;

        var stopWatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await OnFire();
        float elapsedTime = stopWatch.ElapsedMilliseconds / 1000f;

        currentlyFiringOnlyIfModified = false;
        
        ConnectorBehaviour previewConnector = GetOutputConnection("preview");
        if (previewConnector != null && previewConnector.IsConnected() && result.dataType == DataType.HeightMap && result.asHeightMap != null)
        {
            foreach (ConnectorBehaviour connectedTo in previewConnector.multipleConnectedTo)
            {
                (connectedTo.node as ViewNode).UpdatePreview(result.GetValue<List<List<float>>>());
            }
        }

        // Ajouter le temps de génération seulement si l'arbre complet est lancé.
        if (!onlyIfModified)
            TerrainManager.Instance.generationStatistics.actual.AddTime(nodeTimeType, prefabName, elapsedTime);

        SetLastOutput(result);
        SetModifiedSinceLastFire(false);
        return result;
    }

    public virtual Task<Variant> OnFire() { return Task.FromResult(new Variant()); }

    public virtual Task<float> GetPredictedTime() { return Task.FromResult(0f); }

    public void PauseGeneration() { paused_generation = true; }
    public void UnpauseGeneration() { paused_generation = false; }

    public bool IsGenerationPaused() { return paused_generation; }

    public bool IsLoading() { return loadingIcon.activeSelf; }

    protected async Task WaitForUnpause()
    {
        while (paused_generation)
            await Task.Delay(100);
    }

    public virtual void Start()
    {
        foreach (ConnectorBehaviour inputConnection in inputConnections)
            inputConnection.node = this;

        foreach (ConnectorBehaviour outputConnection in outputConnections)
            outputConnection.node = this;
    }

    public virtual void InputUpdated(ConnectorBehaviour connector)
    {
        modifiedSinceLastFire = true;

        if (outputConnections.Count > 0)
        {
            foreach (ConnectorBehaviour outputConnection in outputConnections)
            {
                if (outputConnection.IsConnected())
                {
                    foreach (ConnectorBehaviour connectedTo in outputConnection.multipleConnectedTo)
                    {
                        connectedTo.InputUpdated();
                    }
                }
            }
        }
    }

    public void ShowLoadingIcon(bool show)
    {
        if (loadingIcon != null)
            loadingIcon.SetActive(show);
    }

    public bool IsFlagged()
    {
        return flagToggle != null && flagToggle.isOn;
    }

    public virtual Task<Vector2Int> GetTerrainSize() { return Task.FromResult(Vector2Int.zero); }

    public void SetLastOutput(Variant output) { lastOutput = output; }
    public Variant GetLastOutput() { return lastOutput; }
    public void SetModifiedSinceLastFire(bool modified) { modifiedSinceLastFire = modified; }
    public bool IsModifiedSinceLastFire() { return modifiedSinceLastFire; }

    public void DisconnectAll()
    {
        foreach (ConnectorBehaviour connector in inputConnections)
        {
            connector.RemoveConnections();
        }

        foreach (ConnectorBehaviour connector in outputConnections)
        {
            connector.RemoveConnections();
        }
    }

    public ConnectorBehaviour GetInputConnection(string name)
    {
        return inputConnections.Find(c => c.connectionName == name);
    }

    public ConnectorBehaviour GetOutputConnection(string name)
    {
        return outputConnections.Find(c => c.connectionName == name);
    }

    async public Task<Variant> GetInputValue(string name, bool onlyIfModified = default)
    {
        ConnectorBehaviour connector = GetInputConnection(name);
        if (connector != null)
        {
            if (connector.IsConnected())
            {
                bool varOnlyIfModified = onlyIfModified == default ? currentlyFiringOnlyIfModified : onlyIfModified;
                return await connector.multipleConnectedTo[0].node.Fire(varOnlyIfModified);  // A cheap way to pass the onlyIfModified argument to other nodes.
            }
            else
            {
                if (connector.multiInput != null)
                    return connector.multiInput.GetVariant();
                else
                {
                    // Debug.Log($"Input connection '{name}' on node '{gameObject.name}' is not connected and has no default value");
                    return new Variant(connector.dataType);
                }
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
    public List<List<Vector2>> asDomainMap;
    public AnimationCurve asCurve;

    public Variant()
    {
        dataType = DataType.None;
        asFloat = 0f;
    }

    public Variant(DataType dataType) { this.dataType = dataType; }

    public Variant(int value) { dataType = DataType.Int; asInt = value; }
    public Variant(float value) { dataType = DataType.Float; asFloat = value; }
    public Variant(string value) { dataType = DataType.String; asString = value; }
    public Variant(bool value) { dataType = DataType.Bool; asBool = value; }
    public Variant(Vector2 value) { dataType = DataType.Vector2; asVector2 = value; }
    public Variant(Vector3 value) { dataType = DataType.Vector3; asVector3 = value; }
    public Variant(Color value) { dataType = DataType.Color; asColor = value; }
    public Variant(List<List<float>> value) { dataType = DataType.HeightMap; asHeightMap = value; }
    public Variant(Texture2D value) { dataType = DataType.Texture; asTexture = value; }
    public Variant(List<List<Vector2>> value) { dataType = DataType.DomainMap; asDomainMap = value; }
    public Variant(AnimationCurve value) { dataType = DataType.Curve; asCurve = value; }
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
        
        if (typeof(T) == typeof(List<List<Vector2>>) && dataType == DataType.DomainMap)
            return (T)(object)asDomainMap;
        
        if (typeof(T) == typeof(AnimationCurve) && dataType == DataType.Curve)
            return (T)(object)asCurve;

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
        else if (typeof(T) == typeof(List<List<Vector2>>))
        {
            dataType = DataType.DomainMap;
            asDomainMap = (List<List<Vector2>>)(object)value;
        }
        else if (typeof(T) == typeof(AnimationCurve))
        {
            dataType = DataType.Curve;
            asCurve = (AnimationCurve)(object)value;
        }
        else
        {
            throw new System.InvalidCastException($"Unsupported type {typeof(T)} for Variant");
        }
    }
}
