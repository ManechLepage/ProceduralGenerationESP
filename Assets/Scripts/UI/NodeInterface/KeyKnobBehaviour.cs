using UnityEngine;
using UnityEngine.UI;
using System;

public class KeyKnobBehaviour : MonoBehaviour
{
    public Image background;
    public float knobSize = 20f;

    [Space]
    public Keyframe key;
    public CurveNode curveNode;

    [Space]
    public RectTransform handleRect1;
    public RectTransform handleRect2;

    public RectTransform knob1;
    public RectTransform knob2;

    private bool knobsEnabled = false;
    private RectTransform selectedKnob;

    public void DeselectAllKnobs()
    {
        selectedKnob = null;
    }

    void Update()
    {
        if (!knobsEnabled) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (MouseInKnob(knob1))
            {
                selectedKnob = knob1;
            }
            else if (MouseInKnob(knob2))
            {
                selectedKnob = knob2;
            }

            if (selectedKnob != null)
            {
                UIDraggable draggable = curveNode.GetComponent<UIDraggable>();
                draggable.CancelNextDrag();
            }
        }
        else if (Input.GetMouseButton(0))
        {
            if (selectedKnob != null)
            {
                Vector2 localMousePos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(background.transform as RectTransform, Input.mousePosition, null, out localMousePos);
                
                float distance = localMousePos.magnitude;

                if (selectedKnob == knob1)
                {
                    float angle = Mathf.Atan2(-localMousePos.y, -localMousePos.x);

                    handleRect1.sizeDelta = new Vector2(distance, handleRect1.sizeDelta.y);
                    handleRect1.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg);

                    key.inTangent = Mathf.Tan(angle);
                    key.inWeight = distance / knobSize;
                }
                else
                {
                    float angle = Mathf.Atan2(localMousePos.y, localMousePos.x);

                    handleRect2.sizeDelta = new Vector2(distance, handleRect2.sizeDelta.y);
                    handleRect2.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg);

                    key.outTangent = Mathf.Tan(angle);
                    key.outWeight = distance / knobSize;
                }

                curveNode.DidUpdateKeyframe(this);
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            selectedKnob = null;
        }
    }

    bool MouseInKnob(RectTransform knob)
    {
        Vector2 localMousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(knob, Input.mousePosition, null, out localMousePos);
        return knob.rect.Contains(localMousePos);
    }

    public void SetKnobKeyframe(Keyframe keyframe)
    {
        key = keyframe;

        // Set the positions of the handle rects based on the tangents and weights
        float angle1 = Mathf.Atan2(keyframe.inTangent, 1f) * Mathf.Rad2Deg;
        float angle2 = Mathf.Atan2(keyframe.outTangent, 1f) * Mathf.Rad2Deg;

        handleRect1.sizeDelta = new Vector2(keyframe.inWeight * knobSize, handleRect1.rect.height);
        handleRect2.sizeDelta = new Vector2(keyframe.outWeight * knobSize, handleRect2.rect.height);

        handleRect1.localRotation = Quaternion.Euler(0f, 0f, angle1);
        handleRect2.localRotation = Quaternion.Euler(0f, 0f, angle2);

        if (keyframe.inWeight == 0f) { handleRect1.gameObject.SetActive(false); } else { handleRect1.gameObject.SetActive(true); }
        if (keyframe.outWeight == 0f) { handleRect2.gameObject.SetActive(false); } else { handleRect2.gameObject.SetActive(true); }
    }

    public void Disable()
    {
        knobsEnabled = false;

        handleRect1.gameObject.SetActive(false);
        handleRect2.gameObject.SetActive(false);
        knob1.gameObject.SetActive(false);
        knob2.gameObject.SetActive(false);
    }

    public void Enable()
    {
        knobsEnabled = true;

        handleRect1.gameObject.SetActive(true);
        handleRect2.gameObject.SetActive(true);
        knob1.gameObject.SetActive(true);
        knob2.gameObject.SetActive(true);
    }
}
