using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// Types d'outils utilisables.
public enum ToolType
{
    Paint,
    Smooth,
    Flatten
}

public class PaintManager : MonoBehaviour
{
    /*
    Ce fichier gère la logique de la peinture sur une texture dans la scène, en 2D. Il élabore un système permettant de peindre en utilisant
    divers outils (peinture, lissage, aplanissement) et différentes brosses. Il gère également les sauvegardes, les annulations, et l'affichage d'une superposition de courbes de hauteur
    pour aider à visualiser les changements de hauteur sur la texture (pas encore tout à fait implémenté).

    Contrôles :
     - Click gauche : ajouter du terrain
     - Click droit : enelever du terrain
     - Click milieu : déplacer la texture
     - Molette de souris : changer la taille de l'outil (en maintenant Ctrl pour zoomer sur la texture)
     - Nombre 1 à 9 : changer de brosse (juste 3 disponible pour le moment)
    
    Autres :
     - P : outil de peinture
     - O : outil de lissage
     - F : outil d'aplanissement
     - S : sauvegarder la texture peinte
     - Touche Z : annuler la dernière action

    Dans les prochaines versions, il serait intéressant d'implémenter du ray-tracing pour afficher non pas les hauteurs mais aussi les ombres pour mieux
    distinguer le relief du terrain.
    */
    
    [Header("General Settings")]
    public bool isEnabled = true;
    public Vector2Int paintSize = new Vector2Int(64, 64);
    public float physicalScale = 1f;
    public float zoom = 1f;
    public Material paintMaterial;

    [Header("Brush Settings")]
    public int brushIndex = 0;
    public float holdTimeToPaint = 0.1f;

    public List<BrushSettings> brushes = new List<BrushSettings>();

    [Header("Tool Settings")]
    public ToolType toolType = ToolType.Paint;
    public List<ToolParameters> tools = new List<ToolParameters>();

    [Header("Mouse Settings")]
    public GameObject brushGO;

    [Header("Saving Settings")]
    public int backupLimit = 5;
    private List<float[]> paintBackups = new List<float[]>();

    [Header("Overlay Settings")]
    public bool enabledHeightCurves = true;
    public int heightCurvesSpacing = 5;

    [Header("Others")]
    public Transform paintParent;
    public Canvas canvas;
    public TextureHelpers textureHelpers;

    private Texture2D paintTexture;
    private GameObject paintGO;
    private float lastPaintTime = 0f;
    private Vector2 holdOffset = Vector2.zero;
    private Vector2 initialPaintScale;

    private Texture2D overlayTexture;
    private GameObject overlayGO;

    void Start()
    {
        /*
        Préparer la liste des outils pour qu'ils soient utilisables.
        Initialiser la texture sur laquelle on peint.
        */
        
        InitializePainting();
        initialPaintScale = paintGO.transform.localScale;

        if (brushes.Count == 0)
        {
            BrushSettings defaultBrush = new BrushSettings();
            brushes.Add(defaultBrush);
        }

        MakeAllBrushesPng();
        
        foreach (var tool in tools)
        {
            if (tool.toolType == ToolType.Paint)
                tool.toolSettings = new PaintTool();
            else if (tool.toolType == ToolType.Smooth)
                tool.toolSettings = new SmoothingTool();
            else if (tool.toolType == ToolType.Flatten)
                tool.toolSettings = new FlattenTool();
        }
    }

