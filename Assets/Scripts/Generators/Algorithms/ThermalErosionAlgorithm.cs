using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class ThermalErosionAlgorithm : MonoBehaviour
{
    /*
    Implémentation de l'algorithme d'érosion thermale.
    La logique de base est d'imiter l'effritement et l'effondrement des parois par le changement froid-chaud.
    Pour cet algorithme, une simplification efficace est de considérer simplement l'effritement et l'effondrement,
    sans le changement de température, ce qui donne tout de même un résultat réaliste.
    */

    void Awake()
    {
        AlgorithmRegistry.Instance.Register("TEA");
    }
    
    public void ApplyErosionStep(List<List<float>> heightMap, List<List<float>> bedrockMap, List<List<float>> sedimentMap, ThermalErosionSettings settings, float pixelDistance)
    {
        /*
        Deux algorithmes similaires peuvent être utilisés selon le paramètre settings.sedimentMap.

        Le premier, plus simple, effrite et fait tomber des sédiments directement.
         1) Faire la boucle de chaque voisin et vérifier ceux qui sont plus bas
         2) Selon le différentiel de hauteur, calculer l'angle et voir s'il répasse settings.talusAngle
         3) Déplacer une partie de sédiments dans les voisins qui respectent la condition 2)
        
        Le deuxième, plus complexe mais plus réaliste, utilise deux listes 2D bedrockMap et sedimentMap
         1) Pour chaque voisin, comparer la différence de hauteur et l'angle pour la liste de bedrockMap
         2) Si l'angle dépasse settings.talusAngle, transférer une partie de la bedrockMap en sédiment dans la sedimentMap
         3) Effectuer ensuite les mêmes étapes que l'algorithme 1 en déplaçant cette fois uniquement les sédiments
        
        Paramètres :
         - 'heightMap' : Le heightmap du terrain à éroder.
         - 'bedrockMap' : Une copie du heightmap utilisée pour simuler la roche mère, si settings.sedimentMap est activé.
         - 'sedimentMap' : Une liste 2D pour stocker les sédiments produits par l'érosion, si settings.sedimentMap est activé.
         - 'settings' : Les paramètres de l'érosion thermique.
         - 'pixelDistance' : La distance entre les pixels du heightmap, utilisée pour calculer les angles d'inclinaison.
        */

        int width = heightMap.Count;
        int height = heightMap[0].Count;

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Vector2Int currentPos = new Vector2Int(i, j);
                float currentHeight = settings.sedimentMap ? bedrockMap[i][j] + sedimentMap[i][j] : heightMap[i][j];

                // Calculer tous les voisins qui sont dans les limites
                List<Vector2Int> neighbors = GetNeighbors(heightMap, currentPos);

                // Facteur aléatoire influençant la pente pour plus de réalisme
                float randomFactor = UnityEngine.Random.Range(1f - settings.randomness, 1f + settings.randomness) / 50f;

                if (settings.sedimentMap)
                {
                    // Pour la version 2, transformer la couche de terrain d'en dessous en sédiments
                    float bedrockSlope = GetBedrockSlope(bedrockMap, currentPos, neighbors, pixelDistance);
                    if (bedrockSlope + randomFactor > settings.talusAngle)
                    {
                        float productionAmount = bedrockSlope * settings.talusProduction / 10f;

                        bedrockMap[i][j] -= productionAmount;
                        sedimentMap[i][j] += productionAmount;
                    }
                }

                if (!settings.sedimentMap || sedimentMap[i][j] != 0)
                {
                    foreach (Vector2Int neighbor in neighbors)
                    {
                        // Regarder si le voisin fais une pente assez raide pour qu'un transfert de sédiments s'effectue.

                        float neighborHeight = settings.sedimentMap ? bedrockMap[neighbor.x][neighbor.y] + sedimentMap[neighbor.x][neighbor.y] : heightMap[neighbor.x][neighbor.y];
                        float heightDifference = heightMap[i][j] - neighborHeight;
                        float diff = heightDifference / pixelDistance;

                        if (diff + randomFactor > settings.talusAngle)
                        {
                            float erosionAmount;
                            if (settings.sedimentMap)
                                erosionAmount = Mathf.Min(diff * settings.intensity / 10f, sedimentMap[i][j]);
                            else
                                erosionAmount = diff * settings.intensity / 10f;

                            if (settings.sedimentMap)
                            {
                                sedimentMap[i][j] -= erosionAmount;
                                sedimentMap[neighbor.x][neighbor.y] += erosionAmount;
                            }
                            
                            heightMap[i][j] -= erosionAmount;
                            heightMap[neighbor.x][neighbor.y] += erosionAmount;
                        }
                    }
                }
            }
        }
    }

    public List<Vector2Int> GetNeighbors(List<List<float>> heightMap, Vector2Int position)
    {
        /*
        Obtenir les voisins d'un point dans le heightmap, en vérifiant les limites du heightmap.
         - 'heightMap' : Le heightmap du terrain, utilisé pour vérifier les limites.
         - 'position' : La position centrale dont on veut obtenir les voisins.
         - Retourne une liste de positions des voisins valides.
         Note : cette fonction considère les 8 voisins (y compris diagonaux).
        */

        List<Vector2Int> neighbors = new List<Vector2Int>();

        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0) continue;

                Vector2Int neighborPos = new Vector2Int(
                    position.x + i,
                    position.y + j
                );

                // Ignorer les voisins hors des limites
                if (neighborPos.x < 0 || neighborPos.x >= heightMap.Count || neighborPos.y < 0 || neighborPos.y >= heightMap[0].Count)
                    continue;

                neighbors.Add(neighborPos);
            }
        }

        return neighbors;
    }

    public float GetBedrockSlope(List<List<float>> heightMap, Vector2Int position, List<Vector2Int> neighbors, float pixelDistance)
    {
        /*
        Calculer la pente maximale entre un point et ses voisins en utilisant uniquement la couche de bedrock, pour la version avec sedimentMap.
         - 'heightMap' : La couche de bedrock du terrain, utilisée pour calculer les pentes.
         - 'position' : La position centrale dont on veut calculer la pente.
         - 'neighbors' : La liste des voisins à comparer pour calculer les pentes.
         - 'pixelDistance' : La distance entre les pixels du heightmap, utilisée pour calculer les angles d'inclinaison.
         - Retourne la pente maximale entre le point central et ses voisins.
         Note : cette fonction est utilisée pour déterminer si un point doit produire des sédiments en fonction de l'angle de sa roche mère.
        */

        float minHeight = float.MaxValue;
        Vector2Int lowestNeighbor = new Vector2Int(-1, -1);

        foreach (Vector2Int neighbor in neighbors)
        {
            float neighborHeight = heightMap[neighbor.x][neighbor.y];
            if (neighborHeight < minHeight)
            {
                minHeight = neighborHeight;
                lowestNeighbor = neighbor;
            }
        }

        // Calculer la différence de hauteur et la pente selon le voisin le plus bas
        float heightDifference = heightMap[position.x][position.y] - heightMap[lowestNeighbor.x][lowestNeighbor.y];
        return heightDifference / pixelDistance;
    }

    public IEnumerator ApplyErosion(List<List<float>> heightMap, ThermalErosionSettings settings, float pixelDistance, Action<float, float> onProgress=null)
    {
        /*
        Fonction principale de l'application d'érosion thermique.
         - 'heightMap' : Le heightmap du terrain à éroder.
         - 'settings' : Les paramètres de l'érosion thermique.
         - 'pixelDistance' : Distance de référence entre les pixels pour calculer la pente
         - 'onProgress' : Fonction de callback permettant une barre de progrès ou une modification en temps réel de l'érosion
        */

        List<List<float>> sedimentMap = new List<List<float>>();
        List<List<float>> bedrockMap = new List<List<float>>();

        if (settings.sedimentMap)
        {
            // Création des tableaux seulement s'ils sont nécessaires.

            for (int i = 0; i < heightMap.Count; i++)
            {
                sedimentMap.Add(new List<float>());
                for (int j = 0; j < heightMap[0].Count; j++)
                {
                    sedimentMap[i].Add(0f);
                }
            }

            for (int i = 0; i < heightMap.Count; i++)
            {
                bedrockMap.Add(new List<float>());
                for (int j = 0; j < heightMap[0].Count; j++)
                {
                    bedrockMap[i].Add(heightMap[i][j]);
                }
            }
        }

        float startTime = Time.unscaledTime;

        for (int i = 1; i < settings.steps + 1; i++)
        {
            // Appliquer une étape d'érosion thermique
            ApplyErosionStep(heightMap, bedrockMap, sedimentMap, settings, pixelDistance);

            if (i % 2 == 0)
            {
                // Appeler le callback de progression tous les 2 steps pour éviter de trop ralentir l'algorithme.
                Debug.Log($"Erosion step {i}/{settings.steps}");
                onProgress?.Invoke(i, settings.steps);
                yield return new WaitForSeconds(0.01f);
            }
        }

        float endTime = Time.unscaledTime;
        float elapsedTime = endTime - startTime;
        // Pour des tests, afficher le temps d'exécution.
        // Debug.Log($"Elapsed Time for Thermal Erosion: {elapsedTime:F5}.");

        yield return null;
    }

    public void ErosionProcess(List<List<float>> heightMap, ThermalErosionSettings settings, float pixelDistance, Action<float, float> onProgress=null)
    {
        /*
        Fonction appelée à partir d'un code externe pour automatiquement démarrer la coroutine.
        Pour arrêter ce processus, appeler StopAllCoroutines sur l'instance de cette classe.
         - 'heightMap' : Le heightmap du terrain à modifier.
         - 'settings' : Les paramètres de l'algorithme d'érosion thermique
         - 'pixelDistance' : La distance entre les pixels du heightmap, utilisée pour calculer les angles d'inclinaison.
         - 'onProgress' : Une fonction de callback pour indiquer la progression de l'érosion, utilisée pour afficher
                          en temps réel l'évolution du terrain
        */

        StartCoroutine(ApplyErosion(heightMap, settings, pixelDistance, onProgress));
    }
}

[System.Serializable]
public class ThermalErosionSettings
{
    /*
    Paramètres pour l'érosion thermique.
    */

    public int steps = 50;  // Nombre d'itérations de l'érosion thermique à appliquer.
    public float intensity = 0.5f;  // Intensité de l'érosion, influençant la quantité de sédiments déplacés à chaque étape.
    public float talusProduction = 0.5f;  // Facteur influençant la quantité de sédiments produits par l'effritement de la roche mère,
                                          // utilisé uniquement si sedimentMap est activé.
    public float talusAngle = 0.5f;  // Angle de talus, exprimé en pente (différence de hauteur / distance entre pixels), au-delà duquel l'érosion se produit.
    public float randomness = 0.1f;  // Facteur de randomisation pour ajouter du réalisme en évitant des motifs d'érosion trop réguliers
    public bool sedimentMap = true;  // Activation de l'algorithme 2, qui utilise la bedrock et les sédiments séparément. Plus réaliste mais plus coûteux en performance.
}
