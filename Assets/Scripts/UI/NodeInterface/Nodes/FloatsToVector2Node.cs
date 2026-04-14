using UnityEngine;
using System.Threading.Tasks;

public class FloatsToVector2Node : NodeBehaviour
{
    async public override Task<Variant> OnFire()
    {
        float x = (await GetInputValue("float1")).GetValue<float>();
        float y = (await GetInputValue("float2")).GetValue<float>();
        return new Variant(new Vector2(x, y));
    }
}
