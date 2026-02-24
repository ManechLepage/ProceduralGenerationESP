using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

[CreateAssetMenu(fileName = "New Node", menuName = "Node")]
public class Node : ScriptableObject
{
    public string nodeName;
    public List<InputOutputType> inputs = new List<InputOutputType>();
    public List<string> inputNames = new List<string>();
    public List<InputOutputType> outputs = new List<InputOutputType>();
}

public enum NodeType
{
    Algorithm,
    Function,
    Output
}

public enum InputOutputType
{
    Int,
    Float,
    SliderFloat,
    Vector2,
    Heightmap,
    Texture,
    Color,
    Gradient,
    Curve
}
