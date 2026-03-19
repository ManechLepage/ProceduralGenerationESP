using UnityEngine;

public class RandomValueNode : NodeBehaviour
{
    public override Variant OnFire()
    {
        Vector2 range = GetInputValue("range").GetValue<Vector2>();
        float randomValue = Random.Range(range.x, range.y);

        return new Variant(randomValue);
    }
}
