using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;

public class FBMAlgorithm : MonoBehaviour
{
    /*
    Ce programme implémente l'algorithme de Fractal Brownian Motion (FBM) pour générer des terrains procéduraux.
    Il utilise la fonction de Perlin Noise pour créer des variations de hauteur, et combine plusieurs fois
    cette même fonction à lui-même, chaque fois avec une échelle, une amplitude et une fréquence différentes, pour créer des terrains plus complexes et réalistes.

    Ce fichier est séparé en deux méthodes de génération :
     1) Génération d'un heightmap pixel par pixel, ce qui est plus simple à comprendre, quoique plus lent pour les grandes tailles de terrain.
     2) Génération d'un heightmap en utilisant le multithreading avec les Jobs de Unity, ce qui est plus rapide pour les grandes tailles de terrain.
    
    Plus bas dans le fichier se retrouve la classe FBMSettings, qui contient tous les paramètres nécessaires pour contrôler le résultat de l'algorithme de FBM.
    */
    
    private FBMSettings baseSettings;

    void Awake()
    {
        /*
        Initialiser les paramètres de défaut de génération.
        */

        baseSettings = new FBMSettings();

        if (AlgorithmRegistry.Instance != null)
            AlgorithmRegistry.Instance.Register("FBM");
    }

    /* Première partie: génération pixel par pixel */

    public float GetValue(float x, float y, FBMSettings settings = null)
    {
        /*
        Cette fonction retourne une valeur de FBM pour un point donné à l'aide des paramètres spécifiés.
         - 'x' et 'y' : Les coordonnées du point pour lequel on veut calculer la valeur de FBM.
         - 'settings' : Les paramètres de génération à utiliser. Si null, les paramètres par défaut seront utilisés.
         - return : La valeur de FBM pour le point donné, normalisée entre 0 et 1.
        */

        settings = settings ?? baseSettings;

        float amplitude = 1;
        float frequency = 1;
        float noiseHeight = 0;

        // On ajoute plusieurs octaves de Perlin Noise ensemble, avec chacun des échelles et poids différents
        for (int i = 0; i < settings.octaves; i++)
        {
            float xCoord = (x - settings.seed) * settings.scale * frequency;
            float yCoord = (y - settings.seed) * settings.scale * frequency;

            float sample = Mathf.PerlinNoise(xCoord, yCoord) * 2f - 1f;

            if (settings.absolute)
                sample = Mathf.Abs(sample);

            noiseHeight += sample * amplitude;

            amplitude *= settings.persistence;
            frequency *= settings.lacunarity;
        }

        if (!settings.absolute)
            noiseHeight = (noiseHeight + 1f) / 2f;
        
        if (settings.inverted)
            noiseHeight = 1f - noiseHeight;
        
        // Si une courbe d'animation est utilisée, on applique la courbe à la valeur de FBM pour plus de contrôle sur le résultat final
        noiseHeight = settings.curve.Evaluate(noiseHeight);

        return noiseHeight;
    }

    public List<List<float>> GetHeightMap(Vector2 size, FBMSettings settings = null)
    {
        /*
        Cette fonction permet de générer un terrain d'un coup en entier en utilisant GetValue pour chaque point du terrain.
         - 'size' : La taille du terrain à générer, en nombre de pixels (ex: 256x256).
         - 'settings' : Les paramètres de génération à utiliser. Si null, les paramètres par défaut seront utilisés.
        - return : Un heightmap généré selon les paramètres spécifiés.
        */

        settings = settings ?? baseSettings;

        List<List<float>> heightMap = new List<List<float>>();

        for (int x = 0; x < size.x; x++)
        {
            heightMap.Add(new List<float>());
            for (int y = 0; y < size.y; y++)
            {
                float xCoord = (float)(x + settings.offset.x) / size.x;
                float yCoord = (float)(y + settings.offset.y) / size.y;

                // Assigner la valeur de FBM pour ce point du terrain
                float noiseHeight = GetValue(xCoord, yCoord, settings);
                heightMap[heightMap.Count - 1].Add(noiseHeight);
            }
        }

        return heightMap;
    }

