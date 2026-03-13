using UnityEngine;

public class CategoryButton : MonoBehaviour
{
    public GameObject categoryMenu;
    [HideInInspector] public NodeCreationMenuManager menuManager;
    void Start()
    {
        Initialize();
    }
    private void Initialize()
    {
        categoryMenu.SetActive(false);
        menuManager = GetComponentInParent<NodeCreationMenuManager>();
    }

    public void ClickedCategory()
    {
        menuManager.CloseAllMenus();
        categoryMenu.SetActive(true);
    }
}
