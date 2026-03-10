using UnityEngine;
using UnityEngine.EventSystems;

public class EarlyButtonClick : MonoBehaviour, IPointerDownHandler
{
    private ConnectorBehaviour connector;
    public void OnPointerDown(PointerEventData eventData)
    {
        GetComponent<ConnectorBehaviour>().StartConnection();
    }
}