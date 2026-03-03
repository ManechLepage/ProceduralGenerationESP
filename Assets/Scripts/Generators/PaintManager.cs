using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public enum ToolType
{
    Paint,
    Smooth,
    Flatten
}

public class PaintManager : MonoBehaviour
{
    [Header("General Settings")]
    public bool isEnabled = true;
    public Vector2Int paintSize = new Vector2Int(64, 64);
    public float physicalScale = 1f;
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

    [Header("Others")]
    public Transform paintParent;
    public Canvas canvas;
    public TextureHelpers textureHelpers;

    private Texture2D paintTexture;
    private GameObject paintGO;
    private float lastPaintTime = 0f;

    void Start()
    {
        InitializePainting();

        if (brushes.Count == 0)
        {
            BrushSettings defaultBrush = new BrushSettings();
            brushes.Add(defaultBrush);
        }
        
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

            Vector2 downLeftPaintPos = paintGO.transform.position - paintGO.transform.localScale * 100f / 2f * canvas.scaleFactor;
            Vector2 paintScreenSize = paintGO.transform.localScale * 100f * canvas.scaleFactor;

            Vector2 mousePosition = Input.mousePosition;

            Vector2Int paintPosition = new Vector2Int(
                Mathf.FloorToInt((mousePosition.x - downLeftPaintPos.x) / paintScreenSize.x * paintSize.x),
                Mathf.FloorToInt((mousePosition.y - downLeftPaintPos.y) / paintScreenSize.y * paintSize.y)
            );
            
            
            brushes[brushIndex].Apply(paintTexture, paintPosition, GetToolParameters(toolType), remove: remove);
            paintTexture.Apply();
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            ToolParameters tool = GetToolParameters(toolType);
            if (tool != null)
            {
                tool.size -= Mathf.RoundToInt(scroll * 5f * (tool.size / 10f));
                tool.size = Mathf.Clamp(tool.size, 1f, 100f);
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

        UpdateBrushGO();
    }

    void SaveBackup()
    {
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
            brushSize * 100f / paintSize.x,
            brushSize * 100f / paintSize.y
        ) * physicalScale;

        brushGO.transform.SetAsLastSibling();

        brushGO.SetActive(true);
    }
    public void InitializePainting()
    {
        if (paintGO == null)
            paintGO = new GameObject("PaintTexture");
        
        if (paintGO.GetComponent<RawImage>() == null)
            paintGO.AddComponent<RawImage>();
        
        paintGO.GetComponent<RawImage>().material = paintMaterial;

        paintTexture = new Texture2D(paintSize.x, paintSize.y, TextureFormat.RFloat, false, true);

        for (int x = 0; x < paintSize.x; x++)
        {
            for (int y = 0; y < paintSize.y; y++)
            {
                paintTexture.SetPixel(x, y, Color.black);
            }
        }

        paintTexture.Apply();

        SaveBackup();

        paintTexture.filterMode = FilterMode.Point;
        paintGO.transform.SetParent(paintParent, false);
        paintGO.GetComponent<RawImage>().texture = paintTexture;

        float maxSize = Mathf.Max(paintSize.x, paintSize.y);
        paintGO.transform.localScale = new Vector3(physicalScale * paintSize.x / maxSize, physicalScale * paintSize.y / maxSize, 1f);
    }

    public void SavePaintTexture(string name="Paint")
    {
        textureHelpers.SaveTexture(paintTexture, $"Assets/Painting/Textures/{name}.exr", makeReadable: true);
    }
}

[System.Serializable]
public class ToolParameters
{
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
    public string brushName = "Default Brush";
    public Texture2D texture;

    public void Apply(Texture2D paintTexture, Vector2Int position, ToolParameters toolParameters, bool remove = false)
    {
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
        Texture percentage represents the relative coordinates on the brush,
        in order to sample a certain part of the texture
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
        float currentHeight = paintTexture.GetPixel(x, y).r;
        return Mathf.Lerp(currentHeight, centralPixel, pressure * brushValue);
    }
}