    void Update()
    {
        /*
        Gérer les entrées de l'utilisateur pour peindre sur la texture, changer d'outil, sauvegarder, annuler, etc.
        */
        
        if (!isEnabled)
            return;
        
        lastPaintTime += Time.deltaTime;
        
        bool leftClick = Input.GetMouseButton(0);
        bool rightClick = Input.GetMouseButton(1);

        bool didLeftClick = Input.GetMouseButtonDown(0);
        bool didRightClick = Input.GetMouseButtonDown(1);

        bool canPaint = didLeftClick || didRightClick;
        bool remove = didRightClick;

        if (!canPaint)
        {
            if (leftClick || rightClick)
            {
                if (lastPaintTime >= holdTimeToPaint)
                {
                    canPaint = true;
                    remove = leftClick ? false : true;
                }
            }
        }

        if (canPaint)
        {
            lastPaintTime = 0f;

            Vector2 paintScreenSize = paintGO.transform.localScale * 100f * canvas.scaleFactor;
            Vector2 downLeftPaintPos = new Vector2(paintGO.transform.position.x, paintGO.transform.position.y) - paintScreenSize / 2f;

            Vector2 mousePosition = Input.mousePosition;

            Vector2Int paintPosition = new Vector2Int(
                Mathf.FloorToInt((mousePosition.x - downLeftPaintPos.x) / paintScreenSize.x * paintSize.x),
                Mathf.FloorToInt((mousePosition.y - downLeftPaintPos.y) / paintScreenSize.y * paintSize.y)
            );
            
            // Peindre avec l'outil et la brosse sélectionnés
            brushes[brushIndex].Apply(paintTexture, paintPosition, GetToolParameters(toolType), remove: remove);
            paintTexture.Apply();
            if (enabledHeightCurves)
            {
                Vector2Int updateZone = new Vector2Int(
                    Mathf.CeilToInt(GetToolParameters(toolType).size),
                    Mathf.CeilToInt(GetToolParameters(toolType).size)
                );

                Vector2Int updateOffset = paintPosition - new Vector2Int(updateZone.x / 2, updateZone.y / 2);

                UpdateHeightCurves(updateOffset, updateZone);
            }
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            if (Input.GetKey(KeyCode.LeftControl))
            {
                float lastZoom = zoom;
                zoom *= 1f + scroll;
                zoom = Mathf.Clamp(zoom, 0.1f, 10f);

                Vector3 mouseWorldPos = Input.mousePosition;
                Vector3 zoomCenter = paintGO.transform.position;
                Vector3 offset = mouseWorldPos - zoomCenter;
                paintGO.transform.position += offset * (1f - zoom / lastZoom);
            }
            else
            {
                ToolParameters tool = GetToolParameters(toolType);
                if (tool != null)
                {
                    tool.size -= Mathf.RoundToInt(scroll * 5f * (tool.size / 10f));
                    tool.size = Mathf.Clamp(tool.size, 1f, 100f);
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            SavePaintTexture();
        }

        for (int i = 1; i <= 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i))
            {
                brushIndex = i - 1;
                if (brushIndex >= brushes.Count)
                    brushIndex = brushes.Count - 1;
            }
        }

        foreach (ToolParameters toolParams in tools)
        {
            if (Input.GetKeyDown(toolParams.hotKey))
            {
                toolType = toolParams.toolType;
                break;
            }
        }

        if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1))
            SaveBackup();

        if (Input.GetKeyDown(KeyCode.Z))
            Undo();
        
        if (Input.GetMouseButtonDown(2))
        {
            holdOffset = Input.mousePosition - paintGO.transform.position;
        }

        if (Input.GetMouseButton(2))
        {
            paintGO.transform.position = Input.mousePosition - new Vector3(holdOffset.x, holdOffset.y, 0f);
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            enabledHeightCurves = !enabledHeightCurves;
            if (!enabledHeightCurves)
                ClearOverlay();
            else
                UpdateHeightCurves();
        }

        UpdatePaintGO();
        UpdateOverlayGO();
        UpdateBrushGO();
    }

    void MakeAllBrushesPng()
    {
        foreach (BrushSettings brush in brushes)
        {
            if (brush.texture != null)
            {
                // rewrite all pixels and set alpha to the .r channel

                Texture2D newTexture = new Texture2D(brush.texture.width, brush.texture.height, TextureFormat.RGBA32, false);
                newTexture.filterMode = FilterMode.Point;
                newTexture.wrapMode = TextureWrapMode.Clamp;

                for (int x = 0; x < brush.texture.width; x++)
                {
                    for (int y = 0; y < brush.texture.height; y++)
                    {
                        float value = brush.texture.GetPixel(x, y).r;
                        newTexture.SetPixel(x, y, new Color(value, value, value, value));
                    }
                }

                newTexture.Apply();
                brush.texture = newTexture;

                // Save as PNG
                //textureHelpers.SaveTexture(newTexture, $"Assets/Painting/Brushes/{brush.brushName + "_temp"}.png", makeReadable: true);
            }
        }
    }

    void SaveBackup()
    {
        /*
        Sauvegarder l'état actuel de la texture peinte pour permettre d'annuler les actions.
        On garde une liste de backups, et on limite sa taille pour éviter d'utiliser trop de mémoire.
        */
        
        if (paintTexture == null)
            return;

        float[] backup = paintTexture.GetRawTextureData<float>().ToArray();
        paintBackups.Add(backup);

        if (paintBackups.Count > backupLimit)
        {
            paintBackups.RemoveAt(0);
        }
    }

    public void Undo()
    {
        /*
        Revenir à un backup précédent de la texture peinte, si disponible.
        */
        
        if (paintBackups.Count <= 1)
            return;

        float[] backup = paintBackups[paintBackups.Count - 2];
        paintBackups.RemoveAt(paintBackups.Count - 1);

        paintTexture.SetPixelData(backup, 0);
        paintTexture.Apply();
    }

    ToolParameters GetToolParameters(ToolType toolType)
    {
        foreach (var tool in tools)
        {
            if (tool.toolType == toolType)
                return tool;
        }

        return null;
    }

    void UpdateBrushGO()
    {
        /*
        Mettre à jour l'interface du pinceau affichée à l'écran.
        Changer sa position, sa taille, et sa texture en fonction de la brosse sélectionnée et des paramètres de l'outil.
        */
        
        Texture2D brushTexture = brushes[brushIndex].texture;

        if (brushTexture != null)
        {
            brushGO.GetComponent<RawImage>().texture = brushTexture;
        }

        brushGO.transform.position = new Vector3(
            Input.mousePosition.x,
            Input.mousePosition.y,
            -5f
        );

        float brushSize = GetToolParameters(toolType)?.size ?? 10f;

        brushGO.GetComponent<RectTransform>().sizeDelta = new Vector2(
            brushSize * 100f / paintSize.x * zoom,
            brushSize * 100f / paintSize.y * zoom
        ) * physicalScale;

        brushGO.transform.SetAsLastSibling();

        brushGO.SetActive(true);
    }

    public void ClearOverlay()
    {
        for (int x = 0; x < paintSize.x; x++)
        {
            for (int y = 0; y < paintSize.y; y++)
            {
                overlayTexture.SetPixel(x, y, new Color(0f, 0f, 0f, 0f));
            }
        }
        overlayTexture.Apply();
    }

    public void UpdatePaintGO()
    {
        paintGO.transform.localScale = initialPaintScale * zoom;
    }

    void UpdateOverlayGO()
    {
        overlayGO.transform.localScale = paintGO.transform.localScale;
        overlayGO.transform.position = paintGO.transform.position;
    }

    public void UpdateHeightCurves(Vector2Int offset = default, Vector2Int zone = default)
    {
        /*
        Mettre des pixels noirs en opacité aux endroits où il y a des courbes de hauteur, pour aider à visualiser les changements de hauteur sur la texture.
        */

        offset = offset == default ? Vector2Int.zero : offset;
        zone = zone == default ? paintSize : zone;

        int minX = Mathf.Clamp(offset.x, 0, paintSize.x - 1);
        int minY = Mathf.Clamp(offset.y, 0, paintSize.y - 1);
        int maxX = Mathf.Clamp(offset.x + zone.x, minX, paintSize.x);
        int maxY = Mathf.Clamp(offset.y + zone.y, minY, paintSize.y);
        
        for (int x = minX; x < maxX; x++)
        {
            for (int y = minY; y < maxY; y++)
            {
                float heightValue = paintTexture.GetPixel(x, y).r;
                int level = (int)(heightValue * 255f / heightCurvesSpacing);

                // Look neighbors
                int levelN0 = (int)(PaintSample(x - 1, y) * 255f / heightCurvesSpacing);
                int levelN1 = (int)(PaintSample(x + 1, y) * 255f / heightCurvesSpacing);
                int levelN2 = (int)(PaintSample(x, y - 1) * 255f / heightCurvesSpacing);
                int levelN3 = (int)(PaintSample(x, y + 1) * 255f / heightCurvesSpacing);

                bool sameAsNeighbor = level == levelN0 && level == levelN1 && level == levelN2 && level == levelN3;

                if (!sameAsNeighbor)
                {
                    overlayTexture.SetPixel(x, y, new Color(0f, 0f, 0f, 1f));
                }
                else
                {
                    overlayTexture.SetPixel(x, y, new Color(0f, 0f, 0f, 0f));
                }
            }
        }

        overlayTexture.Apply();
    }

    public float PaintSample(int x, int y)
    {
        return paintTexture.GetPixel(
            Mathf.Clamp(x, 0, paintSize.x - 1),
            Mathf.Clamp(y, 0, paintSize.y - 1)
        ).r;
    }

    public void InitializePainting()
    {
        /*
        Initialisation des textures et des GameObjects nécessaires pour la peinture, ainsi que de leur affichage dans la scène.
        */
        
        if (paintGO == null)
            paintGO = new GameObject("PaintTexture");
        if (overlayGO == null)
            overlayGO = new GameObject("OverlayTexture");
        
        if (paintGO.GetComponent<RawImage>() == null)
            paintGO.AddComponent<RawImage>();
        
        if (overlayGO.GetComponent<RawImage>() == null)
            overlayGO.AddComponent<RawImage>();
        
        paintGO.GetComponent<RawImage>().material = paintMaterial;

        paintTexture = new Texture2D(paintSize.x, paintSize.y, TextureFormat.RFloat, false, true);
        overlayTexture = new Texture2D(paintSize.x, paintSize.y, TextureFormat.RGBA32, false);

        for (int x = 0; x < paintSize.x; x++)
        {
            for (int y = 0; y < paintSize.y; y++)
            {
                paintTexture.SetPixel(x, y, Color.black);
                overlayTexture.SetPixel(x, y, new Color(0f, 0f, 0f, 0f));
            }
        }

        paintTexture.Apply();
        overlayTexture.Apply();

        // Sauvegarder l'état initial de la texture peinte pour permettre d'annuler les actions dès le début
        SaveBackup();

        paintTexture.filterMode = FilterMode.Point;
        paintGO.transform.SetParent(paintParent, false);
        paintGO.GetComponent<RawImage>().texture = paintTexture;

        overlayTexture.filterMode = FilterMode.Point;
        overlayGO.transform.SetParent(paintParent, false);
        overlayGO.GetComponent<RawImage>().texture = overlayTexture;

        float maxSize = Mathf.Max(paintSize.x, paintSize.y);
        paintGO.transform.localScale = new Vector3(physicalScale * paintSize.x / maxSize, physicalScale * paintSize.y / maxSize, 1f);
        overlayGO.transform.localScale = new Vector3(physicalScale * paintSize.x / maxSize, physicalScale * paintSize.y / maxSize, 1f);

        if (enabledHeightCurves)
            UpdateHeightCurves();
    }

    public void SavePaintTexture(string name="Paint")
    {
        textureHelpers.SaveTexture(paintTexture, $"Assets/Painting/Textures/{name}.exr", makeReadable: true);
    }
}

