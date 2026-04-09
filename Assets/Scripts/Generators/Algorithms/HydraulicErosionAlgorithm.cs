using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class HydraulicErosionAlgorithm : MonoBehaviour
{
    /*
    Implémentation de l'algorithme d'érosion hydraulique, qui simule l'effet de l'eau sur le terrain pour créer des formes plus naturelles.
    Pour ce faire, des milliers de gouttes d'eau sont simulées, chacune se déplaçant en accumulant des sédiments pour ensuite
    les déposer plus bas.

    Inspiration :
    https://www.youtube.com/watch?v=eaXk97ujbPQ
    Par Sebastian Lague.
    */

    void Start()
    {
        if (AlgorithmRegistry.Instance != null)
            AlgorithmRegistry.Instance.Register("FBM");
    }

    float GetRandomRange(float min, float max)
    {
        System.Random rng = new System.Random();
        return (float)(rng.NextDouble() * (max - min) + min);
    }

    public void ApplyErosionStep(List<List<float>> heightMap, float dropSize, HydraulicErosionSettings settings)
    {
        /*
        Fonction principale de l'algorithme qui simule le déplacement d'une goutte.

        Les étapes de simulations sont les suivantes : 
         1) Initialisation de la position, de la direction, de la vitesse, de l'eau et des sédiments de la goutte.
         2) Calcul du gradient (pente) et de la hauteur du terrain à l'aide d'une interpolation bilinéaire.
         3) Modification de la nouvelle direction résultante, direction également impactée par le vent.
         4) Déplacement la goutte et observer le changement de hauteur
         5) Calcul de la nouvelle capacité de sédiments et dépôt ou érosion selon cette valeur et selon le movement de la goutte
         6) Le dépôt de sédiments se fait grâce à une interpolation bilinéaire et l'érosion selon une fonction de distance.
         7) Modification de la vitesse selon le changement de hauteur et évaporation de l'eau.
        
        Paramètres :
         - 'heightMap' : Le heightmap du terrain à modifier.
         - 'dropSize' : La quantité d'eau de la goutte, qui impacte la capacité de sédiments.
         - 'settings' : Les paramètres de l'algorithme d'érosion hydraulique
        */

        int width = heightMap.Count;
        int height = heightMap[0].Count;

        // Initialisation de la position aléatoire de la goutte et des autres paramètres.
        Vector2 position = new Vector2(GetRandomRange(0, width - 1), GetRandomRange(0, height - 1));
        Vector2 direction = settings.windDirection.normalized * settings.windStrength / 100f;

        float speed = 1f;
        float water = dropSize;
        float sediment = 0f;

        float inertia = 0.05f;
        
        for (int i = 0; i < settings.maxStepsPerDrop; i++)
        {
            // Calcul de décalage entre la position de la goutte et de la grille pour les interpolations bilinéaires.
            Vector2Int gridPosition = new Vector2Int(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y));
            Vector2 dropOffset = position - gridPosition;

            Tuple<Vector2, float> gradientAndNewHeight = GetGradientAndHeight(heightMap, position);
            Vector2 gradient = gradientAndNewHeight.Item1;

            float currentHeight = gradientAndNewHeight.Item2;

            // S'il y a du vent, calculer son impact sur la direction.
            Vector2 windFactor = Vector2.zero;
            if (settings.windEnabled)
            {
                windFactor = settings.windDirection.normalized * settings.windStrength;
            }

            Vector2 externalForces = -gradient + windFactor / 100f;
            direction = direction * inertia + externalForces * (1f - inertia);
            if (direction.magnitude != 0)
                direction.Normalize();

            // Déplacer la goutte
            position += direction;

            if (position.x < 0 || position.x >= width - 1 || position.y < 0 || position.y >= height - 1 || (direction == Vector2.zero))
                break;
            
            // Calculer la nouvelle valeur de hauteur selon une interpolation bilinéaire.
            float newHeight = GetGradientAndHeight(heightMap, position, calculateGradient: false).Item2;
            float heightDelta = newHeight - currentHeight;

            // Calculer la nouvelle capacité de sédiments selon la vitesse, la quantité d'eau et la différence de hauteur.
            float capacity = Mathf.Max(-heightDelta * speed * water * settings.intensity, 0.01f);

            if (sediment > capacity || heightDelta > 0)
            {
                // Dépôt d'une partie des sédiments accumulés, ou de tous les sédiments si la goutte monte.
                float deposition;
                if (heightDelta > 0) deposition = Mathf.Min(sediment, heightDelta);
                else deposition = (sediment - capacity) * 0.3f;
                sediment -= deposition;
                
                // Interpolation bilinéaire du dépôt sur les cellules adjacentes.
                heightMap[gridPosition.x][gridPosition.y] += deposition * (1f - dropOffset.x) * (1f - dropOffset.y);
                heightMap[gridPosition.x + 1][gridPosition.y] += deposition * dropOffset.x * (1f - dropOffset.y);
                heightMap[gridPosition.x][gridPosition.y + 1] += deposition * (1f - dropOffset.x) * dropOffset.y;
                heightMap[gridPosition.x + 1][gridPosition.y + 1] += deposition * dropOffset.x * dropOffset.y;
            }
            else
            {
                // Érosion d'une partie du terrain sur une région autour de la goutte.
                float erosion = Mathf.Min((capacity - sediment) * 0.3f, -heightDelta);
                sediment += erosion;
                ModifyTerrain(width, height, heightMap, gridPosition, -erosion, settings.radius);
            }

            // Modification de la vitesse et évaporation.
            speed = Mathf.Sqrt(speed * speed + heightDelta * 2f);
            water *= 0.99f;
        }
    }

    Tuple<Vector2, float> GetGradientAndHeight(List<List<float>> heightMap, Vector2 position, bool calculateGradient=true)
    {
        /*
        Calcul du gradient et de la hauteur à un point dans le heightmap.
         - 'heightMap' : Le heightmap du terrain.
         - 'position' : La position de la goutte pour laquelle calculer le gradient et la hauteur.
         - 'calculateGradient' : S'il faut calculer le gradient ou non, car le calcul du gradient est coûteux en performance.
        */

        Vector2Int gridPosition = new Vector2Int(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y));
        Vector2 dropOffset = position - gridPosition;

        // Calcul des cellules adjacentes à la position.
        float h00 = heightMap[gridPosition.x][gridPosition.y];
        float h01 = heightMap[gridPosition.x][gridPosition.y + 1];
        float h10 = heightMap[gridPosition.x + 1][gridPosition.y];
        float h11 = heightMap[gridPosition.x + 1][gridPosition.y + 1];

        Vector2 gradient = Vector2.zero;
        if (calculateGradient)
        {
            // Calcul du gradient selon les dérivées partielles en x et en y
            gradient = new Vector2(
                (h10 - h00) * (1f - dropOffset.y) + (h11 - h01) * dropOffset.y,
                (h01 - h00) * (1f - dropOffset.x) + (h11 - h10) * dropOffset.x
            );
        }

        // Interpolation bilinéaire de la hauteur au point.
        float height = h00 * (1f - dropOffset.x) * (1f - dropOffset.y) + h10 * dropOffset.x * (1f - dropOffset.y) + h01 * (1f - dropOffset.x) * dropOffset.y + h11 * dropOffset.x * dropOffset.y;

        return new Tuple<Vector2, float>(gradient, height);
    }

    public void ApplyInstantErosion(List<List<float>> heightMap, HydraulicErosionSettings settings)
    {
        /*
        Fonction pour appliquer l'érosion instantanément sans passer par une coroutine, ce qui peut être utile pour des petites érosions ou pour des tests.
         - 'heightMap' : Le heightmap du terrain à modifier.
         - 'settings' : Les paramètres de l'algorithme d'érosion hydraulique
        */

        for (int i = 0; i < settings.steps; i++)
        {
            float currentDropSize = ProcessDropSize(settings.waterQuantity, i, settings.steps);
            ApplyErosionStep(heightMap, currentDropSize, settings);
        }
    }

    public IEnumerator ApplyErosion(List<List<float>> heightMap, HydraulicErosionSettings settings, Action<float, float> onProgress=null)
    {
        /*
        Boucle principale gérant le nombre de gouttes à faire tomber.
         - 'heightMap' : Le heightmap du terrain à modifier.
         - 'settings' : Les paramètres de l'algorithme d'érosion hydraulique
         - 'onProgress' : Une fonction de callback pour indiquer la progression de l'érosion, utilisée pour afficher
                          en temps réel l'évolution du terrain
        */

        for (int i = 1; i < settings.steps + 1; i++)
        {
            // Faire tomber une goutte d'eau avec une quantité d'eau diminuant au fil des inérations.
            float currentDropSize = ProcessDropSize(settings.waterQuantity, i, settings.steps);
            ApplyErosionStep(heightMap, currentDropSize, settings);

            onProgress?.Invoke(i, settings.steps);

            if (i % 1000 == 0)
            {
                // Appeler le callback.
                Debug.Log($"Erosion step {i}/{settings.steps}");
                onProgress?.Invoke(i, settings.steps);

                // Attendre un peu pour que la scène puisse avoir le temps de loader le nouveau terrain.
                yield return null;
            }
        }

        yield return null;
    }

    public void ErosionProcess(List<List<float>> heightMap, HydraulicErosionSettings settings, Action<float, float> onProgress=null)
    {
        /*
        Fonction appelée à partir d'un code externe pour automatiquement démarrer la coroutine.
        Pour arrêter ce processus, appeler StopAllCoroutines sur l'instance de cette classe.
         - 'heightMap' : Le heightmap du terrain à modifier.
         - 'settings' : Les paramètres de l'algorithme d'érosion hydraulique
         - 'onProgress' : Une fonction de callback pour indiquer la progression de l'érosion, utilisée pour afficher
                          en temps réel l'évolution du terrain
        */

        StartCoroutine(ApplyErosion(heightMap, settings, onProgress));
    }

    public float ProcessDropSize(float dropSize, float current, float total)
    {
        /*
        Fonction pour déterminer la quantité d'eau selon l'intération.
         - 'dropSize' : La quantité d'eau au départ.
         - 'current' : L'itération actuelle de l'érosion.
         - 'total' : Le nombre d'itérations total
        */

        float progress = current / total;
        return dropSize / (progress + 1f);
    }

    private void ModifyTerrain(int width, int height, List<List<float>> heightMap, Vector2Int pos, float amount, float radius)
    {
        /*
        Dépôt - ou retrait - de sédiments autour d'un point sur un rayon (non-entier).
         - 'width' : La largeur du heightmap, pour les limites de la zone de dépôt.
         - 'height' : La hauteur du heightmap, pour les limites de la zone de dépôt.
         - 'heightMap' : Le heightmap du terrain à modifier.
         - 'pos' : La position centrale du dépôt ou de l'érosion.
         - 'amount' : La quantité de sédiments à déposer ou retirer.
         - 'radius' : Le rayon d'influence du dépôt ou de l'érosion.
        */

        int rInt = Mathf.CeilToInt(radius);
        int startX = Mathf.Max(0, pos.x - rInt);
        int startY = Mathf.Max(0, pos.y - rInt);
        int endX   = Mathf.Min(width - 1, pos.x + rInt);
        int endY   = Mathf.Min(height - 1, pos.y + rInt);

        var cells = new List<(int x, int y, float w)>();
        float weightSum = 0f;

        // Faire une boucle sur les pixels environnants (en excluants ceux qui sont trop loin)
        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                float dx = x - pos.x;
                float dy = y - pos.y;
                float sqr = dx*dx + dy*dy;
                if (sqr < radius * radius)
                {
                    // Ajouter le dépôt pondéré selon la distance au débôt total.
                    float dist = Mathf.Sqrt(sqr);
                    float w = 1f - (dist / radius);
                    if (w > 0f) { cells.Add((x,y,w)); weightSum += w; }
                }
            }
        }

        if (weightSum <= 0f) return;

        // S'assurer que le dépôt total sera de 1.
        float invSum = 1f / weightSum;

        foreach (var c in cells)
        {
            // Déposer la quantité pondérée pour le pixel, selon son poids.
            float influence = c.w * invSum;
            heightMap[c.x][c.y] += amount * influence;
        }
    }
}


[System.Serializable]
public class HydraulicErosionSettings
{
    /*
    Paramètres de génération de l'érosion hydraulique.
    */
    public int steps = 1000;  // Nombre de gouttes à simuler
    public float waterQuantity = 1f;  // Quantité d'eau dans les gouttes
    public float intensity = 1f;  // Intensité de l'érosion et du dépôt
    public float radius = 2f;  // Rayon d'influence de l'érosion des gouttes
    public int maxStepsPerDrop = 100;  // Nombre d'étapes de déplacement des gouttes

    [Header("Wind")]
    public bool windEnabled = false;
    public Vector2 windDirection = new Vector2(1f, 0f);  // Direction du vent
    public float windStrength = 1f;  // Intensité du vent
}
