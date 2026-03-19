using UnityEngine;

public class ViewNodeSpawner : MonoBehaviour
{
    public GameObject viewNodePrefab;

    private ConnectorBehaviour connector;

    void Awake()
    {
        connector = GetComponent<ConnectorBehaviour>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(2))
        {
            if (connector.MouseInConnector(Input.mousePosition, connector))
            {
                ClickedConnection();
            }
        }
    }

    public void ClickedConnection()
    {
        if (!connector.IsConnected())
        {
            AddConnectedViewNode();
        }
    }

    public void AddConnectedViewNode()
    {
        Vector2 nodeBottomRight = new Vector2(transform.position.x + GetComponent<RectTransform>().rect.width, transform.position.y);
        Vector2 spawnPosition = nodeBottomRight + new Vector2(175, -100) * GraphManager.Instance.currentZoom;

        GameObject newNode = SpawnViewNode(spawnPosition);
        NodeBehaviour nodeBehaviour = newNode.GetComponent<NodeBehaviour>();

        ConnectorBehaviour newConnector = nodeBehaviour.GetInputConnection("preview");

        connector.currentLine = connector.CreateLineFromConnection();
        newConnector.currentLine = connector.currentLine;

        connector.connectedTo = newConnector;
        newConnector.connectedTo = connector;

        connector.currentLine.GetComponent<LineManager>().output = newConnector;
        connector.currentLine.GetComponent<LineManager>().isLinked = true;

        newConnector.node = nodeBehaviour;
        newConnector.InputUpdated();
    }

    public GameObject SpawnViewNode(Vector2 position)
    {
        if (viewNodePrefab == null)
        {
            Debug.Log($"ViewNode prefab not assigned");
            return null;
        }

        GameObject newNode = Instantiate(viewNodePrefab, Vector3.zero, Quaternion.identity);
        newNode.transform.SetParent(GraphManager.Instance.nodeParent.transform);
        newNode.transform.position = position;
        newNode.transform.localScale *= GraphManager.Instance.currentZoom * 1.4f;

        GraphManager.Instance.nodes.Add(newNode.GetComponent<NodeBehaviour>());
        return newNode;
    }
}
