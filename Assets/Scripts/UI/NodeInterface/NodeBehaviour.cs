using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine.EventSystems;

public class NodeBehaviour : MonoBehaviour
{
    public Node nodeData;

    public void Start()
    {
        if (nodeData != null)
        {
            LoadNode(nodeData);
        }
    }
    public void LoadNode(Node node)
    {

    }

}