[System.Serializable]
public class ToolParameters
{
    /*
    Classe pour stocker les paramètres d'un outil de peinture, comme sa taille, sa pression, et sa texture de brosse.
    */
    
    public ToolType toolType;
    public KeyCode hotKey;
    public float size = 16f;
    public float pressure = 1f;
    public float randomness = 0.1f;
    
    [HideInInspector]
    public ToolSettings toolSettings;
}

[System.Serializable]
public class BrushSettings
{
    /*
    Classe permettant de définir une brosse pour la peinture, avec un nom et une texture.
    La texture est utilisée pour moduler l'effet de la brosse en fonction de la position relative du pixel peint par rapport au centre de la brosse.
    */
    
    public string brushName = "Default Brush";
    public Texture2D texture;

    public void Apply(Texture2D paintTexture, Vector2Int position, ToolParameters toolParameters, bool remove = false)
    {
        /*
        Applique l'effet de la brosse sur la texture de peinture à la position donnée.
        Utilisation des positions relatives pour échantilloner la texture proportionnellement à la
        résolution cible.
        */
        
        Vector2Int startPos = new Vector2Int(
            Mathf.FloorToInt(position.x - toolParameters.size / 2),
            Mathf.FloorToInt(position.y - toolParameters.size / 2)
        );

        Vector2Int endPos = new Vector2Int(
            Mathf.FloorToInt(position.x + toolParameters.size / 2),
            Mathf.FloorToInt(position.y + toolParameters.size / 2)
        );

        float centralPixel = paintTexture.GetPixel(position.x, position.y).r;

        for (int x = startPos.x; x < endPos.x; x++)
        {
            for (int y = startPos.y; y < endPos.y; y++)
            {
                if (x < 0 || x >= paintTexture.width || y < 0 || y >= paintTexture.height)
                    continue;

                Vector2 texturePercentage = new Vector2(
                    (float)(x - startPos.x) / toolParameters.size,
                    (float)(y - startPos.y) / toolParameters.size
                );

                float sampledValue = SampleTexture(texturePercentage);

                float pressure = toolParameters.pressure;
                pressure += (Random.value - 0.5f) * toolParameters.randomness * 0.025f / sampledValue;
                pressure = Mathf.Clamp01(pressure);

                float finalValue = toolParameters.toolSettings.GetBrushValue(x, y, sampledValue, pressure, remove: remove, paintTexture, centralPixel);
                paintTexture.SetPixel(x, y, new Color(finalValue, 0f, 0f, 1f));
            }
        }
    }

