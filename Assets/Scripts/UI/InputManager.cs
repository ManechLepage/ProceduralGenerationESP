using UnityEngine;

public class InputManager : MonoBehaviour
{
    public GameObject nodeCreationMenu;

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
    }
}
