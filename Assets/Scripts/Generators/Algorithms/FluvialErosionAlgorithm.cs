using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum FluvialErosionType
{
    D8,
    MFD,
    DInfinite
}

public class FluvialErosionAlgorithm : MonoBehaviour
{
    /*
    Implémentation d'un algorithme d'érosion fluviale sous format de 'flux accumulation', c'est à dire la simulation de
    l'accumulation de l'eau à tous les points du terrain pour éroder par la suite.

    Le 'flux accumulation' possède plusieurs variantes qui sont impémentées ici :
     - D8 : L'eau s'écoule uniquement vers le voisin le plus bas, ce qui crée des rivières très linéaires.
     - MFD (Multiple Flow Direction) : L'eau s'écoule vers tous les voisins plus bas, avec une répartition du flux basée sur la pente.
     - D-Infinite : L'eau s'écoule vers les deux voisins les plus bas, avec une répartition du flux 
                    basée sur l'angle de la pente, ce qui crée des rivières plus naturelles.
    */

    // Directions pour atteindre les voisins, de l'est jusqu'au sud-est dans le sens antihoraire, utilisées pour l'algorithme D-Infinite.
    Vector2Int[] directions = new Vector2Int[]
    {
        new Vector2Int(1, 0),   
        new Vector2Int(1, 1),   
        new Vector2Int(0, 1),   
        new Vector2Int(-1, 1),  
        new Vector2Int(-1, 0),  
        new Vector2Int(-1, -1), 
        new Vector2Int(0, -1),  
        new Vector2Int(1, -1)   
    };

    void Awake()
    {
        AlgorithmRegistry.Instance.Register("FEA");
    }

    public void ApplyErosion(List<List<float>> heightMap, FluvialErosionSettings settings)
    {
        /*
        L'érosion est effectuée en une seule itération, en suivant les étapes suivantes :
         1 - Remplissage des puits d'eau pour éviter les problèmes de flux.
         2 - Calcul des directions de flux pour chaque cellule du terrain, en fonction de la pente et du type d'érosion choisi.
         3 - Simulation de l'accumulation d'eau à chaque cellule en fonction des directions, en partant de la cellule la plus
             haute vers la cellule la plus basse.
         4 - Éroder le terrain selon le logarithme de l'accumulation d'eau et de la pente, en appliquant
             une intensité plus élevée pour les rivières (zones avec beaucoup d'eau).

        Paramètres :
         - 'heightMap' : Le heightmap du terrain à modifier.
         - 'settings' : Les paramètres de l'algorithme d'érosion fluviale.
        */

        FillSinks(heightMap);
        List<FlowTarget>[,] flowTargets = CalculateFlowTargets(heightMap, settings);
        float[,] waterMap = new float[heightMap.Count, heightMap[0].Count];

        // Initialiser la quantité d'eau à chaque cellule avec une valeur de base.
        for (int i = 0; i < heightMap.Count; i++)
        {
            for (int j = 0; j < heightMap[0].Count; j++)
            {
                waterMap[i, j] = settings.waterQuantity;
            }
        }

        FlowAccumulation(heightMap, flowTargets, waterMap);
        ErodeHeightMap(heightMap, flowTargets, waterMap, settings);
    }