    public float SampleTexture(Vector2 texturePercentage)
    {
        /*
        Échantillonner la texture de la brosse à partir des coordonnées de texture données,
        qui sont proportionnelles à la position du pixel peint par rapport au centre de la brosse.
        */

        if (texture == null)
            return 1f;

        Vector2Int textureSize = new Vector2Int(texture.width, texture.height);
        Vector2Int pixelPosition = new Vector2Int(
            Mathf.FloorToInt(texturePercentage.x * textureSize.x),
            Mathf.FloorToInt(texturePercentage.y * textureSize.y)
        );

        if (pixelPosition.x < 0 || pixelPosition.x >= textureSize.x || pixelPosition.y < 0 || pixelPosition.y >= textureSize.y)
            return 0f;

        return texture.GetPixel(pixelPosition.x, pixelPosition.y).r;
    }

    public Color InvertColor(Color inputColor)
    {
        return new Color(1f - inputColor.r, 1f - inputColor.g, 1f - inputColor.b, inputColor.a);
    }
}

[System.Serializable]
public class ToolSettings
{
    /*
    Classe de base pour les paramètres d'un outil de peinture, avec une méthode virtuelle pour calculer
    la valeur finale du pinceau à appliquer sur la texture de peinture.
    Les classes dérivées peuvent implémenter cette méthode pour créer différents comportements d'outils (peinture, lissage, aplanissement).
    */
    
