using UnityEngine;
using System.Collections.Generic;

public class MeshGenerator : MonoBehaviour
{
    /*
    Ce fichier sert à la génération de mesh à partir d'une texture EXR, en prenant la satuation de chaque pixel
    pour évaluer la hauteur des vertex.

    Note : des parties du code sont destinées à tester la génération de mesh, mais elles ne sont pas
    actives à l'instant car enableTest est à false dans l'inspecteur.
    */

    // Paramètres de test
    public bool enableTest = true;
    public int smoothingLevel = 0;
    public float height = 50f;
    public Texture2D testTexture;
    public Vector2 testSize = new Vector2(16f, 16f);

    [Space]
    public GameObject meshPrefab;
    public Material coloredMeshMaterial;

    private GameObject testMeshGO;
    private MeshColorSettings defaultColorSettings = new MeshColorSettings
    {
        isEnabled = false,
    };

    void Start()
    {
        if (enableTest)
        {
            // Création du mesh et de son GameObject pour l'afficher en 3D
            testMeshGO = CreateMeshObject(transform);
            Mesh mesh = TextureToMesh(testTexture, height, testSize, smoothingLevel);
            UpdateMesh(testMeshGO, mesh, testSize);
        }
    }

    public Mesh TextureToMesh(Texture2D texture, float height=1f, Vector2 size=default, int smoothing=0)
    {
        /*
        Transformer une texture EXR en mesh 3D, en utilisant la saturation de chaque pixel pour évaluer la hauteur des vertex.
         - 'texture' : la texture à convertir en mesh. Doit être au format EXR pour avoir les données de saturation.
         - 'height' : multiplicateur de la hauteur des saturations pour le mesh final.
         - 'size' : taille finale du mesh en unités Unity. Par défaut, (1, 1)
         - 'smoothing' : niveau de lissage à appliquer à la texture avant de la convertir.
        */

        List<List<float>> heightMap = GameManager.Instance.textureHelpers.TextureToHeightMap(texture, smoothing);
        return HeightMapToMesh(heightMap, height, size);
    }

