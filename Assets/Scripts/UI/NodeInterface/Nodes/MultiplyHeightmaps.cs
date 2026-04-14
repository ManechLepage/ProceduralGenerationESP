using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

public class MultiplyHeightmaps : NodeBehaviour
{
    async public override Task<Variant> OnFire()
    {
        List<List<float>> x = (await GetInputValue("heightmap1")).GetValue<List<List<float>>>();
        List<List<float>> y = (await GetInputValue("heightmap2")).GetValue<List<List<float>>>();

        if (x != null && x.Count > 0 && y != null && y.Count > 0)
        {
            // S'assurer que les heightmaps ont la même taille
            if (x.Count != y.Count || x[0].Count != y[0].Count)
                return new Variant(new List<List<float>>());

            List<List<float>> result = new List<List<float>>();
            for (int i = 0; i < x.Count; i++)
            {
                List<float> row = new List<float>();
                for (int j = 0; j < x[i].Count; j++)
                {
                    row.Add(x[i][j] * y[i][j]);
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

    async public override Task<Vector2Int> GetTerrainSize()
    {
        ConnectorBehaviour heightmapInput1 = GetInputConnection("heightmap1");
        ConnectorBehaviour heightmapInput2 = GetInputConnection("heightmap2");

        if (heightmapInput1.IsConnected())
            return await heightmapInput1.multipleConnectedTo[0].node.GetTerrainSize();
        else if (heightmapInput2.IsConnected())
            return await heightmapInput2.multipleConnectedTo[0].node.GetTerrainSize();

        return Vector2Int.zero;
    }
}
