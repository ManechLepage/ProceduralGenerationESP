using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

public class AddHeightmapsNode : NodeBehaviour
{
    public override async Task<Variant> OnFire()
    {
        List<List<float>> x = (await GetInputValue("heightmap1")).GetValue<List<List<float>>>();
        List<List<float>> y = (await GetInputValue("heightmap2")).GetValue<List<List<float>>>();

        if (x != null && x.Count > 0 && y != null && y.Count > 0)
        {
            List<List<float>> result = new List<List<float>>();
            for (int i = 0; i < x.Count; i++)
            {
                List<float> row = new List<float>();
                for (int j = 0; j < x[i].Count; j++)
                {
                    row.Add(x[i][j] + y[i][j]);
                }
                result.Add(row);
            }

            return new Variant(result);
        }
        else if (y != null && y.Count > 0)
        {
            return new Variant(y);
        }
        else if (x != null && x.Count > 0)
        {
            return new Variant(x);
        }

        return new Variant(new List<List<float>>());
    }
}
