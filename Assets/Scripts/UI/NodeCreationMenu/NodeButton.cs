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
        if (nodePrefab == null)
        {
            Debug.Log($"Node prefab not assigned");
            return;
        }

        GameObject newNode = Instantiate(nodePrefab, Vector3.zero, Quaternion.identity);
        newNode.transform.SetParent(nodeManager.transform);
        newNode.transform.position = Input.mousePosition;
        newNode.transform.localScale *= GraphManager.Instance.currentZoom * 1.4f;

        GraphManager.Instance.nodes.Add(newNode.GetComponent<NodeBehaviour>());
        
        // The close menu is overwritten by the InputManager's click detection
        nodeCreationMenu.CloseMenu();
    }
}
