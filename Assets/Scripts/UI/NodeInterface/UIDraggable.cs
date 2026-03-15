using UnityEngine;
using UnityEngine.EventSystems;

public class UIDraggable : MonoBehaviour
{
    private Canvas canvas;
    public float leftSideBorder = 2.5f;
    public float rightSideBorder = 10f;
    private RectTransform rectTransform;
    
    private Vector2 offset;
    private bool isDragging;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
    }

    void Update()
    {
        bool leftMouseButtonDown = Input.GetMouseButtonDown(0);
        bool leftMouseButton = Input.GetMouseButton(0);
        Vector2 mousePosition = Input.mousePosition;

        if (isDragging && Input.GetKeyDown(KeyCode.Backspace))
        {
            isDragging = false;
            GraphManager.Instance.nodes.Remove(GetComponent<NodeBehaviour>());
            Destroy(gameObject);
        }

        if (!isDragging)
        {
            float totalBorder = leftSideBorder + rightSideBorder;
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x - totalBorder, rectTransform.sizeDelta.y);
            rectTransform.position = new Vector2(rectTransform.position.x + leftSideBorder, rectTransform.position.y);

            bool isInRect = RectTransformUtility.RectangleContainsScreenPoint(rectTransform, mousePosition, canvas.worldCamera);

            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x + totalBorder, rectTransform.sizeDelta.y);
            rectTransform.position = new Vector2(rectTransform.position.x - leftSideBorder, rectTransform.position.y);

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
