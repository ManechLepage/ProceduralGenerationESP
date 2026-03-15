using UnityEngine;

public class FloatsToVector2Node : NodeBehaviour
{
    public override Variant Fire()
    {
        float x = GetInputValue("float1").GetValue<float>();
        float y = GetInputValue("float2").GetValue<float>();
        return new Variant(new Vector2(x, y));
    }
}
