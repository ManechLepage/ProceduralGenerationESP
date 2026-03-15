using UnityEngine;

public class RandomValueNode : NodeBehaviour
{
    public override Variant Fire()
    {
        Vector2 range = GetInputValue("range").GetValue<Vector2>();
        float randomValue = Random.Range(range.x, range.y);

        Debug.Log($"RandomValueNode: range=({range.x}, {range.y}), randomValue={randomValue}");

        return new Variant(randomValue);
    }
}
