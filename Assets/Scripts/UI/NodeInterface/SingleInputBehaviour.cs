using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

public class SingleInputBehaviour : MonoBehaviour
{
    public SingleInputType inputType;
    public TMP_InputField inputField;
    public int GetIntValue()
    {
        int value;
        if (int.TryParse(inputField.text, out value))
        {
            return value;
        }
        return 0;
    }

    public float GetFloatValue()
    {
        float value;
        if (float.TryParse(inputField.text, out value))
        {
            return value;
        }
        return 0f;
    }
}

public enum SingleInputType
{
    Int,
    Float
}
