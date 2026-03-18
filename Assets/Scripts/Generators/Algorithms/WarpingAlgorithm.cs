using UnityEngine;
using System.Collections.Generic;

public class WarpingAlgorithm : MonoBehaviour
{
    /*
    À venir...
    Cet algorithme aura pour but de modifier les positions x et y des point avant de les mettre dans d'autres algorithmes
    pour créer des terrains plus variés.

    Par exemple, un algorithme de FBM ou de Voronoi pourrait être appliqué aux positions qui sont ensuite passées dans ces algorithmes.
    C'est la logique de la composition de fonction : FBM(pos + FBM(pos + FBM(pos))) pour créer des terrains plus complexes.
    */

    public List<List<Vector2>> GetWarpedDomainMap(Vector2Int size, WarpingSettings settings = null)
    {
        settings = settings ?? new WarpingSettings();

        List<List<Vector2>> warpedDomainMap = new List<List<Vector2>>();

        float flowScale = settings.flowScale * settings.scale / 50f;
        float noiseScale = settings.noiseScale * settings.scale / 50f;

        for (int x = 0; x < size.x; x++)
        {
            List<Vector2> column = new List<Vector2>();

            for (int y = 0; y < size.y; y++)
            {
                float n = Mathf.PerlinNoise(x * flowScale + settings.offset.x,
                                                y * flowScale + settings.offset.y);

                float angle = n * Mathf.PI * 2f;

                Vector2 flow = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                float distortion = Mathf.PerlinNoise(
                    (x + 200f) * noiseScale + settings.offset.y,
                    (y + 200f) * noiseScale + settings.offset.x
                ) * 2f - 1f;

                flow += new Vector2(-flow.y, flow.x) * distortion;

                Vector2 domainValue = new Vector2(
                    x + flow.x * settings.strength,
                    y + flow.y * settings.strength
                );

                column.Add(domainValue);
            }

            warpedDomainMap.Add(column);
        }

        return warpedDomainMap;
    }
}

[System.Serializable]
public class WarpingSettings
{
    public int seed = 0;
    public float strength = 0.5f;
    public float scale = 1f;
    public float flowScale = 0.8f;
    public float noiseScale = 2f;
    public Vector2 offset = Vector2.zero;
}
