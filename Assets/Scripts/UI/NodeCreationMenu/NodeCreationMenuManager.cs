using UnityEngine;

public class NodeCreationMenuManager : MonoBehaviour
{
    public GameObject categoryMenus;
    public GameObject nodeCreationMenu;

    public void CloseAllMenus()
    {
        foreach (Transform child in categoryMenus.transform)
        {
            child.gameObject.SetActive(false);
        }
    }

    public void OpenMenu()
    {
        nodeCreationMenu.SetActive(true);
    }

    public void CloseMenu()
    {
        nodeCreationMenu.SetActive(false);
    }
}
