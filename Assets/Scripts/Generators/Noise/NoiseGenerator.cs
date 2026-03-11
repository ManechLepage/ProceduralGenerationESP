using UnityEngine;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Mathematics;

[ExecuteInEditMode]
public class NoiseGenerator : MonoBehaviour
{
    /*
    Première version du générateur de FBM (Fractal Brownian Motion) pour la création de heightmaps procédurales.
    Les paramètres d'octaves, de persistance et de lacunarité permettent de contrôler la génération.
    */
    
    public TextureHelpers textureHelpers;
    
    [Header("Noise Settings")]
    public Vector2 textureSize = new Vector2(256, 256);
    public int octaves = 4;
    public float scale = 20f;
    public float persistence = 0.5f;
    public float lacunarity = 2f;
    public Vector2 offset = Vector2.zero;
    [Space]
    public bool absoluteNoise = false;
    public AnimationCurve heightCurve;
    [Space]
    public bool GenerateTexture = false;

   
    void Update()
    {
        if (GenerateTexture)
        {
            // Générer la texture si elle n'a pas encore été générée.

            GenerateTexture = false;
            List<List<float>> heightMap = GenerateNoise(textureSize, octaves, scale, persistence, lacunarity, offset);
            Texture2D texture = textureHelpers.HeightMapToTexture(heightMap);
            textureHelpers.SaveTexture(texture, "Assets/Textures/Previews/Noise.png");
        }
    }

    public List<List<float>> GenerateDefaultNoise(Vector2 size)
    {
        return GenerateNoise(size, octaves, scale, persistence, lacunarity, offset);
    }
    public List<List<float>> GenerateNoise(Vector2 size, int octaves, float scale, float persistence, float lacunarity, Vector2 offset)
    {
        /*
        Générer un heightmap à partir de valeurs de noise.
        */

        List<List<float>> heightMap = new List<List<float>>();

        for (int x = 0; x < size.x; x++)
        {
            heightMap.Add(new List<float>());
            for (int y = 0; y < size.y; y++)
            {
                float xCoord = (float)(x + offset.x) / size.x;
                float yCoord = (float)(y + offset.y) / size.y;

                float noiseHeight = GetNoiseValue(
                    xCoord, yCoord,
                    octaves, scale, persistence, lacunarity,
                    absoluteNoise, heightCurve
                );

                heightMap[x].Add(noiseHeight);
            }
        }

        return heightMap;
    }

    public float GetNoiseValue(
        float x, float y,
        int octaves=6, float scale=1f, float persistence=0.5f, float lacunarity=2f,
        bool absolute=false, AnimationCurve heightCurve=null
    )
    {
        /*
        On ajoute plusieurs fois du Perlin Noise avec lui-même à différentes échelles (fréquences) et amplitudes pour créer un bruit plus complexe et réaliste.
         - 'x' et 'y' : positon dans l'espace de noise.
         - 'octaves' : nombre de couches de noise à superposer.
         - 'scale' : échelle globale du noise (plus petit = plus lisse).
         - 'persistence' : contrôle comment l'amplitude diminue à chaque octave
         - 'lacunarity' : contrôle comment la fréquence augmente à chaque octave
         - 'absolute' : rendre chaque valeur en valeur absolue pour créer des pics.
         - 'heightCurve' : une courbe d'animation pour ajuster la distribution des hauteurs (optionnel).
         - return : la valeur de noise finale pour les coordonnées données.
        */

        float amplitude = 1;
        float frequency = 1;
        float noiseHeight = 0;

        for (int i = 0; i < octaves; i++)
        {
            float xCoord = x * scale * frequency;
            float yCoord = y * scale * frequency;

            float sample = Mathf.PerlinNoise(xCoord, yCoord) * 2f - 1f;

            if (absolute)
                sample = Mathf.Abs(sample);

            noiseHeight += sample * amplitude;

            amplitude *= persistence;
            frequency *= lacunarity;
        }

        if (!absolute)
            noiseHeight = (noiseHeight + 1f) / 2f; // Normalize to [0,1]

        if (heightCurve != null)
            noiseHeight *= heightCurve.Evaluate(noiseHeight);
        
        return noiseHeight;
    }

    public List<List<float>> GenerateSimpleNoise(Vector2 size, float scale)
    {
        /*
        Générer un noise à uniquement 1 octave et sans autres paramètres que le scale
         - 'size' : taille du heightmap à générer
         - 'scale' : échelle du noise (plus petit = plus lisse)
         - return : le heightmap généré
        */
        
        List<List<float>> heightMap = new List<List<float>>();

        for (int x = 0; x < size.x; x++)
        {
            heightMap.Add(new List<float>());
            for (int y = 0; y < size.y; y++)
            {
                float xCoord = (float)x / size.x * scale;
                float yCoord = (float)y / size.y * scale;

                float sample = Mathf.PerlinNoise(xCoord, yCoord);
                heightMap[x].Add(sample);
            }
        }
    
        return heightMap;
    }
}
