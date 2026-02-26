using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class UIPanZoom : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Pan Settings")]
    public float panSpeed = 1f;
    public bool requireMiddleMouseButton = true;

    [Header("Zoom Settings")]
    public float zoomSpeed = 0.1f;
    public float minZoom = 0.1f;
    public float maxZoom = 5f;

    [Header("References")]
    public RectTransform nodeContainer;

    private bool isHovered = false;
    private bool isPanning = false;
    private Vector2 lastMousePosition;
    private Canvas canvas;

    void Awake()
    {
        canvas = GetComponentInParent<Canvas>();

        if (nodeContainer == null)
            nodeContainer = transform as RectTransform;
    }

    void Update()
    {
        HandlePan();
        HandleZoom();
    }

    void HandlePan()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        bool panButton = requireMiddleMouseButton
            ? mouse.middleButton.isPressed
            : mouse.rightButton.isPressed;

        if (panButton && isHovered)
        {
            if (!isPanning)
            {
                isPanning = true;
                lastMousePosition = mouse.position.ReadValue();
            }

            Vector2 currentMousePos = mouse.position.ReadValue();
            Vector2 delta = (currentMousePos - lastMousePosition) / canvas.scaleFactor;
            lastMousePosition = currentMousePos;

            nodeContainer.anchoredPosition += delta * panSpeed;
        }
        else
        {
            isPanning = false;
        }
    }

    void HandleZoom()
    {
        if (!isHovered) return;

        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Approximately(scroll, 0f)) return;

        float currentScale = nodeContainer.localScale.x;
        float newScale = Mathf.Clamp(currentScale + scroll * zoomSpeed, minZoom, maxZoom);

        nodeContainer.localScale = Vector3.one * newScale;
    }

    public void OnPointerEnter(PointerEventData eventData) => isHovered = true;
    public void OnPointerExit(PointerEventData eventData) => isHovered = false;
}