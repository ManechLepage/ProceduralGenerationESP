using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
public class CurveNode : NodeBehaviour
{
    public float doubleClickInterval = 0.5f;
    public Color normalKeyColor = Color.white;
    public Color selectedKeyColor = Color.blue;

    [Space]
    public RectTransform previewContainer;
    public GameObject keyPrefab;
    public AnimationCurve curve;
    public GameObject lineGO;

    [Space]
    public GameObject keyParent;

    private MaskableUILineRenderer lineRenderer;
    private List<UIKeyFrame> keyFrames = new List<UIKeyFrame>();
    private UIKeyFrame currentKeyFrame;
    private bool didMoveKey = false;
    private Vector2 moveOffset;

    private UIKeyFrame selectedKeyFrame;

    private bool didFirstClick = false;
    private Vector2 firstClickPosition;
    private float lastClickTime = 0f;

    void Awake()
    {
        lineRenderer = lineGO.GetComponent<MaskableUILineRenderer>();
    }

    public override void Start()
    {
        base.Start();

        UpdateLine();
        RecreateKeys();
    }

    public override Variant OnFire()
    {
        return new Variant(curve);
    }

    public override void InputUpdated(ConnectorBehaviour connector)
    {
        base.InputUpdated(connector);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePosition = Input.mousePosition;
            UIKeyFrame pressedKeyFrame = GetPressedKeyFrame(mousePosition);
            if (pressedKeyFrame != null)
            {
                UIDraggable draggable = GetComponent<UIDraggable>();
                if (draggable != null)
                    draggable.CancelNextDrag();

                moveOffset = (Vector2)pressedKeyFrame.keyGO.transform.position - mousePosition;
                currentKeyFrame = pressedKeyFrame;
                didMoveKey = false;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (didMoveKey && currentKeyFrame != null)
            {
                InputUpdated(null);
            }
            else if (currentKeyFrame != null)
            {
                if (selectedKeyFrame != null)
                    selectedKeyFrame.keyGO.GetComponent<Image>().color = normalKeyColor;

                selectedKeyFrame = currentKeyFrame;
                selectedKeyFrame.keyGO.GetComponent<Image>().color = selectedKeyColor;
            }
            else if (currentKeyFrame == null)
            {
                if (didFirstClick)
                {
                    float dt = Time.time - lastClickTime;
                    Vector2 clickDelta = (Vector2)Input.mousePosition - firstClickPosition;
                    if (dt <= doubleClickInterval && clickDelta.magnitude < 5f)
                        CreateNewKey(Input.mousePosition);

                    didFirstClick = false;
                }
                else
                {
                    didFirstClick = true;
                    lastClickTime = Time.time;
                    firstClickPosition = Input.mousePosition;
                }
            }

            currentKeyFrame = null;
            moveOffset = Vector2.zero;
        }

        if (Input.GetMouseButton(0) && currentKeyFrame != null)
        {
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                int keyIndex = GetKeyIndex(currentKeyFrame);
                curve.RemoveKey(keyIndex);
                Destroy(currentKeyFrame.keyGO);
                keyFrames.Remove(currentKeyFrame);
                UpdateLine();
                currentKeyFrame = null;
                InputUpdated(null);
                return;
            }
            else
            {
                Vector2 mousePosition = Input.mousePosition;
                mousePosition += moveOffset;

                RectTransform rectTransform = lineGO.GetComponent<RectTransform>();
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, mousePosition, null, out localPoint);

                float time = Mathf.Clamp01((localPoint.x / rectTransform.rect.width) + 0.5f);
                float value = Mathf.Clamp01((localPoint.y / rectTransform.rect.height) + 0.5f);

                if (!OccupiedKeyTime(time))  // Do not move the key if the new time is already occupied by another key
                {
                    int keyIndex = GetKeyIndex(currentKeyFrame);
                    
                    currentKeyFrame.curveKey.time = time;
                    currentKeyFrame.curveKey.value = value;

                    UpdateKeyGOPosition(currentKeyFrame.keyGO, time, value);
                    curve.MoveKey(keyIndex, currentKeyFrame.curveKey);
                    UpdateLine();

                    didMoveKey = true;
                }

                if (didMoveKey && selectedKeyFrame != null)
                {
                    selectedKeyFrame.keyGO.GetComponent<Image>().color = normalKeyColor;
                    selectedKeyFrame = null;
                }
            }
        }
    }

    public void CreateNewKey(Vector2 mousePosition)
    {
        RectTransform rectTransform = lineGO.GetComponent<RectTransform>();
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, mousePosition, null, out localPoint);

        float time = Mathf.Clamp01((localPoint.x / rectTransform.rect.width) + 0.5f);
        float value = Mathf.Clamp01((localPoint.y / rectTransform.rect.height) + 0.5f);

        if (!OccupiedKeyTime(time))
        {
            Keyframe newKey = new Keyframe(time, value);
            curve.AddKey(newKey);

            GameObject keyGO = Instantiate(keyPrefab, keyParent.transform);
            UIKeyFrame uiKeyFrame = new UIKeyFrame {
                keyGO = keyGO,
                curveKey = newKey
            };
            keyFrames.Add(uiKeyFrame);

            UpdateKeyGOPosition(keyGO, time, value);

            keyGO.SetActive(true);

            UpdateLine();
            InputUpdated(null);
        }
    }

    bool OccupiedKeyTime(float time)
    {
        foreach (var keyFrame in keyFrames)
        {
            if (Mathf.Approximately(keyFrame.curveKey.time, time))
            {
                return true;
            }
        }
        return false;
    }

    int GetKeyIndex(UIKeyFrame keyFrame)
    {
        int keyIndex = -1;
        for (int i = 0; i < curve.length; i++)
        {
            if (curve.keys[i].time == keyFrame.curveKey.time && curve.keys[i].value == keyFrame.curveKey.value)
            {
                keyIndex = i;
                break;
            }
        }
        return keyIndex;
    }

    void UpdateKeyGOPosition(GameObject keyGO, float time, float value)
    {
        RectTransform rectTransform = lineGO.GetComponent<RectTransform>();

        float x = (time - 0.5f) * rectTransform.rect.width;
        float y = (value - 0.5f) * rectTransform.rect.height;
        keyGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, y);
    }

    public UIKeyFrame GetPressedKeyFrame(Vector2 mousePosition)
    {
        foreach (var keyFrame in keyFrames)
        {
            RectTransform keyRect = keyFrame.keyGO.GetComponent<RectTransform>();
            if (RectTransformUtility.RectangleContainsScreenPoint(keyRect, mousePosition, null))
            {
                return keyFrame;
            }
        }
        return null;
    }

    public Vector2 PercentageToPosition(Vector2 percentage)
    {
        RectTransform rectTransform = lineGO.GetComponent<RectTransform>();

        float x = percentage.x * rectTransform.rect.width;
        float y = percentage.y * rectTransform.rect.height;
        return new Vector2(x, y);
    }

    void RecreateKeys()
    {
        foreach (var keyFrame in keyFrames)
        {
            Destroy(keyFrame.keyGO);
        }
        keyFrames.Clear();

        for (int i = 0; i < curve.length; i++)
        {
            Keyframe keyframe = curve[i];

            GameObject keyGO = Instantiate(keyPrefab, keyParent.transform);
            keyFrames.Add(new UIKeyFrame {
                keyGO = keyGO,
                curveKey = keyframe
            });

            UpdateKeyGOPosition(keyGO, keyframe.time, keyframe.value);

            keyGO.SetActive(true);
        }
    }

    void UpdateLine()
    {
        int resolution = 100;
        float step = 1f / (resolution - 1);

        List<Vector2> points = new List<Vector2>();
        for (int i = 0; i < resolution; i++)
        {
            float t = i * step;
            float value = curve.Evaluate(t);

            Vector2 position = PercentageToPosition(new Vector2(t, value));

            points.Add(position);
        }

        lineRenderer.points = points.ToArray();
        lineRenderer.SetVerticesDirty();
    }
}

[System.Serializable]
public class UIKeyFrame
{
    public GameObject keyGO;
    public Keyframe curveKey;
}
