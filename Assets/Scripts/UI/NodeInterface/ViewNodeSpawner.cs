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
                if (!connector.IsConnected())
                    AddConnectedViewNode();
            }
        }
    }

    public void AddConnectedViewNode()
    {
        Vector2 nodeBottomRight = new Vector2(transform.position.x + GetComponent<RectTransform>().rect.width, transform.position.y);
        Vector2 spawnPosition = nodeBottomRight + new Vector2(125, -50) * GraphManager.Instance.currentZoom;

        GameObject newNode = GraphManager.Instance.CreateNode(viewNodePrefab, spawnPosition);
        NodeBehaviour nodeBehaviour = newNode.GetComponent<NodeBehaviour>();

        ConnectorBehaviour newConnector = nodeBehaviour.GetInputConnection("preview");
        newConnector.node = nodeBehaviour;

        GraphManager.Instance.LinkConnections(connector, newConnector);

        /*GameObject currentLine = connector.CreateLineFromConnection();
        connector.connectionLines.Add(currentLine);
        newConnector.connectionLines.Add(currentLine);

        connector.multipleConnectedTo.Add(newConnector);
        newConnector.multipleConnectedTo.Add(connector);

        currentLine.GetComponent<LineManager>().output = newConnector;
        currentLine.GetComponent<LineManager>().isLinked = true;

        newConnector.InputUpdated();*/
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
