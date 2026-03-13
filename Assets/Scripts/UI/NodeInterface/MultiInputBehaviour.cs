using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Globalization;
using UnityEngine.UI;

public class MultiInputBehaviour : MonoBehaviour
{
    public DataType dataType;
    public List<TMP_InputField> inputFields = new List<TMP_InputField>();
    public Toggle boolToggle;

    public Variant GetVariant()
    {
        Variant value = new Variant();
        value.dataType = dataType;

        switch (dataType)
        {
            case DataType.Int:
                value.asInt = ParseInt(inputFields[0]);
                break;
            case DataType.Float:
                value.asFloat = ParseFloat(inputFields[0]);
                break;
            case DataType.String:
                value.asString = ParseString(inputFields[0]);
                break;
            case DataType.Bool:
                value.asBool = boolToggle.isOn;
                break;
            case DataType.Vector2:
                value.asVector2 = new Vector2(ParseFloat(inputFields[0]), ParseFloat(inputFields[1]));
                break;
            case DataType.Vector3:
                value.asVector3 = new Vector3(ParseFloat(inputFields[0]), ParseFloat(inputFields[1]), ParseFloat(inputFields[2]));
                break;
        }

        return value;
    }

    int ParseInt(TMP_InputField inputField)
    {
        int value;
        string text = inputField.text;
        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
        {
            return value;
        }
        return 0;
    }

    float ParseFloat(TMP_InputField inputField)
    {
        float value;
        string txt = inputField.text.Replace(',', '.');
        if (float.TryParse(txt, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            return value;
        }
        return 0f;
    }

    string ParseString(TMP_InputField inputField)
    {
        return inputField.text;
    }
}
