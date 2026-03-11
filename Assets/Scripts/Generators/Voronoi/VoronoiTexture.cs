using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class VoronoiTexture : MonoBehaviour
{
    /*
    Première version de génération de Voronoi dans un espace donné.
    La fonction de voronoi présentée est rapide et ne peut cependant s'étendre à l'infini.
    */

    public TextureHelpers textureHelpers;
    [Header("Voronoi Settings")]
    public int numberOfPoints = 10;
    public Vector2 textureSize = new Vector2(256, 256);
    [Space]
    public bool GenerateTexture = false;

    void Update()
    {
        if (GenerateTexture)
        {
            // Générer la texture de Voronoi et la sauvegarder dans les assets si ça n'a pas été fait auparavant
            GenerateTexture = false;
            List<List<float>> heightMap = LoadVoronoiTexture((int)textureSize.x, (int)textureSize.y);
            Texture2D texture = textureHelpers.HeightMapToTexture(heightMap);
            textureHelpers.SaveTexture(texture, "Assets/Textures/Previews/Voronoi.png");
        }
    }

    public List<List<float>> LoadVoronoiTexture(int textureWidth = 512, int textureHeight = 512)
    {
        /*
        Générer une texture de Voronoi et la retourner sous forme de heightmap normalisé par rapport à la valeur la plus haute.
        */

        List<List<float>> heightMap = GenerateVoronoiHeightMap(textureWidth, textureHeight, numberOfPoints);
        List<List<float>> normalizedHeightMap = NormalizeHeightMap(heightMap);
        
        return normalizedHeightMap;
    }

    List<List<float>> GenerateVoronoiHeightMap(int width, int height, int numPoints)
    {
        /*
        Générer un heightmap de voronoi en suivant ces étapes :
         1 - Générer des points aléatoires dans l'espace de la texture
         2 - Calculer, pour chaque pixel, la distance au point le plus proche
        
        Paramètres :
         - 'width' : largeur de la texture
         - 'height' : hauteur de la texture
         - 'numPoints' : nombre de points à générer pour le diagramme de Voronoi
         - return : heightmap de Voronoi non normalisé
        */
        
        List<List<float>> heightMap = new List<List<float>>();
        Vector2[] points = new Vector2[numPoints];


        for (int i = 0; i < numPoints; i++)
        {
            points[i] = new Vector2(Random.Range(0, width), Random.Range(0, height));
        }

        for (int y = 0; y < height; y++)
        {
            heightMap.Add(new List<float>());
            for (int x = 0; x < width; x++)
            {
                Vector2 pixel = new Vector2(x, y);
                float minDist = GetMinDistance(pixel, points);
                float intensity = Mathf.InverseLerp(0, Mathf.Sqrt(width * width + height * height), minDist);
                heightMap[y].Add(intensity);
            }
        }

        return heightMap;
    }

    float Distance(Vector2 a, Vector2 b)
    {
        /*
        Fonction de distance euclédienne entre deux points
        */
        
        return Vector2.Distance(a, b);
    }

    float GetMinDistance(Vector2 pixel, Vector2[] points)
    {
        /*
        Calculer la distance entre un pixel et tous les points, et retourner la distance minimale.
         - 'pixel' : position du pixel pour lequel on veut calculer la distance
         - 'points' : tableau de points de Voronoi
         - return : distance minimale entre le pixel et les points de Voronoi
        */
        
        float minDist = float.MaxValue;
        foreach (var point in points)
        {
            float dist = Distance(pixel, point);
            if (dist < minDist)
            {
                minDist = dist;
            }
        }
        return minDist;
    }

    List<List<float>> NormalizeHeightMap(List<List<float>> heightMap)
    {
        /*
        Normaliser la heightmap selon la valeur la plus haute et la plus basse pour que les valeurs soient comprises entre 0 et 1.
         - 'heightMap' : heightmap à normaliser
         - return : heightmap normalisée
        */
        
        float minVal = float.MaxValue;
        float maxVal = float.MinValue;

        for (int y = 0; y < heightMap.Count; y++)
        {
            for (int x = 0; x < heightMap[0].Count; x++)
            {
                if (heightMap[y][x] < minVal)
                    minVal = heightMap[y][x];
                if (heightMap[y][x] > maxVal)
                    maxVal = heightMap[y][x];
            }
        }

        List<List<float>> normalizedMap = new List<List<float>>();

        for (int y = 0; y < heightMap.Count; y++)
        {
            normalizedMap.Add(new List<float>());
            for (int x = 0; x < heightMap[0].Count; x++)
            {
                float normalizedValue = (heightMap[y][x] - minVal) / (maxVal - minVal);
                normalizedMap[y].Add(normalizedValue);
            }
        }

        return normalizedMap;
    }

    Texture AddTextures(Texture texture1, Texture texture2, Vector2 position)
    {
        /*
        Fonction en développement pour ajouter des textures ensemble. 
        */
        
        return texture1;
    }
}