    public string toolName = "Default Tool";
    public Texture2D icon;

    public virtual float GetBrushValue(int x, int y, float brushValue, float pressure, bool remove, Texture2D paintTexture, float centralPixel)
    {
        brushValue *= pressure;
        float paintValue = SampleTexture(paintTexture, new Vector2Int(x, y));

        if (remove)
            brushValue = paintValue - brushValue;
        else
            brushValue = paintValue + brushValue;
        
        return Mathf.Clamp01(brushValue);
    }

    public float SampleTexture(Texture2D texture, Vector2Int pixelPosition)
    {
        if (texture == null)
            return 1f;

        if (pixelPosition.x < 0 || pixelPosition.x >= texture.width || pixelPosition.y < 0 || pixelPosition.y >= texture.height)
            return -1f;

        return texture.GetPixel(pixelPosition.x, pixelPosition.y).r;
    }
}

[System.Serializable]
public class PaintTool : ToolSettings { }

[System.Serializable]
public class SmoothingTool : ToolSettings
{
    public override float GetBrushValue(int x, int y, float brushValue, float pressure, bool remove, Texture2D paintTexture, float centralPixel)
    {
        /*
        Prendre la moyenne des pixels entourant le pixel ciblé, pondérée par la pression et la valeur de la brosse, pour créer un effet de lissage.
        */
        
        int kernelSize = 3;
        int halfKernel = kernelSize / 2;

        float total = 0f;
        int count = 0;

        for (int offsetX = -halfKernel; offsetX <= halfKernel; offsetX++)
        {
            for (int offsetY = -halfKernel; offsetY <= halfKernel; offsetY++)
            {
                int sampleX = x + offsetX;
                int sampleY = y + offsetY;

                if (sampleX < 0 || sampleX >= paintTexture.width || sampleY < 0 || sampleY >= paintTexture.height)
                    continue;
                
                total += paintTexture.GetPixel(sampleX, sampleY).r * pressure;
                count++;
            }
        }

        float average = total / count;
        return Mathf.Lerp(paintTexture.GetPixel(x, y).r, average, pressure * Mathf.Sqrt(brushValue));
    }
}

[System.Serializable]
public class FlattenTool : ToolSettings
{
    public override float GetBrushValue(int x, int y, float brushValue, float pressure, bool remove, Texture2D paintTexture, float centralPixel)
    {
        /*
        Rapprocher les valeurs de tous les pixels du pixel central selon les valeurs d'intensité de la texture
        de la brosse de l'outil utilisé.
        */
        
        float currentHeight = paintTexture.GetPixel(x, y).r;
        return Mathf.Lerp(currentHeight, centralPixel, pressure * brushValue);
    }
}
