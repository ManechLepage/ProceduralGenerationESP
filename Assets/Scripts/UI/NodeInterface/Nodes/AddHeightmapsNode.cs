using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

public class AddHeightmapsNode : NodeBehaviour
{
    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && IsFlagged())
        {
            UnpauseGeneration();
        }
    }
    
    public override async Task<Variant> OnFire()
    {
        List<List<float>> x = (await GetInputValue("heightmap1")).GetValue<List<List<float>>>();
        List<List<float>> y = (await GetInputValue("heightmap2")).GetValue<List<List<float>>>();

        List<List<float>> result = new List<List<float>>();

        ShowLoadingIcon(true);

        if (x != null && x.Count > 0 && y != null && y.Count > 0)
        {
            for (int i = 0; i < x.Count; i++)
            {
                List<float> row = new List<float>();
                for (int j = 0; j < x[i].Count; j++)
                {
                    row.Add(x[i][j] + y[i][j]);
                }
                result.Add(row);
            }
        }
        else if (y != null && y.Count > 0)
        {
            result = y;
        }
        else if (x != null && x.Count > 0)
        {
            result = x;
        }

        if (IsFlagged() && result.Count > 0)
        {
            TerrainManager.Instance.PreviewHeightMap(result);
            PauseGeneration();
            await WaitForUnpause();
        }

        ShowLoadingIcon(false);

        return new Variant(result);
    }
}
