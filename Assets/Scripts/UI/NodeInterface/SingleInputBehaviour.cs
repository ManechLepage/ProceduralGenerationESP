using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;
using System.Globalization; 

public class SingleInputBehaviour : MonoBehaviour
{
    public SingleInputType inputType;
    public TMP_InputField inputField;
    public int GetIntValue()
    {
        int value;
        if (int.TryParse(inputField.text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
        {
            return value;
        }
        return 0;
    }

    public float GetFloatValue()
    {
        float value;
        string txt = inputField.text.Replace(',', '.');
        if (float.TryParse(txt, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
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