    public Mesh HeightMapToMesh(List<List<float>> heightMap, float height=1f, Vector2 size=default, bool borderNormals=false, MeshColorSettings colorSettings = default, bool lowBorders = false, float pixelDistance=1f)
    {
        /*
        Transformer un heightmap en mesh, en assignant à chaque vertex une hauteur correspondant à la valeur dans le heightmap.
         - 'heightMap' : une liste de listes de float représentant les hauteurs des vertex.
         - 'height' : multiplicateur de la hauteur des vertex pour le mesh final.
         - 'size' : taille finale du mesh en unités Unity. Par défaut, (1, 1)
         - 'borderNormals' : si true, les normales des vertex sur les bords du mesh seront calculées en utilisant les lignes et colonnes à l'extrémité.
         - 'colorSettings' : paramètres pour assigner des couleurs aux vertex en fonction de leur hauteur et de leur pente.
         - 'lowBorders' : si true, les vertex sur les bords du mesh auront une hauteur plus basse pour donner une impression d'épaisseur.
        */

        if (size == default)
            size = new Vector2(1f, 1f);
        
        if (colorSettings == default)
            colorSettings = defaultColorSettings;

        // fill the mesh with the data from the texture2d, using the greyscale as height (and multiply by height)
        // the final mesh size should be size.

        Mesh mesh = new Mesh();

        // Augmenter la limite de vertex pour pouvoir avoir des grands terrains (65535 x 65535 théoriquement)
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Color> colors = new List<Color>();
        List<Vector3> normals = new List<Vector3>();
        
        // Adapter les boucles pour include (ou pas) les premières et dernières lignes et colonnes
        int startX = borderNormals ? 1 : 0;
        int startY = borderNormals ? 1 : 0;
        int endX = heightMap[0].Count;  //borderNormals ? heightMap[0].Count : heightMap[0].Count + 1;
        int endY = heightMap.Count;  // borderNormals ? heightMap.Count : heightMap.Count + 1;

        float lowBorderValue = -0.25f;

        // Si l'on veut des bordures basses, on ajoute des lignes et des colonnes sur les contours
        if (lowBorders)
        {
            startX -= 1;
            startY -= 1;
            endX += 1;
            endY += 1;
        }

        int verticesPerRow = endX - startX;
        int rows = endY - startY;

        // Calculer l'espacement entre les vertex selon la taille physique cible
        float sizeXPerPixel = size.x / (verticesPerRow - 1f);
        float sizeYPerPixel = size.y / (rows - 1f);

        for (int y = startY; y < endY; y++)
        {
            for (int x = startX; x < endX; x++)
            {
                // Obtenir la hauteur à un point, selon les limites
                float pixelHeight = SampleHeightMap(heightMap, x, y, height, lowBorders: true, lowBorderValue: lowBorderValue);

                Vector3 vertexPosition = new Vector3(
                    x * sizeXPerPixel, 
                    pixelHeight, 
                    y * sizeYPerPixel
                );

                if (lowBorders)
                {
                    // Modifier la position des bordures basses pour que la position x et y soit juste en dessous des lignes
                    // et colonnes des contours (bordures verticales)

                    if (x == startX)
                        vertexPosition.x = (x + 1) * sizeXPerPixel;
                    else if (x == endX - 1)
                        vertexPosition.x = (x - 2) * sizeXPerPixel;
                    
                    if (y == startY)
                        vertexPosition.z = (y + 1) * sizeYPerPixel;
                    else if (y == endY - 1)
                        vertexPosition.z = (y - 2) * sizeYPerPixel;
                }

                vertices.Add(vertexPosition);

                // Calculer les normales selon les hauteurs à proximité
                float hL = SampleHeightMap(heightMap, x - 1, y, height, lowBorders: lowBorders, lowBorderValue: lowBorderValue);
                float hR = SampleHeightMap(heightMap, x + 1, y, height, lowBorders: lowBorders, lowBorderValue: lowBorderValue);
                float hD = SampleHeightMap(heightMap, x, y - 1, height, lowBorders: lowBorders, lowBorderValue: lowBorderValue);
                float hU = SampleHeightMap(heightMap, x, y + 1, height, lowBorders: lowBorders, lowBorderValue: lowBorderValue);

                Vector3 normal = new Vector3(hL - hR, 2f, hD - hU).normalized;

                normals.Add(normal);

                int localX = x - startX;
                int localY = y - startY;

                if (localX < verticesPerRow - 1 && localY < rows - 1)
                {
                    // Créer les triangles à partir d'index des vertex correspondant.
                    // 2 triangles sont nécessaires par vertex (compléter un carré)

                    int i = localY * verticesPerRow + localX;

                    // First triangle
                    triangles.Add(i);
                    triangles.Add(i + verticesPerRow);
                    triangles.Add(i + verticesPerRow + 1);

                    // Second triangle
                    triangles.Add(i);
                    triangles.Add(i + verticesPerRow + 1);
                    triangles.Add(i + 1);
                }

                if (colorSettings.isEnabled)
                {
                    // Calculer la pente et la hauteur pour assigner la couleur voulue.
                    // Note : les couleurs ne sont affichées que grâce à un shader spécial appliqué sur le matériau du GameObject du mesh

                    float slope = Vector3.Angle(normal, Vector3.up) / 90f / pixelDistance;
                    float colorHeight = pixelHeight / height;

                    Color color;

                    if (!colorSettings.useGradient)
                    {
                        color = Color.white;

                        foreach (ColorConstraint constraint in colorSettings.constraints)
                        {
                            if (slope >= constraint.slopeRange.x && slope <= constraint.slopeRange.y &&
                                colorHeight >= constraint.heightRange.x && colorHeight <= constraint.heightRange.y)
                            {
                                color = constraint.color;
                                break;
                            }
                        }
                    }
                    else
                    {
                        color = colorSettings.slopeGradient.Evaluate(slope * 5f);
                    }

                    colors.Add(color);
                }
            }
        }

        // Assigner les valeurs au mesh

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();

        mesh.normals = normals.ToArray();

        if (colorSettings.isEnabled)
            mesh.colors = colors.ToArray();

        return mesh;
    }

