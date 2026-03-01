using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PaintManager : MonoBehaviour
{
    [Header("General Settings")]
    public bool isEnabled = true;
    public Vector2Int paintSize = new Vector2Int(64, 64);
    public float physicalScale = 1f;
    public Material paintMaterial;

    [Header("Brush Settings")]
    public float brushSize = 16f;
    public float brushOpacity = 1f;
    public int brushIndex = 0;
    public float holdTimeToPaint = 0.1f;

    public List<BrushSettings> brushes = new List<BrushSettings>();

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
        }

        if (canPaint)
        {
            Vector2 downLeftPaintPos = paintGO.transform.position - paintGO.transform.localScale * 100f / 2f * canvas.scaleFactor;
            Vector2 paintScreenSize = paintGO.transform.localScale * 100f * canvas.scaleFactor;

            Vector2 mousePosition = Input.mousePosition;

            Vector2Int paintPosition = new Vector2Int(
                Mathf.FloorToInt((mousePosition.x - downLeftPaintPos.x) / paintScreenSize.x * paintSize.x),
                Mathf.FloorToInt((mousePosition.y - downLeftPaintPos.y) / paintScreenSize.y * paintSize.y)
            );
            
            if (paintPosition.x >= 0 && paintPosition.x < paintSize.x && paintPosition.y >= 0 && paintPosition.y < paintSize.y)
            {
                brushes[brushIndex].Apply(paintTexture, paintPosition, brushSize, brushOpacity, remove: remove);
                paintTexture.Apply();
            }
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            brushSize -= Mathf.RoundToInt(scroll * 5f * (brushSize / 10f));
            brushSize = Mathf.Clamp(brushSize, 1f, 100f);
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            SavePaintTexture();
        }

        // Number = brush
        for (int i = 1; i <= 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i))
            {
                brushIndex = i - 1;
                if (brushIndex >= brushes.Count)
                    brushIndex = brushes.Count - 1;
            }
        }
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
public class BrushSettings
{
    public string brushName = "Default Brush";
    public Texture2D texture;

    public void Apply(Texture2D paintTexture, Vector2Int position, float size, float opacity, bool additive = true, bool remove = false)
    {
        Vector2Int startPos = new Vector2Int(
            Mathf.FloorToInt(position.x - size / 2),
            Mathf.FloorToInt(position.y - size / 2)
        );

        Vector2Int endPos = new Vector2Int(
            Mathf.FloorToInt(position.x + size / 2),
            Mathf.FloorToInt(position.y + size / 2)
        );

        for (int x = startPos.x; x < endPos.x; x++)
        {
            for (int y = startPos.y; y < endPos.y; y++)
            {
                Vector2 texturePercentage = new Vector2(
                    (float)(x - startPos.x) / size,
                    (float)(y - startPos.y) / size
                );

                float sampledValue = SampleTexture(texturePercentage);
                float finalValue = sampledValue * opacity;

                if (x >= 0 && x < paintTexture.width && y >= 0 && y < paintTexture.height)
                {
                    float paintValue = paintTexture.GetPixel(x, y).r;
                    float blendedValue;
                    if (!additive)
                    {
                        if (remove)
                        {
                            finalValue = 1f - finalValue;
                        }
                        blendedValue = Mathf.Lerp(paintValue, finalValue, finalValue);
                    }
                    else
                    {
                        if (!remove)
                            blendedValue = paintValue + finalValue;
                        else
                            blendedValue = paintValue - finalValue;
                        
                        blendedValue = Mathf.Clamp01(blendedValue);
                    }

                    paintTexture.SetPixel(x, y, new Color(blendedValue, 0f, 0f, 1f));
                }
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
