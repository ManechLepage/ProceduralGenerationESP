using UnityEngine;
using System.Collections.Generic;
using System;
using System.Threading;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;

// Types d'algorithmes de distance disponibles pour le voronoi.
public enum DistanceType
{
    Euclidean,
    Manhattan
}

public class VoronoiAlgorithm : MonoBehaviour
{
    /*
    Génération d'un terrain en utilisant l'algorithme de Voronoi, qui divise l'espace en régions basées sur la distance
    à un ensemble de points générés aléatoirement (les "corners").
    Ensuite, la hauteur correspond à la distance entre un point donné et le coin le plus proche, ce qui crée des formes de terrain uniques et intéressantes.

    Ce fichier est séparé en deux méthodes de génération :
     1) Génération d'un heightmap pixel par pixel, ce qui est plus simple à comprendre, quoique plus lent pour les grandes tailles de terrain.
     2) Génération d'un heightmap en utilisant le multithreading avec les Jobs de Unity, ce qui est plus rapide pour les grandes tailles de terrain.
    
    Plus bas dans le fichier se retrouve la classe VoronoiSettings, qui contient tous les paramètres nécessaires pour contrôler le résultat de l'algorithme de Voronoi.
    */

    private VoronoiSettings baseSettings;

    void Awake()
    {
        /*
        Initialiser les paramètres de génération par défaut.
        */

        baseSettings = new VoronoiSettings();
        AlgorithmRegistry.Instance.Register("Voronoi");
    }
    
    /* Première partie : génération pixel par pixel */

    public float GetValue(float x, float y, VoronoiSettings settings = null)
    {
        /*
        Obtenir la valeur de hauteur pour un point donné en utilisant l'algorithme de Voronoi à partir d'un point et des paramètres.
        On veut d'abord définir les coordonnées des coins de la grille qui entourent le point. Ces coins correspondent
        au paramètre settings.neighborhoodSize, qui permet d'avoir plus de coins à considérer pour le calcul de la hauteur, ce qui limite
        les biais d'aléatoire lors du déplacement des coins.
         - 'x' et 'y' : Les coordonnées du point pour lequel on veut calcul
         - 'settings' : Les paramètres de génération à utiliser pour calculer la hauteur. Si null, les paramètres par défaut seront utilisés.
         - return : La valeur de hauteur calculée pour le point (x, y) en utilisant l'algorithme de Voronoi.
        */

        settings = settings ?? baseSettings;

        float scaledX = x * settings.scale;
        float scaledY = y * settings.scale;

        Vector2 scaledPoint = new Vector2(scaledX, scaledY);

        int gridX = Mathf.FloorToInt(scaledX);
        int gridY = Mathf.FloorToInt(scaledY);

        bool evenX = settings.neighborhoodSize.x % 2 == 0;
        bool evenY = settings.neighborhoodSize.y % 2 == 0;
        int halfSizeX = evenX ? settings.neighborhoodSize.x / 2 : (settings.neighborhoodSize.x + 1) / 2;
        int halfSizeY = evenY ? settings.neighborhoodSize.y / 2 : (settings.neighborhoodSize.y + 1) / 2;

        float closestDistance = float.MaxValue;

        // Faire une boucle de tous les coins environnants du point pour trouver lequel est le plus près.
        for (int i = -(evenX ? halfSizeX : halfSizeX - 1); i <= halfSizeX; i++)
        {
            for (int j = -(evenY ? halfSizeY : halfSizeY - 1); j <= halfSizeY; j++)
            {
                // Déplacer un peu le coin pour avoir des formes plus naturelles au lieu d'une grille.
                Vector2 corner = GetModifiedCorner(new Vector2Int(gridX + i, gridY + j), settings.variation, settings.seed);

                float distance = 0f;

                // Calcul de la distance entre le point et le coin en utilisant la méthode choisie dans les paramètres.
                switch (settings.distanceType)
                {
                    case DistanceType.Euclidean:
                        distance = GetFastEucleideanDistance(scaledPoint, corner);
                        break;
                    case DistanceType.Manhattan:
                        distance = GetManhattanDistance(scaledPoint, corner);
                        break;
                }

                if (distance < closestDistance)
                    closestDistance = distance;
            }
        }

        // Normaliser la distance pour qu'elle soit entre 0 et 1.
        float maxDistance = Mathf.Pow(Mathf.Sqrt(2) + settings.variation, 2);
        closestDistance = closestDistance / maxDistance;

        // Retourner la valeur inversée selon le paramètre 'inverted'.
        return settings.inverted ? 1 - closestDistance : closestDistance;
    }