    float SampleHeightMap(List<List<float>> heightMap, int x, int y, float height, bool lowBorders = false, float lowBorderValue = 0f)
    {
        /*
        Obtenir la hauteur d'un point dans le heightmap, selon les limites
         - 'heightMap' : le heightmap à échantillonner
         - 'x' et 'y' : les coordonnées du point à échantillonner
         - 'height' : multiplicateur de la hauteur pour le mesh final
         - 'lowBorders' : si true, les points juste en dehors des limites du heightmap auront une hauteur plus basse pour donner une impression d'épaisseur
         - 'lowBorderValue' : la valeur de hauteur à utiliser pour les points sur la bordure basse
        */

        if (lowBorders && (x == -1 || y == -1 || x == heightMap[0].Count || y == heightMap.Count))
            return lowBorderValue * height;
        
        x = Mathf.Clamp(x, 0, heightMap[0].Count - 1);
        y = Mathf.Clamp(y, 0, heightMap.Count - 1);

        return heightMap[y][x] * height;
    }

    public GameObject CreateMeshObject(Transform parent, bool colored = false)
    {
        /*
        Création du GameObject qui permet de montrer le mesh grâce à l'instantiation d'un prefab
        attaché à ce fichier.Le prefab a un MeshFilter et un MeshRenderer, et un matériau de base.
         - 'parent' : le transform parent auquel le GameObject du mesh sera attaché
         - 'colored' : si true, le matériau du GameObject du mesh sera remplacé par un matériau spécial qui affiche les couleurs assignées aux vertex du mesh
        */

        GameObject meshGO = Instantiate(meshPrefab, parent);
        meshGO.SetActive(true);
        if (colored)
            meshGO.GetComponent<MeshRenderer>().material = coloredMeshMaterial;
        return meshGO;
    }

    public void UpdateMesh(GameObject meshGO, Mesh mesh, Vector2 size)
    {
        /*
        Mise à jour du mesh d'un GameObject existant, et ajustement de sa taille physique selon les paramètres.
         - 'meshGO' : le GameObject du mesh à mettre à jour. Doit avoir un MeshFilter pour que le mesh puisse être assigné.
         - 'mesh' : le nouveau mesh à assigner au GameObject
         - 'size' : la nouvelle taille physique du mesh en unités Unity. Par défaut, (1, 1)
        */

        meshGO.GetComponent<MeshFilter>().mesh = mesh;
        meshGO.transform.localScale = new Vector3(size.x, 1f, size.y);
    }
}


[System.Serializable]
public class MeshColorSettings
{
    /*
    Paramètres de coloration du terrain selon la pente et la hauteur du terrain.
    Ces paramètres consistent en une liste de contraintes qui permettent de chosir la couleur selon les critères.

    Note : si une contrainte en début de liste est remplie, les autres ne seront pas observées.
    */
    
    public bool isEnabled = false;
    public bool useGradient = false;
    public List<ColorConstraint> constraints = new List<ColorConstraint>();

    // Ces valeurs sont utiles pour par exemple conserver une version ultérieure pour tester.
    public List<ColorConstraint> constraintsTemp;
    public Gradient slopeGradient;
}


[System.Serializable]
public class ColorConstraint
{
    /*
    Contrainte de couleur qui s'applique si la pente et la hauteur d'un vertex sont dans les plages spécifiées.
    */
    
    public Color color;  // Couleur à assigner si les critères sont remplis
    public Vector2 slopeRange;  // Plage de pente pour laquelle la contrainte s'applique; valeur entre 0 (plat) et 1 (vertical)
    public Vector2 heightRange;  // Plage de hauteur pour laquelle la contrainte s'applique; valeur entre 0 (hauteur minimale) et 1 (hauteur maximale)
}