    public void ErodeHeightMap(List<List<float>> heightMap, List<FlowTarget>[,] flowTargets, float[,] waterMap, FluvialErosionSettings settings)
    {
        /*
        Érode le heightmap en fonction de l'accumulation d'eau et de la pente à chaque cellule.
         - La pente est calculée en fonction des voisins vers lesquels l'eau s'écoule.
         - L'érosion est ensuite calculée en fonction du logarithme de l'accumulation d'eau et de la pente,
           avec une intensité plus élevée pour les rivières.
         - Enfin, la hauteur de chaque cellule est réduite en fonction de l'érosion calculée.
        
        Paramètres :
         - 'heightMap' : Le heightmap du terrain à modifier.
         - 'flowTargets' : Les directions de flux pour chaque cellule, calculées précédemment.
         - 'waterMap' : L'accumulation d'eau à chaque cellule, calculée précédemment.
         - 'settings' : Les paramètres de l'algorithme d'érosion fluviale, utilisés pour ajuster l'intensité de l'érosion et les seuils de rivière.
        */

        for (int x = 0; x < heightMap.Count; x++)
        {
            for (int y = 0; y < heightMap[0].Count; y++)
            {
                // Calcul de la pente pondérée selon tous les voisins vers lesquels l'eau s'écoule.
                float slope = 0f;
                foreach (FlowTarget cellFlowTarget in flowTargets[x, y])
                {
                    int targetX = x + cellFlowTarget.x;
                    int targetY = y + cellFlowTarget.y;
                    if (targetX < 0 || targetX >= heightMap.Count || targetY < 0 || targetY >= heightMap[0].Count)
                    {
                        continue;
                    }
                    slope += (heightMap[x][y] - heightMap[targetX][targetY]) * cellFlowTarget.weight;
                }

                slope = Mathf.Max(0f, slope);
                slope = Mathf.Sqrt(slope + 1f) - 1f;

                // Calcul de l'érosion effectuée. Le double logarithme permet d'avoir des différences d'érosion plus jolies.
                float erosionAmount = settings.erosionIntensity / 10f * Mathf.Log(1f + Mathf.Log(1f + waterMap[x, y])) * slope;

                if (waterMap[x, y] > settings.riverThreshold * 10f)
                {
                    erosionAmount *= settings.riverIntensity;
                }

                // Vérifier les limites de l'érosion pour ne pas casser le terrain.
                float minHeight = 0f;
                erosionAmount = Mathf.Min(erosionAmount, heightMap[x][y] - minHeight);
                if (float.IsNaN(erosionAmount) || float.IsInfinity(erosionAmount)) erosionAmount = 0f;

                // Appliquer l'érosion
                heightMap[x][y] -= erosionAmount;
            }
        }
    }

