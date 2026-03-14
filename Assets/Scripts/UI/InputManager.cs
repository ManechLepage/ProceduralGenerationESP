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
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            canvas.SetActive(!canvas.activeSelf);

            if (GameManager.Instance != null)
                GameManager.Instance.openedUI = canvas.activeSelf;
        }
    }
}
