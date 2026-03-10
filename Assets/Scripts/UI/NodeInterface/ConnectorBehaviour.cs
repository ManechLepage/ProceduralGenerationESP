using UnityEngine;

public class ConnectorBehaviour : MonoBehaviour
{
   public Type type;
   public GameObject linePrefab;

   public void StartConnection()
    {
        GameObject line = Instantiate(linePrefab, transform.position, Quaternion.identity);
        line.GetComponent<LineManager>().isLinked = false;
        line.GetComponent<LineManager>().input = this;
        line.transform.SetParent(GameObject.FindWithTag("LineHandler").transform);
    }
}

public enum Type{
    Input,
    Output
}