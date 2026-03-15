using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class MasterNode : NodeBehaviour
{
    public UnityEvent onFire;

    public override Variant Fire()
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
        Fire();
    }
}
