using UnityEngine;

public class ColorNode : NodeBehaviour
{
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
}