    /* Deuxième partie: génération en parallèle */
    public List<List<float>> GetHeightMapThreading(Vector2 size, FBMSettings settings = null)
    {
        /*
        Cette fonction génère un terrain en utilisant le multithreading avec les Jobs de Unity pour calculer les valeurs de FBM
        en parallèle à l'aide du struct CalculateHeightJob.
         - 'size' : La taille du terrain à générer, en nombre de pixels.
         - 'settings' : Les paramètres de génération à utiliser.
         - return : Un heightmap généré selon les paramètres spécifiés.
        */

        settings = settings ?? baseSettings;

        int width = (int)size.x;
        int height = (int)size.y;
        int totalCells = width * height;

        // Initialiser un tableau 1D pour stocker les résultats de chaque point du terrain, qui sera rempli par le job en parallèle
        NativeArray<float> results = new NativeArray<float>(totalCells, Allocator.TempJob);

        // Ajouter la courbe d'animation à un tableau pour que le job puisse y accéder, car les jobs
        // ne peuvent pas accéder directement aux types complexes comme les AnimationCurves
        int resolution = 256;
        NativeArray<float> curveLUT =
            new NativeArray<float>(resolution, Allocator.TempJob);

        for (int i = 0; i < resolution; i++)
        {
            float t = i / (float)(resolution - 1);
            curveLUT[i] = settings.curve.Evaluate(t);
        }

        // Note: les FBMSettings ne peuvent pas être passés directement au job car ils sont un type complexe, donc on passe chaque champ individuellement
        CalculateHeightJob job = new CalculateHeightJob
        {
            width = width,
            height = height,
            seed = settings.seed,
            scale = settings.scale,
            offset = (float2)settings.offset,
            octaves = settings.octaves,
            persistence = settings.persistence,
            lacunarity = settings.lacunarity,
            absolute = settings.absolute,
            inverted = settings.inverted,
            curveLUT = curveLUT,
            results = results
        };

        JobHandle handle = job.Schedule(totalCells, 256);
        handle.Complete();

        // Transformer le tableau 1D de résultats en un heightmap 2D pour l'utiliser dans le reste du programme
        List<List<float>> heightMap = CombineResults(results, size);

        results.Dispose();
        curveLUT.Dispose();

        return heightMap;
    }

    [BurstCompile]
    struct CalculateHeightJob : IJobParallelFor
    {
        /*
        Structure qui permet de générer en parallèle les valeurs de FBM pour chaque point du terrain. Cette structure est utilisée par la méthode GetHeightMapThreading.

        Note : les fonctions définies ici sont des copies presque exactes des fonctions de FBMAlgorithm, car il n'est pas possible d'accéder
        aux fonctions externes dans les Jobs.
        */

        [ReadOnly] public int width;
        [ReadOnly] public int height;
        [ReadOnly] public int seed;
        [ReadOnly] public float scale;
        [ReadOnly] public float2 offset;
        [ReadOnly] public int octaves;
        [ReadOnly] public float persistence;
        [ReadOnly] public float lacunarity;
        [ReadOnly] public bool absolute;
        [ReadOnly] public bool inverted;
        [ReadOnly] public NativeArray<float> curveLUT;

        [WriteOnly] public NativeArray<float> results;

        public void Execute(int index)
        {
            /*
            On veut calculer ici la hauteur du pixel correspondant à l'index donné.
            Ex. Si on a un heightmap de taille 16x16 et un index de de 21, on veut calculer la hauteur du pixel en position (5, 1) du heightmap.
             - 'index' : L'index du point du terrain pour lequel on veut calculer la valeur de FBM. Cet index correspond à une position (x, y) dans le heightmap.
             - return : La valeur de FBM pour le point correspondant à l'index donné, qui sera stockée dans le tableau 'results'.
            */

            int x = index % width;
            int y = index / width;

            float xCoord = (float)(x + offset.x) / width;
            float yCoord = (float)(y + offset.y) / height;

            results[index] = GetValueJob(xCoord, yCoord);
        }

