using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class SaveGraphManager : MonoBehaviour
{
    /*
    Le format de sauvegarde est un fichier JSON contenant les nodes et leurs paramètres, ainsi qu'une
    liste des connexions entre chaque nodes (en spécifiant les ports connectés).

    Les nodes sont représentés par un ID unique et le nom de leur prefab (ex. "PerlinNoiseNode"), ainsi
    que leurs paramètres, leur type et leur valeur.
    
    Ex. pour un arbre simple
    {
        "nodes": [
            {
                "id": "123456",
                "prefab": "PerlinNoiseNode",
                "inputs": {
                    "scale": {
                        "type": "float",
                        "value": 1.5
                    },
                    "offset": {
                        "type": "vector2",
                        "value": (0.5, 0.5)
                    }
                }
            },
            {
                "id": "654321",
                "prefab": "RescaleHeightmapNode",
                "inputs": {
                    "heightmap": {
                        "type": "HeightMap",
                        "value": none
                    }
                }
            }
        ],
        "connections": [
            {
                "fromNode": "123456",
                "fromOutputName": "heightmap",
                "toNode": "654321",
                "toInputName": "heightmap"
            }
        ]
    }

    Pour les valeurs des paramètres, puisqu'il y a beaucoup de types, chaque type a une représentation spéficique:
     - string: 'valeur'
     - int/float: valeur (un point pour les décimales)
     - vector2/3/4: (x, y) / (x, y, z) / (x, y, z, w)
     - color: (r, g, b, a)
     - bool: true/false
     - dropdown: 'option1' (la valeur est la string de l'option sélectionnée)
    
    Les autres types n'admettent aucune valeur à entrer (informations via connexions)
    */

    public void SaveGraph(string path)
    {
        NodeGraphData graphData = CreateGraphData();

        string json = ToJson(graphData);
        System.IO.File.WriteAllText(path, json);
    }

    string ToJson(NodeGraphData graphData)
    {
        var jo = new JObject();
        var nodesArray = new JArray();

        foreach (var node in graphData.nodes)
        {
            var nodeObj = new JObject();
            nodeObj["id"] = node.id;
            nodeObj["prefab"] = node.prefab;
            nodeObj["offsetX"] = node.offsetX;
            nodeObj["offsetY"] = node.offsetY;

            if (node.curveKeys != null)
                nodeObj["curveKeys"] = new JObject { ["keys"] = JArray.FromObject(node.curveKeys) };

            var inputsObj = new JObject();
            foreach (var input in node.inputs)
            {
                inputsObj[input.Key] = ToJToken(input.Value);
            }
            nodeObj["inputs"] = inputsObj;

            nodesArray.Add(nodeObj);
        }

        jo["nodes"] = nodesArray;

        var connectionsArray = new JArray();
        foreach (var connection in graphData.connections)
        {
            var connectionObj = new JObject();
            connectionObj["fromNode"] = connection.fromNode;
            connectionObj["fromOutputName"] = connection.fromOutputName;
            connectionObj["toNode"] = connection.toNode;
            connectionObj["toInputName"] = connection.toInputName;

            connectionsArray.Add(connectionObj);
        }
        jo["connections"] = connectionsArray;

        return jo.ToString(Formatting.Indented);
    }

    NodeGraphData CreateGraphData()
    {
        NodeGraphData graphData = new NodeGraphData();
        graphData.nodes = new List<NodeData>();
        graphData.connections = new List<ConnectionData>();

        foreach (NodeBehaviour node in GraphManager.Instance.nodes)
        {
            NodeData nodeData = new NodeData();
            nodeData.id = node.id.ToString();
            nodeData.prefab = node.prefabName;
            nodeData.inputs = new Dictionary<string, Variant>();
            nodeData.offsetX = node.transform.position.x / GraphManager.Instance.currentZoom + GraphManager.Instance.currentOffset.x;
            nodeData.offsetY = node.transform.position.y / GraphManager.Instance.currentZoom + GraphManager.Instance.currentOffset.y;

            if (node.prefabName == "CurveNode")
            {
                CurveNode curveNode = node as CurveNode;
                // Sauvegarder les points de la courbe dans les inputs du node
                if (curveNode.curve != null)
                {
                    List<CurveKeyData> curveKeys = new List<CurveKeyData>();
                    foreach (Keyframe key in curveNode.curve.keys)
                    {
                        CurveKeyData keyData = new CurveKeyData();
                        keyData.time = key.time;
                        keyData.value = key.value;
                        keyData.inTangent = key.inTangent;
                        keyData.outTangent = key.outTangent;
                        keyData.inWeight = key.inWeight;
                        keyData.outWeight = key.outWeight;
                        curveKeys.Add(keyData);
                    }

                    nodeData.curveKeys = curveKeys;
                }
            }

            foreach (ConnectorBehaviour input in node.inputConnections)
            {
                if (input.multiInput == null)
                {
                    if (!nodeData.inputs.ContainsKey(input.connectionName))
                        nodeData.inputs.Add(input.connectionName, new Variant());
                    continue;
                }

                Variant inputValue = input.multiInput.GetVariant();

                if (inputValue != null)
                    nodeData.inputs.Add(input.connectionName, inputValue);
            }

            graphData.nodes.Add(nodeData);

            foreach (ConnectorBehaviour input in node.inputConnections)
            {
                if (input.IsConnected())
                {
                    ConnectionData connectionData = new ConnectionData();
                    connectionData.toNode = input.node.id.ToString();
                    connectionData.toInputName = input.connectionName;

                    ConnectorBehaviour connectedOutput = input.multipleConnectedTo[0];

                    connectionData.fromNode = connectedOutput.node.id.ToString();
                    connectionData.fromOutputName = connectedOutput.connectionName;

                    graphData.connections.Add(connectionData);
                }
            }
        }

        return graphData;
    }

    public JToken ToJToken(Variant variant)
    {
        return variant.dataType switch
        {
            DataType.Float   => new JValue(variant.GetValue<float>()),
            DataType.Int     => new JValue(variant.GetValue<int>()),
            DataType.Bool    => new JValue(variant.GetValue<bool>()),
            DataType.String  => new JValue(variant.GetValue<string>()),
            DataType.Vector2 => new JObject { ["x"] = variant.GetValue<Vector2>().x, ["y"] = variant.GetValue<Vector2>().y },
            _                => JValue.CreateNull()
        };
    }
}

[System.Serializable]
public class NodeGraphData
{
    public List<NodeData> nodes;
    public List<ConnectionData> connections;
}

[System.Serializable]
public class NodeData
{
    public string id;
    public string prefab;
    public float offsetX;
    public float offsetY;
    public Dictionary<string, Variant> inputs;
    public List<CurveKeyData> curveKeys;
}

[System.Serializable]
public class ConnectionData
{
    public string fromNode;
    public string fromOutputName;
    public string toNode;
    public string toInputName;
}

[System.Serializable]
public class CurveKeyData
{
    public float time;
    public float value;
    public float inTangent;
    public float outTangent;
    public float inWeight;
    public float outWeight;
}
