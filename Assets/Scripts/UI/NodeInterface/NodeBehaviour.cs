using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine.EventSystems;

public class NodeBehaviour : MonoBehaviour
{
    public GameObject Node;
    public List<NodeParameter> inputParameters = new List<NodeParameter>();
    public List<NodeParameter> outputParameters = new List<NodeParameter>();

    void Start()
    {
        foreach (NodeParameter param in inputParameters)
        {
            Connection connection = new Connection();
            connection.targetNode = this;
            connection.targetParameter = param;
            param.connection = connection;

            if (param.inputBehaviour != null)
            {
                param.OnValueChange(param.inputBehaviour.GetFloatValue());
                param.inputBehaviour.inputField.onValueChanged.AddListener((value) =>
                {
                    param.OnValueChange(param.inputBehaviour.GetFloatValue());
                });
            }
        }
    }
}

[System.Serializable]
public class NodeParameter
{
    public string name;
    public float value;
    public Connection connection;

    [Space]
    public SingleInputBehaviour inputBehaviour;

    public void OnValueChange(float newValue)
    {
        value = newValue;
        Debug.Log($"OnValueChange {name}: {value}");
    }
}

[System.Serializable]
public class Connection
{
    public NodeBehaviour sourceNode;
    public NodeParameter sourceParameter;
    public NodeBehaviour targetNode;
    public NodeParameter targetParameter;
}
