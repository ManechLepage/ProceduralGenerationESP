using UnityEngine;
using UnityEngine.UI;

public class ColorNode : NodeBehaviour
{
    public GameObject colorImage;
    public RawImage colorImageComponent;
    public GameObject colorPicker;
    void Start()
    {
        colorImageComponent = colorImage.GetComponent<RawImage>();
    }
    public override Variant OnFire()
    {
        int r = GetInputValue("r").GetValue<int>();
        int g = GetInputValue("g").GetValue<int>();
        int b = GetInputValue("b").GetValue<int>();

        r = Mathf.Clamp(r, 0, 255);
        g = Mathf.Clamp(g, 0, 255);
        b = Mathf.Clamp(b, 0, 255);

        Debug.Log($"ColorNode: r={r}, g={g}, b={b}");

        return new Variant(new Color(r / 255f, g / 255f, b / 255f));
    }

    public void ChangeColor(Color color)
    {
        SetInputValue("r", new Variant((int)(color.r * 255)));
        SetInputValue("g", new Variant((int)(color.g * 255)));
        SetInputValue("b", new Variant((int)(color.b * 255)));

        if (colorImageComponent != null)
        {
            colorImageComponent.color = color;
        }
    }

    public void OpenColorPicker()
    {
        colorPicker.SetActive(!colorPicker.activeSelf);
    }
}