    public List<List<float>> GetHeightMap(Vector2 size, VoronoiSettings settings = null, List<List<Vector2>> domainMap = null)
    {
        /*
        Évaluer la fonction GetValue pour chaque point d'un heightmap d'une certaine taille.
         - 'size' : taille du terrain en pixels.
         - 'settings' : Les paramètres de génération à utiliser pour calculer la hauteur.
         - return : Un heightmap 2D généré en utilisant l'algorithme de Voronoi avec les paramètres donnés.
        */

        settings = settings ?? baseSettings;

        List<List<float>> heightMap = new List<List<float>>();

        for (int x = 0; x < size.x; x++)
        {
            heightMap.Add(new List<float>());
            for (int y = 0; y < size.y; y++)
            {
                float domainX = x;
                float domainY = y;

                if (domainMap != null)
                {
                    domainX = domainMap[x][y].x;
                    domainY = domainMap[x][y].y;
                }

                float xCoord = (float)(domainX + settings.offset.x) / size.x;
                float yCoord = (float)(domainY + settings.offset.y) / size.y;

                // Évaluer la valeur pour chaque point selon les paramètres.
                float value = GetValue(xCoord, yCoord, settings);
                heightMap[heightMap.Count - 1].Add(value);
            }
        }
        
        return heightMap;
    }

    /* Deuxième partie: génération en parallèle */

