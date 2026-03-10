using UnityEngine;
using UnityEngine.EventSystems;

public class UIDraggable : MonoBehaviour
{
    public Canvas canvas;
    public float sideBorder = 15f;
    private RectTransform rectTransform;
    
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
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x - sideBorder * 2, rectTransform.sizeDelta.y);
            rectTransform.position = new Vector2(rectTransform.position.x + sideBorder, rectTransform.position.y);

            bool isInRect = RectTransformUtility.RectangleContainsScreenPoint(rectTransform, mousePosition, canvas.worldCamera);

            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x + sideBorder * 2, rectTransform.sizeDelta.y);
            rectTransform.position = new Vector2(rectTransform.position.x - sideBorder, rectTransform.position.y);

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
