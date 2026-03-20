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
    private List<KeyKnobBehaviour> keyFrames = new List<KeyKnobBehaviour>();
    private KeyKnobBehaviour currentKeyFrame;
    private bool didMoveKey = false;
    private Vector2 initialKeyPosition;

    private Vector2 moveOffset;

    private KeyKnobBehaviour selectedKeyFrame;

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

    void Select(KeyKnobBehaviour keyFrame)
    {
        Deselect();

        selectedKeyFrame = keyFrame;
        if (selectedKeyFrame != null)
        {
            keyFrame.background.color = selectedKeyColor;
            keyFrame.Enable();

            // Keyframe keyframe = selectedKeyFrame.key;
            // Debug.Log($"New key: time={keyframe.time}, value={keyframe.value}, inTangent={keyframe.inTangent}, outTangent={keyframe.outTangent}, inWeight={keyframe.inWeight}, outWeight={keyframe.outWeight}");

        }
    }

    void Deselect()
    {
        if (selectedKeyFrame != null)
        {
            selectedKeyFrame.background.color = normalKeyColor;
            selectedKeyFrame.Disable();
        }

        selectedKeyFrame = null;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePosition = Input.mousePosition;
            KeyKnobBehaviour pressedKeyFrame = GetPressedKeyFrame(mousePosition);
            if (pressedKeyFrame != null)
            {
                UIDraggable draggable = GetComponent<UIDraggable>();
                if (draggable != null)
                    draggable.CancelNextDrag();

                moveOffset = (Vector2)pressedKeyFrame.gameObject.transform.position - mousePosition;
                currentKeyFrame = pressedKeyFrame;
                Select(currentKeyFrame);
                didMoveKey = false;
                initialKeyPosition = pressedKeyFrame.gameObject.GetComponent<RectTransform>().anchoredPosition;
            }
        }

        if (Input.GetMouseButton(0) && currentKeyFrame != null)
        {
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                // Delete the current keyframe

                Deselect();

                int keyIndex = GetKeyIndex(currentKeyFrame);
                curve.RemoveKey(keyIndex);

                Destroy(currentKeyFrame.gameObject);
                keyFrames.Remove(currentKeyFrame);

                currentKeyFrame = null;

                UpdateLine();
                InputUpdated(null);
            }
            else
            {
                // Move the current keyframe based on mouse position
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

                    if (keyIndex != -1)
                    {
                        currentKeyFrame.key.time = time;
                        currentKeyFrame.key.value = value;

                        UpdateKeyGOPosition(currentKeyFrame.gameObject, time, value);
                        curve.MoveKey(keyIndex, currentKeyFrame.key);
                        UpdateLine();
                    }
                    else
                    {
                        Debug.Log($"Error: Could not find key index for key with time {currentKeyFrame.key.time} and value {currentKeyFrame.key.value}");
                    }
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (currentKeyFrame != null)
            {
                float moveDistance = Vector2.Distance(currentKeyFrame.gameObject.GetComponent<RectTransform>().anchoredPosition, initialKeyPosition);
                if (moveDistance > 0.1f)
                {
                    didMoveKey = true;
                }
            }

            if (didMoveKey && currentKeyFrame != null)
            {
                // If a key was moved, send a signal to update the output nodes.
                InputUpdated(null);
                Deselect();
            }
            else if (currentKeyFrame == null)
            {
                // This deselect is called before the keyKnobBehaviour's update, so we need to manually deselect the knobs
                if (selectedKeyFrame != null)
                {
                    selectedKeyFrame.DeselectAllKnobs();
                }

                Deselect();

                // Check if a double click is occuring.
                if (didFirstClick)
                {
                    float dt = Time.time - lastClickTime;
                    Vector2 clickDelta = (Vector2)Input.mousePosition - firstClickPosition;
                    KeyKnobBehaviour newKey = null;
                    if (dt <= doubleClickInterval && clickDelta.magnitude < 5f)
                    {
                        newKey = CreateNewKey(Input.mousePosition);
                        Select(newKey);
                        didFirstClick = false;
                    }
                    else
                    {
                        didFirstClick = true;
                        lastClickTime = Time.time;
                        firstClickPosition = Input.mousePosition;
                    }
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
    }

    public void DidUpdateKeyframe(KeyKnobBehaviour keyFrame)
    {
        int keyIndex = GetKeyIndex(keyFrame);
        if (keyIndex != -1)
        {
            curve.MoveKey(keyIndex, keyFrame.key);
            UpdateLine();
            InputUpdated(null);
        }
        else
        {
            Debug.Log($"Error: Could not find key index for key with time {keyFrame.key.time} and value {keyFrame.key.value}");
        }
    }

    public KeyKnobBehaviour CreateNewKey(Vector2 mousePosition)
    {
        RectTransform rectTransform = lineGO.GetComponent<RectTransform>();
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, mousePosition, null, out localPoint);

        float time = Mathf.Clamp01((localPoint.x / rectTransform.rect.width) + 0.5f);
        float value = Mathf.Clamp01((localPoint.y / rectTransform.rect.height) + 0.5f);

        if (!OccupiedKeyTime(time))
        {
            Keyframe newKey = new Keyframe(time, value);
            newKey.inTangent = 0f;
            newKey.outTangent = 0f;
            newKey.inWeight = 1/3f;
            newKey.outWeight = 1/3f;
            curve.AddKey(newKey);

            GameObject keyGO = Instantiate(keyPrefab, keyParent.transform);
            KeyKnobBehaviour knobBehaviour = keyGO.GetComponent<KeyKnobBehaviour>();
            knobBehaviour.SetKnobKeyframe(newKey);
            knobBehaviour.curveNode = this;
            keyFrames.Add(knobBehaviour);

            knobBehaviour.Disable();

            UpdateKeyGOPosition(keyGO, time, value);

            UpdateLine();
            InputUpdated(null);

            return knobBehaviour;
        }
        return null;
    }

    bool OccupiedKeyTime(float time)
    {
        foreach (var keyFrame in keyFrames)
        {
            if (Mathf.Approximately(keyFrame.key.time, time))
            {
                return true;
            }
        }
        return false;
    }

    int GetKeyIndex(KeyKnobBehaviour keyFrame)
    {
        int keyIndex = -1;
        for (int i = 0; i < curve.length; i++)
        {
            if (curve.keys[i].time == keyFrame.key.time && curve.keys[i].value == keyFrame.key.value)
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

    public KeyKnobBehaviour GetPressedKeyFrame(Vector2 mousePosition)
    {
        foreach (var keyFrame in keyFrames)
        {
            RectTransform keyRect = keyFrame.gameObject.GetComponent<RectTransform>();
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
            Destroy(keyFrame.gameObject);
        }
        keyFrames.Clear();

        for (int i = 0; i < curve.length; i++)
        {
            Keyframe keyframe = curve[i];

            GameObject keyGO = Instantiate(keyPrefab, keyParent.transform);
            KeyKnobBehaviour knobBehaviour = keyGO.GetComponent<KeyKnobBehaviour>();
            knobBehaviour.SetKnobKeyframe(keyframe);
            knobBehaviour.curveNode = this;
            keyFrames.Add(knobBehaviour);

            knobBehaviour.Disable();

            UpdateKeyGOPosition(keyGO, keyframe.time, keyframe.value);
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
