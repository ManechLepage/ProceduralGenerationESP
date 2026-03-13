using UnityEngine;

public class NodeButton : MonoBehaviour
{
    public GameObject nodePrefab;
    private GameObject nodeManager;
    private NodeCreationMenuManager nodeCreationMenu;

    void Start()
    {
        nodeManager = GameObject.Find("NodeHandler");
        nodeCreationMenu = GameObject.Find("NodeCreationMenu").GetComponent<NodeCreationMenuManager>();
    }

    public void ClickedNode()
    {
        GameObject newNode = Instantiate(nodePrefab, Vector3.zero, Quaternion.identity);
        newNode.transform.SetParent(nodeManager.transform);
        newNode.transform.position = Input.mousePosition;
        nodeCreationMenu.CloseMenu();
    }
}
