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

    public void DisableInputs()
    {
        foreach (TMP_InputField inputField in inputFields)
        {
            inputField.textComponent.color = Color.gray;
            inputField.interactable = false;
        }
        if (boolToggle != null)
        {
            boolToggle.GetComponent<Image>().color = Color.gray;
            boolToggle.interactable = false;
        }
    }

    public void EnableInputs()
    {
        foreach (TMP_InputField inputField in inputFields)
        {
            inputField.textComponent.color = Color.white;
            inputField.interactable = true;
        }
        if (boolToggle != null)
        {
            boolToggle.GetComponent<Image>().color = Color.white;
            boolToggle.interactable = true;
        }
    }

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

    public void SetVariant(Variant value)
    {
        switch (value.dataType)
        {
            case DataType.Int:
                inputFields[0].text = value.asInt.ToString();
                break;
            case DataType.Float:
                inputFields[0].text = value.asFloat.ToString(CultureInfo.InvariantCulture);
                break;
            case DataType.String:
                inputFields[0].text = value.asString;
                break;
            case DataType.Bool:
                boolToggle.isOn = value.asBool;
                break;
            case DataType.Vector2:
                inputFields[0].text = value.asVector2.x.ToString(CultureInfo.InvariantCulture);
                inputFields[1].text = value.asVector2.y.ToString(CultureInfo.InvariantCulture);
                break;
            case DataType.Vector3:
                inputFields[0].text = value.asVector3.x.ToString(CultureInfo.InvariantCulture);
                inputFields[1].text = value.asVector3.y.ToString(CultureInfo.InvariantCulture);
                inputFields[2].text = value.asVector3.z.ToString(CultureInfo.InvariantCulture);
                break;
        }
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
