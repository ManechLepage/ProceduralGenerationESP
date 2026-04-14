using UnityEngine;
using DG.Tweening;

public class InputManager : MonoBehaviour
{
    public GameObject nodeCreationMenu;
    public GameObject canvas;
    public GameObject nodeGUI;

    void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.openedUI = true;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && !(GameManager.Instance.openedUI && !nodeGUI.activeSelf))
        {
            if (nodeGUI.activeSelf)
            {
                HideCanvas();
            }
            else
            {
                ShowCanvas();
            }

            if (GameManager.Instance != null)
            {
                if (nodeGUI.activeSelf)
                    GameManager.Instance.DidOpenUI();
                else
                    GameManager.Instance.DidCloseUI();
            }
            
            if (!nodeGUI.activeSelf)
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

    private void ShowCanvas()
    {
        nodeGUI.SetActive(true);
    }

    private void HideCanvas()
    {
        nodeGUI.SetActive(false);
    }
}