    public float GetSlope(List<List<float>> heightMap, int x, int y)
    {
        /*
        Calculer la pente à un point selon le voisin qui crée la pente la plus élevée.
         - 'heightMap' : Le heightmap du terrain.
         - 'x' et 'y' : Les coordonnées de la cellule pour laquelle calculer la pente.
         - return : La pente maximale vers les voisins, utilisée pour calculer l'érosion à ce point.
        */

        float currentHeight = heightMap[x][y];
        float maxSlope = 0f;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                int newX = x + dx;
                int newY = y + dy;
                
                // Ignorer les voisins en dehors du terrain
                if (newX >= 0 && newX < heightMap.Count && newY >= 0 && newY < heightMap[0].Count)
                {
                    float neighborHeight = heightMap[newX][newY];
                    float slope = Mathf.Abs(currentHeight - neighborHeight);
                    maxSlope = Mathf.Max(maxSlope, slope);
                }
            }
        }

        return maxSlope;
    }

    public void FlowAccumulation(List<List<float>> heightMap, List<FlowTarget>[,] flowTargets, float[,] waterMap)
    {
        /*
        Aspect principal de l'algorithme, qui simule le déplacement de l'eau à partir du point le plus haut jusqu'au point le plus bas,
        pour que l'eau soit suivie logiquement.
         - 'heightMap' : Le heightmap du terrain.
         - 'flowTargets' : Les directions de flux pour chaque cellule, calculées précédemment.
         - 'waterMap' : L'accumulation d'eau à chaque cellule, qui sera mise à jour en fonction des flux.
        */

        Vector2Int mapSize = new Vector2Int(waterMap.GetLength(0), waterMap.GetLength(1));

        // Traiter les cellules du heightmap de la plus haute à la plus basse pour simuler l'écoulement de l'eau.
        Vector2Int[] processingOrder = GetSortedHeightMapCells(heightMap);

        foreach (Vector2Int cell in processingOrder)
        {
            // Propager l'eau de la cellule vers les cellules vers lesquelles elle s'écoule, en fonction des directions de flux calculées.
            List<FlowTarget> cellFlowTargets = flowTargets[cell.x, cell.y];
            foreach (FlowTarget target in cellFlowTargets)
            {
                Vector2Int flowDir = new Vector2Int(target.x, target.y);
                if (flowDir != Vector2Int.zero)
                {
                    int targetX = cell.x + flowDir.x;
                    int targetY = cell.y + flowDir.y;
                    if (targetX >= 0 && targetX < mapSize.x && targetY >= 0 && targetY < mapSize.y)
                    {
                        // Tenir compte du poids de chaque direction dans le déplacement de l'eau.
                        waterMap[targetX, targetY] += waterMap[cell.x, cell.y] * target.weight;
                    }
                }
            }
        }
    }

    public Vector2Int[] GetSortedHeightMapCells(List<List<float>> heightMap)
    {
        /*
        Algorithme pour trier les cellules du heightmap selon leur hauteur.
         - 'heightMap' : Le heightmap du terrain.
         - return : Un tableau de Vector2Int représentant les coordonnées des cellules triées de la plus haute à la plus basse.
        */

        List<Vector2Int> cells = new List<Vector2Int>();

        for (int x = 0; x < heightMap.Count; x++)
        {
            for (int y = 0; y < heightMap[0].Count; y++)
            {
                cells.Add(new Vector2Int(x, y));
            }
        }

        // Trier les cellules.
        cells.Sort((a, b) => heightMap[b.x][b.y].CompareTo(heightMap[a.x][a.y]));

        return cells.ToArray();
    }

    public List<FlowTarget>[,] CalculateFlowTargets(List<List<float>> heightMap, FluvialErosionSettings settings)
    {
        /*
        Selon les différents algorithmes de settings.erosionType, trouver les directions de flux.
        Rappel :
         - D8 : L'eau s'écoule uniquement vers le voisin le plus bas, ce qui crée des rivières très linéaires.
         - MFD (Multiple Flow Direction) : L'eau s'écoule vers tous les voisins plus bas, avec une répartition du flux basée sur la pente.
         - D-Infinite : L'eau s'écoule vers les deux voisins les plus bas, avec une répartition du flux basée sur l'angle de la pente,
                        ce qui crée des rivières plus naturelles.
        
        Paramètres :
         - 'heightMap' : Le heightmap du terrain.
         - 'settings' : Les paramètres de l'algorithme d'érosion fluviale, utilisés pour choisir le type d'érosion et ajuster les flux.
         - return : Un tableau 2D de listes de FlowTarget, représentant les directions de flux et leurs poids pour chaque cellule du terrain.
        */

        // Note : le struct FlowTarget est défini plus bas, juste après FluvialErosionAlgorithm.
        List<FlowTarget>[,] flowTargets = new List<FlowTarget>[heightMap.Count, heightMap[0].Count];

        for (int x = 0; x < heightMap.Count; x++)
        {
            for (int y = 0; y < heightMap[0].Count; y++)
            {
                Vector2Int currentPos = new Vector2Int(x, y);
                float currentHeight = heightMap[x][y];
                
                // Assigner un maximum de voisins selon l'algorithme.
                int maxTargets = 1;
                if (settings.erosionType == FluvialErosionType.MFD)
                    maxTargets = 8;
                
                if (settings.erosionType != FluvialErosionType.DInfinite)
                {
                    // Le D8 et le MFD utilisent tous deux la même méthode pour trouver les voisins, et le D-infinite utilise une autre logique, plus bas.

                    List<Vector2Int> neighbors = GetNeighbors(heightMap, currentPos);
                    List<FlowTarget> cellFlowTargets = new List<FlowTarget>();

                    // Regarder chaque voisin et la pente qu'il fait. Plus la pente est élevée, plus le poids du flux vers ce voisin est élevé.
                    foreach (Vector2Int neighbor in neighbors)
                    {
                        Vector2Int direction = new Vector2Int(neighbor.x - x, neighbor.y - y);
                        float slope = currentHeight - heightMap[neighbor.x][neighbor.y];
                        if (direction.x != 0 && direction.y != 0)
                            slope /= Mathf.Sqrt(2f);

                        if (slope > 0)
                        {
                            slope = Mathf.Pow(slope, 1.1f);
                            cellFlowTargets.Add(new FlowTarget { x = direction.x, y = direction.y, weight = slope });
                        }
                    }

                    // Ordonner les voisins selon leur pente, du plus élevé au plus bas.
                    cellFlowTargets.Sort((a, b) => b.weight.CompareTo(a.weight));

                    if (cellFlowTargets.Count == 0)
                    {
                        flowTargets[x, y] = cellFlowTargets;
                        continue;
                    }

                    if (cellFlowTargets.Count > 1)
                    {
                        // Conserver uniquement les targets qui ont la plus grande pente
                        float totalHeightDiff = 0f;

                        if (cellFlowTargets.Count > maxTargets)
                        {
                            cellFlowTargets.RemoveRange(maxTargets, cellFlowTargets.Count - maxTargets);
                        }
                        
                        // Calculer la somme des poids pour ensuite normaliser.
                        foreach (FlowTarget cellFlowTarget in cellFlowTargets)
                        {
                            totalHeightDiff += cellFlowTarget.weight;
                        }

                        float impactFactor = 1f / totalHeightDiff;
                        for (int i = 0; i < cellFlowTargets.Count; i++)
                        {
                            // Assigner le nouveau poids normalisé
                            FlowTarget t = cellFlowTargets[i];
                            t.weight = impactFactor * t.weight;
                            cellFlowTargets[i] = t;
                        }
                    }
                    else
                    {
                        // Pour le D8 (un seul voisin), assigner un poids de 1 à l'unique meilleur voisin.
                        FlowTarget t = cellFlowTargets[0];
                        t.weight = 1f;
                        cellFlowTargets[0] = t;
                    }

                    flowTargets[x, y] = cellFlowTargets;
                }
                else
                {
                    // Pour l'algorithme D-infinite, trouver le gradient et l'angle pour cibler deux voisins avec une interpolation linéaire pour leurs poids.
                    Vector2 gradient = CalculateGradientAtPoint(heightMap, x, y);

                    if (gradient.magnitude < 1e-6f)
                    {
                        // Si le gradient est presque nul, on fais couler toute l'eau vers le voisin le plus bas.
                        var steep = GetNeighbors(heightMap, currentPos, onlySteepest: true);
                        if (steep.Count > 0)
                            flowTargets[x,y] = new List<FlowTarget> { new FlowTarget { x = steep[0].x - x, y = steep[0].y - y, weight = 1f } };
                        else
                            flowTargets[x,y] = new List<FlowTarget>();
                        continue;
                    }

                    // Calculer l'angle du gradient pour déterminer les deux voisins vers lesquels l'eau coule.
                    float angle = Mathf.Atan2(gradient.y, gradient.x);
                    if (angle < 0)
                        angle += 2 * Mathf.PI;
                        
                    float sector = angle / (Mathf.PI / 4f);
                    int neighborIndex = Mathf.FloorToInt(sector) % 8;

                    // À l'aide du array des directions, trouver les deux voisins.
                    Vector2Int neighbor1 = directions[neighborIndex];
                    Vector2Int neighbor2 = directions[(neighborIndex + 1) % 8];

                    Vector2Int neighbor1Pos = new Vector2Int(x + neighbor1.x, y + neighbor1.y);
                    Vector2Int neighbor2Pos = new Vector2Int(x + neighbor2.x, y + neighbor2.y);

                    // Vérifier que les directions d'écoulement sont dans les bornes
                    bool validNeighbor1 = neighbor1Pos.x >= 0 && neighbor1Pos.x < heightMap.Count && neighbor1Pos.y >= 0 && neighbor1Pos.y < heightMap[0].Count;
                    bool validNeighbor2 = neighbor2Pos.x >= 0 && neighbor2Pos.x < heightMap.Count && neighbor2Pos.y >= 0 && neighbor2Pos.y < heightMap[0].Count;

                    // Calculer la différence entre l'angle arrondi du premier voisin et l'angle réel pour faire une interpolation linéaire des poids.
                    float angle_i = neighborIndex * (Mathf.PI / 4f);
                    float t = (angle - angle_i) / (Mathf.PI / 4f);

                    float weight1;
                    float weight2;

                    // Pour les cas ou certains voisins sont hors des bornes, assigner tout le poids au voisin valide, ou 0 si les deux sont invalides.
                    if (validNeighbor1 && validNeighbor2)
                    {
                        weight1 = 1f - t;
                        weight2 = t;
                    }
                    else if (validNeighbor1)
                    {
                        weight1 = 1f;
                        weight2 = 0f;
                    }
                    else if (validNeighbor2)
                    {
                        weight1 = 0f;
                        weight2 = 1f;
                    }
                    else
                    {
                        weight1 = 0f;
                        weight2 = 0f;
                    }

                    List<FlowTarget> cellFlowTargets = new List<FlowTarget>();

                    // Ajouter uniquement les voisins valides
                    if (validNeighbor1)
                        cellFlowTargets.Add(new FlowTarget { x = neighbor1.x, y = neighbor1.y, weight = weight1 });
                    
                    if (validNeighbor2)
                        cellFlowTargets.Add(new FlowTarget { x = neighbor2.x, y = neighbor2.y, weight = weight2 });

                    flowTargets[x, y] = cellFlowTargets;
                }
            }
        }
        return flowTargets;
    }

    public Vector2 CalculateGradientAtPoint(List<List<float>> heightMap, int x, int y)
    {
        /*
        Calcul du gradient selon les dérivées partielles en utilisant les
        8 voisins autour d'un point.
         - 'heightMap' : Le heightmap du terrain.
         - 'x' et 'y' : Les coordonnées de la cellule pour laquelle calculer le gradient.
         - return : Un Vector2 représentant le gradient en x et y, utilisé pour déterminer les directions de flux dans l'algorithme D-Infinite.
        */

        float hNW = SampleHeightMap(heightMap, x - 1, y + 1);
        float hN  = SampleHeightMap(heightMap, x,     y + 1);
        float hNE = SampleHeightMap(heightMap, x + 1, y + 1);
        float hW  = SampleHeightMap(heightMap, x - 1, y);
        float hC  = SampleHeightMap(heightMap, x,     y);
        float hE  = SampleHeightMap(heightMap, x + 1, y);
        float hSW = SampleHeightMap(heightMap, x - 1, y - 1);
        float hS  = SampleHeightMap(heightMap, x,     y - 1);
        float hSE = SampleHeightMap(heightMap, x + 1, y - 1);

        float gradientX = ((hNE + 2f*hE + hSE) - (hNW + 2f*hW + hSW)) / 8f;
        float gradientY = ((hSW + 2f*hS + hSE) - (hNW + 2f*hN + hNE)) / 8f;

        return new Vector2(gradientX, gradientY);
    }

    public float SampleHeightMap(List<List<float>> heightMap, int x, int y)
    {
        /*
        Obtenir la hauteur d'une cellule du heightmap en vérifiant les limites pour éviter les erreurs d'index.
         - 'heightMap' : Le heightmap du terrain.
         - 'x' et 'y' : Les coordonnées de la cellule à échantillonner.
         - return : La hauteur de la cellule, ou la hauteur de la cellule la plus proche si les coordonnées sont en dehors des limites du heightmap.
        */

        x = Mathf.Max(0, Mathf.Min(x, heightMap.Count - 1));
        y = Mathf.Max(0, Mathf.Min(y, heightMap[0].Count - 1));
        return heightMap[x][y];
    }

    public void FillSinks(List<List<float>> heightMap)
    {
        /*
        L'algorithme de remplissage des puits d'eau regarde tous les points et, si tous ses voisins sont plus haut, monte le point à la hauteur
        du voisin le plus bas. Cela permet d'éviter des problèmes de flux d'eau bloqués.
         - 'heightMap' : Le heightmap du terrain à modifier, qui sera mis à jour pour remplir les puits d'eau.
        
        Pour améliorer le réalisme de la fonction, il faudrait également implémenter un FillSinks qui ne regarde pas uniquement les voisins
        à côté, mais ceux plus loin pour éviter les grands puits qui sont ignorés par cette méthode. Cependant, cela serait plus coûteux en performance.
        */

        for (int x = 0; x < heightMap.Count; x++)
        {
            for (int y = 0; y < heightMap[0].Count; y++)
            {
                Vector2Int currentPos = new Vector2Int(x, y);
                List<Vector2Int> neighbors = GetNeighbors(heightMap, currentPos);

                float currentHeight = heightMap[x][y];
                bool isSink = true;
                float lowestNeighborHeight = float.MaxValue;

                foreach (Vector2Int neighbor in neighbors)
                {
                    if (heightMap[neighbor.x][neighbor.y] < currentHeight)
                    {
                        isSink = false;
                        break;
                    }
                    lowestNeighborHeight = Mathf.Min(lowestNeighborHeight, heightMap[neighbor.x][neighbor.y]);
                }

                if (isSink)
                {
                    heightMap[x][y] = lowestNeighborHeight;
                }
            }
        }
    }

    public List<Vector2Int> GetNeighbors(List<List<float>> heightMap, Vector2Int pos, bool onlySteepest = false)
    {
        /*
        Obtenir les voisins d'une cellule du heightmap en vérifiant les limites pour éviter les erreurs d'index.
         - 'heightMap' : Le heightmap du terrain.
         - 'pos' : Les coordonnées de la cellule pour laquelle obtenir les voisins.
         - 'onlySteepest' : Si vrai, ne retourner que le voisin le plus bas (utilisé pour l'algorithme D-Infinite), sinon retourner tous les voisins plus bas.
        */

        List<Vector2Int> neighbors = new List<Vector2Int>();

        float steepestNeighborSlope = -1f;
        Vector2Int steepestNeighbor = new Vector2Int(-1, -1);

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                int newX = pos.x + dx;
                int newY = pos.y + dy;

                // Vérifier que le point est dans les limites
                if (newX >= 0 && newX < heightMap.Count && newY >= 0 && newY < heightMap[0].Count)
                {
                    if (!onlySteepest)
                    {
                        // Ajouter le voisin s'il n'y a pas l'option onlySteepest
                        neighbors.Add(new Vector2Int(newX, newY));
                    }
                    else
                    {
                        // Comparer les pentes pour trouver le voisin qui produit la plus grande pente négative (le plus bas)
                        float neighborHeight = heightMap[newX][newY];
                        float slope = (heightMap[pos.x][pos.y] - neighborHeight) / Mathf.Sqrt(dx * dx + dy * dy);
                        if (slope > steepestNeighborSlope)
                        {
                            steepestNeighborSlope = slope;
                            steepestNeighbor = new Vector2Int(newX, newY);
                        }
                    }
                }
            }
        }

        if (onlySteepest && steepestNeighbor.x != -1)
        {
            neighbors.Add(steepestNeighbor);
        }

        return neighbors;
    }
}

public struct FlowTarget
{
    /*
    Structure temporaire pour stocker les directions de flux et leurs poids. 
    */
    
    public int x;  // Direction x du flux
    public int y;  // Direction y du flux
    public float weight;  // Poids du flux dans cette direction
}

[System.Serializable]
public class FluvialErosionSettings
{
    /*
    Paramètres de l'érosion fluviale.    
    */
    
    public float waterQuantity = 1f;  // Quantité d'eau initiale à chaque cellule, qui sera ensuite accumulée en fonction des flux.
    public float erosionIntensity = 1f;  // Intensité de l'érosion des sédiments
    public FluvialErosionType erosionType = FluvialErosionType.D8;  // Type d'algorithme de flux pour déterminer la façon
                                                                    // dont on sélectionne les directions d'écoulement.
    public float riverThreshold = 1f;  // Eau nécessaire pour que le point soit considéré comme une rivière.
    public float riverIntensity = 2f;  // Multiple de l'intensité d'érosion pour les rivières.
}
