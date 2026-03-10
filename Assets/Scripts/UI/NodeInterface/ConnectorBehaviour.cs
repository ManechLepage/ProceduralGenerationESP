using UnityEngine;

public class ConnectorBehaviour : MonoBehaviour
{
   public Type type;
   public GameObject linePrefab;

   public void StartConnection()
    {
        
    }
}

public enum Type{
    Input,
    Output
}