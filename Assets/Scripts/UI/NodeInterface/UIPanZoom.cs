using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections;

public class UIPanZoom : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Pan Settings")]
    public float panSpeed = 1f;
    public bool requireMiddleMouseButton = true;

    [Header("Zoom Settings")]
    public float zoomSpeed = 0.1f;
    public float minZoom = 0.1f;
    public float maxZoom = 5f;
    public float zoom = 1f;
    public float zoomAnimationDuration = 0.05f;

    [Header("References")]
    public RectTransform nodeContainer;

    private bool isHovered = false;
    private bool isPanning = false;
    private Vector2 lastMousePosition;
    private Canvas canvas;

    private float lastTargetZoom;

    void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        lastTargetZoom = zoom;

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

            float scaleFactor = canvas.scaleFactor / zoom;
            if (nodeContainer != this.GetComponent<RectTransform>())
                scaleFactor = nodeContainer.lossyScale.x;

            Vector2 currentMousePos = mouse.position.ReadValue();
            Vector2 delta = (currentMousePos - lastMousePosition) * zoom / scaleFactor;
            lastMousePosition = currentMousePos;

            nodeContainer.anchoredPosition += delta * panSpeed;

            GraphManager.Instance.currentOffset = nodeContainer.anchoredPosition;
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

        StopAllCoroutines();

        float lastZoom = zoom;
        float nextZoom = lastTargetZoom * (1f + scroll * zoomSpeed);
        nextZoom = Mathf.Clamp(nextZoom, minZoom, maxZoom);
        lastTargetZoom = nextZoom;

        Vector3 lastContainerPos = nodeContainer.position;
        Vector3 lastContainerScale = nodeContainer.localScale;

        Vector3 mouseWorldPos = Input.mousePosition;
        Vector3 zoomCenter = nodeContainer.transform.position;
        Vector3 offset = mouseWorldPos - zoomCenter;

        Vector3 targetPosition = nodeContainer.position + offset * (1f - nextZoom / lastZoom);
        Vector3 targetScale = nodeContainer.localScale * (nextZoom / lastZoom);

        StartCoroutine(AnimateZoom(nodeContainer.gameObject, lastContainerPos, lastContainerScale, targetPosition, targetScale, lastZoom, nextZoom, zoomAnimationDuration));

        //GraphManager.Instance.currentZoom = zoom;
        //GraphManager.Instance.currentOffset = nodeContainer.anchoredPosition;
    }

    IEnumerator AnimateZoom(GameObject container, Vector3 startPosition, Vector3 startScale,
        Vector3 targetPosition, Vector3 targetScale, float startZoom, float targetZoom,
        float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            container.transform.position = EaseVector3(startPosition, targetPosition, t);
            container.transform.localScale = EaseVector3(startScale, targetScale, t);
            
            zoom = EaseFloat(startZoom, targetZoom, t);
            GraphManager.Instance.currentZoom = zoom;
            GraphManager.Instance.currentOffset = nodeContainer.anchoredPosition;

            yield return null;
        }
        container.transform.position = targetPosition;
        container.transform.localScale = targetScale;
    }

    public Vector3 EaseVector3(Vector3 start, Vector3 end, float t)
    {
        t = t * t * (3f - 2f * t); // Smoothstep easing
        return Vector3.Lerp(start, end, t);
    }

    public float EaseFloat(float start, float end, float t)
    {
        t = t * t * (3f - 2f * t); // Smoothstep easing
        return Mathf.Lerp(start, end, t);
    }

    public void OnPointerEnter(PointerEventData eventData) => isHovered = true;
    public void OnPointerExit(PointerEventData eventData) => isHovered = false;
}
