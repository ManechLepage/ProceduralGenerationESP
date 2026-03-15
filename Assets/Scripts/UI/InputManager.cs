using UnityEngine;

public class InputManager : MonoBehaviour
{
    public GameObject nodeCreationMenu;
    public GameObject canvas;

    void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.openedUI = true;
        
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            canvas.SetActive(!canvas.activeSelf);

            if (GameManager.Instance != null)
                GameManager.Instance.openedUI = canvas.activeSelf;
            
            if (!canvas.activeSelf)
            {
                nodeCreationMenu.SetActive(false);
            }
        }

        if (!GameManager.Instance.openedUI) return;

        bool mouseInCreationMenu = RectTransformUtility.RectangleContainsScreenPoint(
            nodeCreationMenu.GetComponent<RectTransform>(),
            Input.mousePosition,
            null
        );

        if (nodeCreationMenu.activeSelf && Input.GetMouseButtonUp(0) && !mouseInCreationMenu)
        {
            nodeCreationMenu.SetActive(false);
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (nodeCreationMenu.activeSelf)
            {
                nodeCreationMenu.SetActive(false);
                return;
            }
            
            Vector3 mousePos = Input.mousePosition;
            nodeCreationMenu.transform.position = mousePos;
            nodeCreationMenu.SetActive(true);
        }
    }
}
