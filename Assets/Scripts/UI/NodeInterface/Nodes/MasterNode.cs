using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class MasterNode : NodeBehaviour
{
    public UnityEvent onFire;
    public UnityEvent onInputUpdated;

    public override Variant OnFire()
    {
        if (!GetInputConnection("heightmap").IsConnected())
        {
            Debug.Log("MasterNode: Heightmap input not connected!");
            return new Variant();
        }

        onFire.Invoke();
        return new Variant();
    }

    public void ButtonFire()
    {
        Fire(onlyIfModified: false);
    }

    public override void InputUpdated(ConnectorBehaviour connector)
    {
        base.InputUpdated(connector);
        onInputUpdated.Invoke();
    }
}
