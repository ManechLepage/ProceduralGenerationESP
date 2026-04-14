using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;

public class LoadGraphManager : MonoBehaviour
{
    public string nodePrefabFolder = "Prefabs/UI/Nodes/";

    public void LoadGraph(string path)
    {
        string json = System.IO.File.ReadAllText(path);
        NodeGraphData graphData = LoadGraphData(json);

        foreach (NodeBehaviour node in GraphManager.Instance.nodes)
        {
            if (node.prefabName != "MasterNode")
            {
                node.DisconnectAll();
                Destroy(node.gameObject);
            }
        }

        GraphManager.Instance.nodes.Clear();

        foreach (NodeData node in graphData.nodes)
        {
            // Ne pas recréer le MasterNode, juste lui assigner les valeurs
            if (node.prefab != "MasterNode")
                CreateNode(node);
            else
            {
                AssignInputs(GraphManager.Instance.masterNode, node.inputs);
                Vector3 position = new Vector3(
                    node.offsetX * GraphManager.Instance.currentZoom - GraphManager.Instance.currentOffset.x,
                    node.offsetY * GraphManager.Instance.currentZoom - GraphManager.Instance.currentOffset.y,
                    0
                );
                GraphManager.Instance.masterNode.transform.position = position;
                GraphManager.Instance.nodes.Add(GraphManager.Instance.masterNode);
            }
        }

        foreach (ConnectionData connection in graphData.connections)
        {
            NodeBehaviour fromNode = GetNodeWithID(int.Parse(connection.fromNode));
            NodeBehaviour toNode = GetNodeWithID(int.Parse(connection.toNode));

            if (fromNode != null && toNode != null)
            {
                GraphManager.Instance.LinkConnections(
                    fromNode.GetOutputConnection(connection.fromOutputName),
                    toNode.GetInputConnection(connection.toInputName),
                    callInputUpdated: false
                );
            }
            else
            {
                Debug.LogWarning($"Connection skipped: from {connection.fromNode} to {connection.toNode}");
            }
        }
    }

    NodeBehaviour GetNodeWithID(int id)
    {
        return GraphManager.Instance.nodes.Find(n => n.id == id);
    }

    void CreateNode(NodeData nodeData)
    {
        GameObject prefab = FindNodePrefab(nodeData.prefab);
        if (prefab == null)
            return;

        Vector3 position = new Vector3(
            nodeData.offsetX * GraphManager.Instance.currentZoom - GraphManager.Instance.currentOffset.x,
            nodeData.offsetY * GraphManager.Instance.currentZoom - GraphManager.Instance.currentOffset.y,
            0
        );
        GameObject node = GraphManager.Instance.CreateNode(prefab, position, int.Parse(nodeData.id));
        NodeBehaviour nodeBehaviour = node.GetComponent<NodeBehaviour>();

        if (nodeBehaviour != null)
        {
            AssignInputs(nodeBehaviour, nodeData.inputs);
        }
    }

    void AssignInputs(NodeBehaviour node, Dictionary<string, Variant> inputs)
    {
        foreach (KeyValuePair<string, Variant> input in inputs)
        {
            if (input.Value.dataType == DataType.None)
                continue;
            
            node.SetInputValue(input.Key, input.Value);
        }
    }

    GameObject FindNodePrefab(string prefabName)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/" + nodePrefabFolder + prefabName + ".prefab"
        );
        
        if (prefab == null)
            Debug.LogWarning($"Prefab not found: {nodePrefabFolder + prefabName}");
        
        return prefab;
    }

    NodeGraphData LoadGraphData(string json)
    {
        var root  = JObject.Parse(json);
        var graph = new NodeGraphData
        {
            nodes = new List<NodeData>(),
            connections = new List<ConnectionData>()
        };

        foreach (var nodeToken in root["nodes"])
        {
            var nodeData = new NodeData
            {
                id = nodeToken["id"].Value<string>(),
                prefab = nodeToken["prefab"].Value<string>(),
                offsetX = nodeToken["offsetX"].Value<float>(),
                offsetY = nodeToken["offsetY"].Value<float>(),
                inputs = new Dictionary<string, Variant>()
            };

            foreach (var input in (JObject)nodeToken["inputs"])
            {
                DataType type = DetermineDataType(input.Value);
                nodeData.inputs[input.Key] = FromJToken(input.Value, type);
            }

            graph.nodes.Add(nodeData);
        }

        foreach (var connectionToken in root["connections"])
        {
            var connectionData = new ConnectionData
            {
                fromNode = connectionToken["fromNode"].Value<string>(),
                fromOutputName = connectionToken["fromOutputName"].Value<string>(),
                toNode = connectionToken["toNode"].Value<string>(),
                toInputName = connectionToken["toInputName"].Value<string>()
            };

            graph.connections.Add(connectionData);
        }

        return graph;
    }

    public static Variant FromJToken(JToken token, DataType type)
    {
        return type switch
        {
            DataType.Float   => new Variant(token.Value<float>()),
            DataType.Int     => new Variant(token.Value<int>()),
            DataType.Bool    => new Variant(token.Value<bool>()),
            DataType.String  => new Variant(token.Value<string>()),
            DataType.Vector2 => new Variant(token.ToObject<Vector2>()),
            _                => new Variant(type)
        };
    }

    DataType DetermineDataType(JToken token)
    {
        if (token.Type == JTokenType.Float)
            return DataType.Float;
        if (token.Type == JTokenType.Integer)
            return DataType.Int;
        if (token.Type == JTokenType.Boolean)
            return DataType.Bool;
        if (token.Type == JTokenType.String)
            return DataType.String;
        if (token.Type == JTokenType.Object && token["x"] != null && token["y"] != null)
            return DataType.Vector2;

        return DataType.None;
    }
}
