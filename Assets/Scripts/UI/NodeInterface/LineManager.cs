using UnityEngine;

public class LineManager : MonoBehaviour
{
    public bool isLinked;
    public ConnectorBehaviour input;
    public ConnectorBehaviour output;

    void Update()
    {
        Vector3 startPos = input.transform.position;
        if (isLinked)
        {
            Vector3 endPos = output.transform.position;
            
        }
        else
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            
        }
    }
}
