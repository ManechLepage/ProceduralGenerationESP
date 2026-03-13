using UnityEngine;

public class FBMNode : NodeBehaviour
{
    public override void Start()
    {
        base.Start();

        // ConnectorBehaviour offsetConnector = GetInputConnection("offset");
        // Vector2 value = offsetConnector.GetInputBehaviour().GetVariant().GetValue<Vector2>();
        // Debug.Log("Offset: " + value);
    }
}