    public List<List<float>> GetHeightMapThreading(Vector2 size, VoronoiSettings settings = null, List<List<Vector2>> domainMap = null)
    {
        /*
        Remplir un heightmap avec des valeurs de voronoi en utilisant les Jobs de Unity pour faire le calcul en parallèle,
        ce qui est plus rapide pour les grandes tailles de terrain.
         - 'size' : taille du terrain en pixels.
         - 'settings' : Les paramètres de génération à utiliser pour calculer la hauteur.
         - return : Un heightmap 2D généré en utilisant l'algorithme de Voronoi avec les paramètres donnés.
        */

        settings = settings ?? baseSettings;

        int width = (int)size.x;
        int height = (int)size.y;
        int totalCells = width * height;

        // Tableau 1D pour stocker les résultats des jobs.
        NativeArray<float> results = new NativeArray<float>(totalCells, Allocator.TempJob);

        NativeArray<Vector2> domainMapArray = new NativeArray<Vector2>(width * height, Allocator.TempJob);
        if (domainMap != null)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (domainMap != null && x < domainMap.Count && y < domainMap[x].Count)
                    {
                        domainMapArray[y * width + x] = domainMap[x][y];
                    }
                    else
                    {
                        domainMapArray[y * width + x] = new Vector2(x, y);
                    }
                }
            }
        }

        // Passer les paramètres de génération un par un car le type VoronoiSettings ne peut pas être utilisé directement dans un job.
        CalculateHeightJob job = new CalculateHeightJob
        {
            width = width,
            height = height,
            seed = settings.seed,
            scale = settings.scale,
            offset = (float2)settings.offset,
            variation = settings.variation,
            distanceType = settings.distanceType,
            neighborhoodSize = new int2(settings.neighborhoodSize.x, settings.neighborhoodSize.y),
            inverted = settings.inverted,
            maxDistance = Mathf.Sqrt(Mathf.Sqrt(2) + settings.variation),
            domainMap = domainMapArray,
            results = results
        };

        JobHandle handle = job.Schedule(totalCells, 256);
        handle.Complete();

        // Reconstruire le heightmap 2D à partir du tableau 1D de résultats.
        List<List<float>> heightMap = CombineResults(results, size);

        results.Dispose();
        domainMapArray.Dispose();

        return heightMap;
    }

    static float HashToFloat(uint x)
    {
        /*
        Fonction très rapide pour hash un entier en float.
        Utilisé pour générer des offsets aléatoires pour les coins de la grille dans l'algorithme de Voronoi, ce qui donne des formes de terrain plus naturelles.
         - 'x' : L'entier à hasher.
         - return : Un float entre 0 et 1 généré à partir du hash de l'entier.
        */

        x ^= x >> 16;
        x *= 0x7feb352d;
        x ^= x >> 15;
        x *= 0x846ca68b;
        x ^= x >> 16;
        return (x & 0x00FFFFFF) / 16777216f;
    }

    [BurstCompile]
    struct CalculateHeightJob : IJobParallelFor
    {
        /*
        Structure permettant de générer des valeurs de voronoi en parallèle rapidmement.
        Des types de Unity.Mathematics sont utilisés pour accélérer les calculs pour une meilleure performance (ex. float2, int2)

        Note : les fonctions de la classe principale VoronoiAlgorithm ne sont pas accessibles ici à cause des Jobs,
        et c'est pour cela que les fonctions présentes sont des versions presque identiques.
        */

        [ReadOnly] public int width;
        [ReadOnly] public int height;
        [ReadOnly] public int seed;
        [ReadOnly] public float scale;
        [ReadOnly] public float2 offset;
        [ReadOnly] public float variation;
        [ReadOnly] public DistanceType distanceType;
        [ReadOnly] public int2 neighborhoodSize;
        [ReadOnly] public bool inverted;
        [ReadOnly] public float maxDistance;
        [ReadOnly] public NativeArray<Vector2> domainMap;

        [WriteOnly] public NativeArray<float> results;

        public void Execute(int index)
        {
            /*
            On veut calculer ici la hauteur du pixel correspondant à l'index donné.
            Ex. Si on a un heightmap de taille 16x16 et un index de de 21, on veut calculer la hauteur du pixel en position (5, 1) du heightmap.
             - 'index' : L'index du point du terrain pour lequel on veut calculer la valeur de Voronoi. Cet index correspond à une position (x, y) dans le heightmap.
             - return : La valeur de Voronoi pour le point correspondant à l'index donné, qui sera stockée dans le tableau 'results'.
            */
            
            int x = index % width;
            int y = index / width;

            float domainX = x;
            float domainY = y;

            if (domainMap.Length > 0)
            {
                domainX = domainMap[index].x;
                domainY = domainMap[index].y;
            }

            float xCoord = (float)(domainX + offset.x) / width;
            float yCoord = (float)(domainY + offset.y) / height;

            // Calculer la valeur pour le point donné.
            results[index] = GetValueJob(xCoord, yCoord);
        }

        float GetValueJob(float x, float y)
        {
            /*
            Équivalent de la fonction GetValue, mais cette fois-ci pour les Jobs.
             - 'x' et 'y' : Les coordonnées du point pour lequel on veut calculer la valeur de Voronoi.
             - return : La valeur de Voronoi calculée pour le point (x, y)
            */

            float scaledX = x * scale;
            float scaledY = y * scale;

            float2 scaledPoint = new float2(scaledX, scaledY);

            int gridX = Mathf.FloorToInt(scaledX);
            int gridY = Mathf.FloorToInt(scaledY);

            bool evenX = neighborhoodSize.x % 2 == 0;
            bool evenY = neighborhoodSize.y % 2 == 0;
            int halfSizeX = evenX ? neighborhoodSize.x / 2 : (neighborhoodSize.x + 1) / 2;
            int halfSizeY = evenY ? neighborhoodSize.y / 2 : (neighborhoodSize.y + 1) / 2;

            float closestDistance = float.MaxValue;

            // Faire une boucle de tous les coins environnants du point pour trouver lequel est le plus près.
            for (int i = -(evenX ? halfSizeX : halfSizeX - 1); i <= halfSizeX; i++)
            {
                for (int j = -(evenY ? halfSizeY : halfSizeY - 1); j <= halfSizeY; j++)
                {
                    // Modifier la position du coin pour avoir des formes plus naturelles au lieu d'une grille.
                    float2 corner = GetModifiedCornerJob(new float2(gridX + i, gridY + j));
                    float distance = 0f;

                    // Calcul de la distance entre le point et le coin en utilisant la méthode choisie dans les paramètres.
                    switch (distanceType)
                    {
                        case DistanceType.Euclidean:
                            distance = GetFastEucleideanDistanceJob(scaledPoint, corner);
                            break;
                        case DistanceType.Manhattan:
                            distance = GetManhattanDistanceJob(scaledPoint, corner);
                            break;
                    }

                    if (distance < closestDistance)
                        closestDistance = distance;
                }
            }

            // Normaliser la distance pour qu'elle soit entre 0 et 1.
            closestDistance = closestDistance / maxDistance;

            // Inverser la valeur selon le paramètre 'inverted' et la retourner.
            return inverted ? 1 - closestDistance : closestDistance;
        }

        float2 GetModifiedCornerJob(float2 corner)
        {
            /*
            Version plus efficace de GetModifiedCorner pour les Jobs.
             - 'corner' : Les coordonnées du coin de la grille avant modification.
             - return : Les coordonnées du coin de la grille après modification avec un offset aléatoire pour donner des formes de terrain plus naturelles.
            */

            // Calculer une seed associée au coin.
            uint s = math.hash(new int2((int)corner.x, (int)corner.y)) + (uint)seed;
            if (s == 0) s = 1u;

            // Générer des offsets aléatoires pour le coin en utilisant la seed calculée.
            float offsetX = (HashToFloat(s) - 0.5f) * variation;
            float offsetY = (HashToFloat(s ^ 0x9E3779B9u) - 0.5f) * variation;
            return new float2(corner.x + offsetX, corner.y + offsetY);
        }

        float GetFastEucleideanDistanceJob(float2 a, float2 b)
        {
            /*
            Distance euclédienne au carré pour plus d'efficacité.
             - 'a' et 'b' : Les coordonnées des deux points entre lesquels on veut calculer la distance.
             - return : La distance euclédienne au carré entre les points a et b.
            */

            float dx = a.x - b.x;
            float dy = a.y - b.y;
            return dx * dx + dy * dy;
        }

        float GetManhattanDistanceJob(float2 a, float2 b)
        {
            /*
            Distance de Manhattan pour les Jobs.
             - 'a' et 'b' : Les coordonnées des deux points entre lesquels on veut calculer la distance.
             - return : La distance de Manhattan entre les points a et b.
            */

            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        static float HashToFloat(uint x)
        {
            /*
            Fonction très rapide pour hash un entier en float.
            Utilisé pour générer des offsets aléatoires pour les coins de la grille dans l'algorithme de Voronoi, ce qui donne des formes de terrain plus naturelles.
            - 'x' : L'entier à hasher.
            - return : Un float entre 0 et 1 généré à partir du hash de l'entier.
            */

            x ^= x >> 16;
            x *= 0x7feb352d;
            x ^= x >> 15;
            x *= 0x846ca68b;
            x ^= x >> 16;
            return (x & 0x00FFFFFF) / 16777216f;
        }
    }

    List<List<float>> CombineResults(NativeArray<float> results, Vector2 size)
    {
        /*
        Recombiner le tableau 1D en heightmap 2D.
         - 'results' : Le tableau 1D contenant les valeurs de Voronoi calculées par le job pour chaque point du terrain.
         - 'size' : La taille du terrain, en nombre de pixels, qui permet de savoir comment convertir l'index 1D en coordonnées (x, y).
         - return : Un heightmap 2D généré à partir des résultats du job.
        */

        List<List<float>> heightMap = new List<List<float>>();

        for (int x = 0; x < size.x; x++)
        {
            heightMap.Add(new List<float>());
            for (int y = 0; y < size.y; y++)
            {
                int index = y * (int)size.x + x;
                heightMap[x].Add(results[index]);
            }
        }

        return heightMap;
    }

    public float GetFastEucleideanDistance(Vector2 a, Vector2 b)
    {
        /*
        Distance euclédienne au carré pour plus d'efficacité.
         - 'a' et 'b' : Les coordonnées des deux points entre lesquels on veut calculer la distance.
         - return : La distance euclédienne au carré entre les points a et b.
        */

        float dx = a.x - b.x;
        float dy = a.y - b.y;
        return dx * dx + dy * dy;
    }

    public float GetManhattanDistance(Vector2 a, Vector2 b)
    {
        /*
        Distance manhattan pour une variation angulaire dans le terrain
         - 'a' et 'b': Coordonnées de deux point pour calculer la distance
         - return : distance manhattan entre les deux points.
        */

        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    public Vector2 GetModifiedCorner(Vector2Int corner, float variation, int seed)
    {
        // Unity Mathematics Random is faster outside of jobs.
        uint s = math.hash(new int2(corner.x, corner.y)) + (uint)seed;
        if (s == 0) s = 1u;

        Unity.Mathematics.Random random = new Unity.Mathematics.Random(s);
        float offsetX = (random.NextFloat() - 0.5f) * variation;
        float offsetY = (random.NextFloat() - 0.5f) * variation;
        return new Vector2(corner.x + offsetX, corner.y + offsetY);
    }
}


[System.Serializable]
public class VoronoiSettings
{
    /*
    Paramètres de génération du Voronoi.
    */

    public int seed = 0;  // Décalage de grande échelle pour avoir des terrains très différents selon la seed.

    [Space]
    public float scale = 1;  // Échelle (zoom) du terrain. Un plus petit scale donne un terrain plus zoomé.
    public Vector2 offset = Vector2.zero;  // Décalage du terrain - déplacement en x et y.
    public float variation = 0.75f;  // Intensité de la variation de position des points. Si cette valeur est très grande,
                                     // il faut alors augmenter neighborhoodSize pour éviter les imprécisions de calcul
    public DistanceType distanceType = DistanceType.Euclidean;  // Type de distance pour le calcul.
    public Vector2Int neighborhoodSize = new Vector2Int(3, 3);  // Grandeur de la région de coins observée autour de chaque point - impact élevé sur la performance.

    [Space]
    public bool inverted = false;  // Inversion des valeurs de terrain.

    public VoronoiSettings GetCopy()
    {
        /*
        Comme le nom l'indique, copier cette instance des paramètres.
         - return : Une nouvelle instance de VoronoiSettings avec les mêmes valeurs que cette instance.
        Note: utiliser seulement lorsque l'on veut une nouvelle instance qui n'est pas une référence pour 
        effectuer des modifications sans affecter les paramètres originaux.
        */

        return new VoronoiSettings
        {
            seed = this.seed,
            scale = this.scale,
            offset = this.offset,
            variation = this.variation,
            distanceType = this.distanceType,
            neighborhoodSize = this.neighborhoodSize,
            inverted = this.inverted
        };
    }

    public bool SameSettings(VoronoiSettings other)
    {
        /*
        Vérifier si les paramètres de cette instance sont les mêmes que ceux d'une autre instance de VoronoiSettings.
         - 'other' : L'autre instance de VoronoiSettings à comparer avec cette instance.
         - return : true si tous les paramètres sont les mêmes, false sinon.
        */

        return 
            this.seed == other.seed &&
            this.scale == other.scale &&
            this.offset == other.offset &&
            this.variation == other.variation &&
            this.distanceType == other.distanceType &&
            this.neighborhoodSize == other.neighborhoodSize &&
            this.inverted == other.inverted;
    }
}