        float GetValueJob(float x, float y)
        {
            /*
            Cette fonction est un équivalent très similaire de GetValue adaptée pour les Jobs.
            On utilise ici les champs donnés au struct au lieu du FBMSettings.
             - 'x' et 'y' : Les coordonnées du point pour lequel on veut calculer la valeur de FBM.
             - return : La valeur de FBM pour le point donné, normalisée entre 0 et 1.
            */

            float amplitude = 1;
            float frequency = 1;
            float noiseHeight = 0;

            // Ajouter plusieurs octaves ensemble
            for (int i = 0; i < octaves; i++)
            {
                float xCoord = (x - seed) * scale * frequency;
                float yCoord = (y - seed) * scale * frequency;

                float sample = Mathf.PerlinNoise(xCoord, yCoord) * 2f - 1f;

                if (absolute)
                    sample = Mathf.Abs(sample);

                noiseHeight += sample * amplitude;

                amplitude *= persistence;
                frequency *= lacunarity;
            }

            if (!absolute)
                noiseHeight = (noiseHeight + 1f) / 2f;
            
            if (inverted)
                noiseHeight = 1f - noiseHeight;
            
            // Appliquer la courbe d'animation à la valeur de FBM en utilisant le tableau curveLUT pour plus de contrôle sur le résultat final
            float t = math.saturate(noiseHeight);
            int idx = (int)(t * (curveLUT.Length - 1));
            float curvedNoiseHeight = curveLUT[idx];

            return curvedNoiseHeight;
        }
    }

    List<List<float>> CombineResults(NativeArray<float> results, Vector2 size)
    {
        /*
        Convertir le tableau 1D des résultats du job en un heightmap 2D pour l'utiliser dans le reste du programme.
         - 'results' : Le tableau 1D contenant les valeurs de FBM calculées par le job pour chaque point du terrain.
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
}


[System.Serializable]
public class FBMSettings
{
    /*
    Stocker les paramètres de génération du Fractal Brownian Motion (FBM).
    */
    
    public int seed = 0;  // Décalage de grande amplitude pour avoir des terrains très différents pour chaque seed.

    [Space]
    public float scale = 1;  // L'échelle de base du Perlin Noise. Un scale plus petit donne un terrain plus zoomé.
    public int octaves = 4;  // Nombre de couches de Perlin Noise superposées - augmente la complexité du terrain.
    public float persistence = 0.5f;  // Dicte à quel point les octaves suivantes contribuent au résultat final.
    public float lacunarity = 2f;  // Dicte à quel point la fréquence augmente pour chaque octave suivante.
    public Vector2 offset = Vector2.zero;  // Décalage des coordonnés - déplacement du terrain en x et y.

    [Space]
    public bool absolute = false;  // Permet la création de 'ridges' en prenant la valeur absolue du Perlin Noise, ce qui donne des terrains plus anguleux.
    public bool inverted = false;  // Invertion de la hauteur du terrain.

    [Space]
    public AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);  // Courbe permettant de modifier la distribution des hauteurs.

    public FBMSettings GetCopy()
    {
        /*
        Comme le nom l'indique, copier cette instance des paramètres.
         - return : Une nouvelle instance de FBMSettings avec les mêmes valeurs que cette instance.
        Note: utiliser seulement lorsque l'on veut une nouvelle instance qui n'est pas une référence pour 
        effectuer des modifications sans affecter les paramètres originaux.
        */

        return new FBMSettings
        {
            seed = this.seed,
            scale = this.scale,
            octaves = this.octaves,
            persistence = this.persistence,
            lacunarity = this.lacunarity,
            offset = this.offset,
            absolute = this.absolute,
            inverted = this.inverted,
            curve = new AnimationCurve(this.curve.keys)
        };
    }

    public bool SameSettings(FBMSettings other)
    {
        /*
        Vérifier si les paramètres de cette instance sont les mêmes que ceux d'une autre instance de FBMSettings.
         - 'other' : L'autre instance de FBMSettings à comparer avec cette instance.
         - return : true si tous les paramètres sont les mêmes, false sinon.
        */

        return 
            this.seed == other.seed &&
            this.scale == other.scale &&
            this.octaves == other.octaves &&
            Mathf.Approximately(this.persistence, other.persistence) &&
            Mathf.Approximately(this.lacunarity, other.lacunarity) &&
            this.offset == other.offset &&
            this.absolute == other.absolute &&
            this.inverted == other.inverted &&
            GameManager.Instance.algorithmHelpers.EqualAnimationCurves(this.curve, other.curve);
    }
}