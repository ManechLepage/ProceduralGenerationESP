using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

public class ApplyCurveNode : NodeBehaviour
{
    public override async Task<Variant> OnFire()
    {
        List<List<float>> heightmap = (await GetInputValue("heightmap")).GetValue<List<List<float>>>();
        AnimationCurve curve = await GetAnimationCurve();

        List<List<float>> modifiedHeightMap = new List<List<float>>();

        for (int y = 0; y < heightmap.Count; y++)
        {
            List<float> modifiedRow = new List<float>();
            for (int x = 0; x < heightmap[y].Count; x++)
            {
                float originalValue = heightmap[y][x];
                float modifiedValue = curve.Evaluate(originalValue);
                modifiedRow.Add(modifiedValue);
            }
            modifiedHeightMap.Add(modifiedRow);
        }

        return new Variant(modifiedHeightMap);
    }

    public async Task<AnimationCurve> GetAnimationCurve()
    {
        if (GetInputConnection("curve").IsConnected())
            return (await GetInputValue("curve")).GetValue<AnimationCurve>();
        else
            return AnimationCurve.Linear(0, 0, 1, 1);
    }
}
