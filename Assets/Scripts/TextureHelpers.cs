using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

public class TextureHelpers : MonoBehaviour
{
    /*
    Gérer la conversion de textures EXR en heightmaps (listes de listes de floats) et inversement, ainsi que des opérations
    entre heightmaps (addition, multiplication). Permet aussi de sauvegarder une texture en EXR dans le projet.
    */

    public List<List<float>> TextureToHeightMap(Texture2D texture, int smoothing=0)
    {
        /*
        Prendre une texture EXR et lire les pixels du canal rouge pour construire une heightmap (liste de listes de floats)
        selon l'intensité de rouge.
         - 'texture': texture à convertir en heightmap
         - 'smoothing': degré de lissage de la heightmap en échantillonnant les pixels environnants (moyenne) pour chaque pixel.
         - return: heightmap construite à partir de la texture
        */

        List<List<float>> heightMap = new List<List<float>>();

        for (int y = 0; y < texture.height; y++)
        {
            List<float> row = new List<float>();
            for (int x = 0; x < texture.width; x++)
            {
                float pixelHeight;
                if (smoothing > 0)
                {
                    pixelHeight = SampleSmoothed(texture, x, y, smoothing);
                }
                else
                {
                    pixelHeight = texture.GetPixel(x, y).r;
                }

                row.Add(pixelHeight);
            }
            heightMap.Add(row);
        }

        return heightMap;
    }

    public Texture2D HeightMapToTexture(List<List<float>> heightMap)
    {
        /*
        Convertir une heightmap (liste de listes de floats) en une texture EXR en utilisant les valeurs de la heightmap pour le canal rouge.
         - 'heightMap': heightmap à convertir en texture
         - return: texture construite à partir de la heightmap
        
        Note: la heightmap doit être de dimensions compatibles avec une texture (toutes les sous-listes doivent avoir la même longueur).
        */

        Texture2D texture = new Texture2D((int)heightMap.Count, (int)heightMap[0].Count, TextureFormat.RFloat, false, true);

        for (int x=0; x<texture.width; x++)
        {
            for (int y=0; y<texture.height; y++)
            {
                texture.SetPixel(x, y, new Color(heightMap[x][y], 0, 0, 1));
            }
        }
        texture.Apply();

        return texture;
    }

    public List<List<float>> AddHeightMaps(List<List<float>> mapA, List<List<float>> mapB, float ratioB)
    {
        /*
        Ajouter deux heightmap ensemble (addition des valeurs pondérées) selon un ratio pour la deuxième heightmap.
         - 'mapA': première heightmap à ajouter
         - 'mapB': deuxième heightmap à ajouter
         - 'ratioB': poids de la deuxième heightmap dans l'addition (entre 0 et 1)
         - return: heightmap résultante de l'addition des deux heightmaps selon le ratio (ratio total de 1)
        */

        List<List<float>> resultMap = new List<List<float>>();

        for (int x = 0; x < mapA.Count; x++)
        {
            resultMap.Add(new List<float>());
            for (int y = 0; y < mapA[0].Count; y++)
            {
                float combinedHeight = mapA[x][y] * (1 - ratioB) + mapB[x][y] * ratioB;
                resultMap[x].Add(combinedHeight);
            }
        }

        return resultMap;
    }

    public List<List<float>> MultiplyHeightMaps(List<List<float>> mapA, List<List<float>> mapB)
    {
        /*
        Multiplier deux heightmaps ensemble (multiplication des valeurs) pour combiner leurs effets.
         - 'mapA': première heightmap à multiplier
         - 'mapB': deuxième heightmap à multiplier
         - return: heightmap résultante de la multiplication des deux heightmaps
        */

        List<List<float>> resultMap = new List<List<float>>();

        for (int x = 0; x < mapA.Count; x++)
        {
            resultMap.Add(new List<float>());
            for (int y = 0; y < mapA[0].Count; y++)
            {
                float combinedHeight = mapA[x][y] * mapB[x][y];
                resultMap[x].Add(combinedHeight);
            }
        }

        return resultMap;
    }

    public void SaveTexture(Texture2D texture, string path, bool refreshAssetDatabase = true, bool makeReadable = false)
    {
        /*
        Sauvegarder une texture en EXR dans le projet Unity à un chemin donné.
         - 'texture': texture à sauvegarder
         - 'path': chemin relatif dans le projet où sauvegarder la texture (ex: "Assets/Textures/heightmap.exr")
         - 'refreshAssetDatabase': si true, rafraîchit la base de données des assets après la sauvegarde pour que la texture soit immédiatement visible dans l'éditeur
         - 'makeReadable': si true, rend la texture lisible après la sauvegarde pour pouvoir accéder à ses pixels depuis le code
         - return: void
        */

        System.IO.File.WriteAllBytes(path, texture.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat));
        if (refreshAssetDatabase)
            UnityEditor.AssetDatabase.Refresh();
        
        if (makeReadable)
        {
            TextureImporter importer = (TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
            importer.isReadable = true;
            importer.SaveAndReimport();
        }
    }

    float SampleSmoothed(Texture2D tex, int x, int y, int radius)
    {
        /*
        Échantillonner une texture avec un lissage appliqué pour un certain pixel, avec un rayon défini.
         - 'tex': texture à échantillonner
         - 'x': coordonnée x du pixel à échantillonner
         - 'y': coordonnée y du pixel à échantillonner
         - 'radius': rayon de lissage (nombre de pixels à considérer autour du pixel cible pour calculer la moyenne)
         - return: valeur moyenne du canal rouge des pixels dans le rayon autour du pixel cible
        */

        float sum = 0f;
        int count = 0;

        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                int sx = Mathf.Clamp(x + dx, 0, tex.width - 1);
                int sy = Mathf.Clamp(y + dy, 0, tex.height - 1);

                sum += tex.GetPixel(sx, sy).r;
                count++;
            }
        }

        return sum / count;
    }

    public List<List<float>> SmoothHeightMap(List<List<float>> heightMap, int radius)
    {
        /*
        Appliquer un lissage à une heightmap en échantillonnant les valeurs environnantes pour chaque point de la heightmap.
         - 'heightMap': heightmap à lisser
         - 'radius': rayon de lissage (nombre de points à considérer autour de chaque point pour calculer la moyenne)
         - return: heightmap lissée
        */

        List<List<float>> smoothedMap = new List<List<float>>();

        for (int x = 0; x < heightMap.Count; x++)
        {
            smoothedMap.Add(new List<float>());
            for (int y = 0; y < heightMap[0].Count; y++)
            {
                float sum = 0f;
                int count = 0;

                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        int sx = Mathf.Clamp(x + dx, 0, heightMap.Count - 1);
                        int sy = Mathf.Clamp(y + dy, 0, heightMap[0].Count - 1);

                        sum += heightMap[sx][sy];
                        count++;
                    }
                }

                smoothedMap[x].Add(sum / count);
            }
        }

        return smoothedMap;
    }
}
