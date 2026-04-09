using UnityEngine;
using System.Threading.Tasks;

public class RandomValueNode : NodeBehaviour
{
    async public override Task<Variant> OnFire()
    {
        Vector2 range = (await GetInputValue("range")).GetValue<Vector2>();
        float randomValue = Random.Range(range.x, range.y);

        return new Variant(randomValue);
    }
}
