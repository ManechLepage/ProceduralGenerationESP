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
        StartStopwatch();

        if (x != null && x.Count > 0 && y != null && y.Count > 0)
        {
            // S'assurer que les heightmaps ont la même taille
            if (x.Count != y.Count || x[0].Count != y[0].Count)
                return new Variant(new List<List<float>>());

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

    async public override Task<Vector2Int> GetTerrainSize()
    {
        ConnectorBehaviour heightmapInput1 = GetInputConnection("heightmap1");
        ConnectorBehaviour heightmapInput2 = GetInputConnection("heightmap2");
        if (heightmapInput1.IsConnected() && heightmapInput2.IsConnected())
        {
            Vector2Int size1 = await heightmapInput1.multipleConnectedTo[0].node.GetTerrainSize();
            Vector2Int size2 = await heightmapInput2.multipleConnectedTo[0].node.GetTerrainSize();

            if (size1 != size2)
                return Vector2Int.zero;

            return size1;
        }
        else if (heightmapInput1.IsConnected())
            return await heightmapInput1.multipleConnectedTo[0].node.GetTerrainSize();
        else if (heightmapInput2.IsConnected())
            return await heightmapInput2.multipleConnectedTo[0].node.GetTerrainSize();

        return Vector2Int.zero;
    }
}
