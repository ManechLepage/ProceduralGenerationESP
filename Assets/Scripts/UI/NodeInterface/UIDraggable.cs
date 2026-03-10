using UnityEngine;
using UnityEngine.EventSystems;

public class UIDraggable : MonoBehaviour
{
    private RectTransform rectTransform;
    public Canvas canvas;
    private Vector2 offset;
    private bool isDragging;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        bool leftMouseButtonDown = Input.GetMouseButtonDown(0);
        bool leftMouseButton = Input.GetMouseButton(0);
        Vector2 mousePosition = Input.mousePosition;

        if (!isDragging)
        {
            bool isInRect = RectTransformUtility.RectangleContainsScreenPoint(rectTransform, mousePosition, canvas.worldCamera);
            if (isInRect)
            {
                if (leftMouseButtonDown)
                {
                    isDragging = true;
                    offset = (Vector2)rectTransform.position - mousePosition;
                }
            }
        }
        else
        {
            rectTransform.position = mousePosition + offset;
        }

        if (!leftMouseButton)
        {
            isDragging = false;
        }
    }
}
