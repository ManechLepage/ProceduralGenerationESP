using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Threading.Tasks;

[System.Serializable]
public class ConnectorEvent : UnityEngine.Events.UnityEvent<ConnectorBehaviour> { }

public class MasterNode : NodeBehaviour
{
    public UnityEvent onFire;
    public ConnectorEvent onInputUpdated;

    public override Task<Variant> OnFire()
    {
        if (!GetInputConnection("heightmap").IsConnected())
        {
            Debug.Log("MasterNode: Heightmap input not connected!");
            return Task.FromResult(new Variant());
        }

        onFire.Invoke();
        return Task.FromResult(new Variant());
    }

    async public void ButtonFire()
    {
        await Fire(onlyIfModified: false);
    }

    public override void InputUpdated(ConnectorBehaviour connector)
    {
        base.InputUpdated(connector);
        onInputUpdated.Invoke(connector);
    }

    async public Task<GenerationStatistics> GetPredictedStatistics()
    {
        GenerationStatistics predictedStats = new GenerationStatistics();
        await PredictTimeForNode(this, predictedStats);
        return predictedStats;
    }

    async Task PredictTimeForNode(NodeBehaviour node, GenerationStatistics accumulatedStats)
    {
        float nodeTime = await node.GetPredictedTime();
        NodeTimeType nodeType = node.nodeTimeType;
        string nodeName = node.prefabName;

        accumulatedStats.AddTime(nodeType, nodeName, nodeTime);

        // Faire la boucle dans les connections pour calculer le temps total.
        foreach (ConnectorBehaviour input in node.inputConnections)
        {
            if (input.IsConnected())
            {
                NodeBehaviour connectedNode = input.multipleConnectedTo[0].node;
                await PredictTimeForNode(connectedNode, accumulatedStats);
            }
        }
    }
}
