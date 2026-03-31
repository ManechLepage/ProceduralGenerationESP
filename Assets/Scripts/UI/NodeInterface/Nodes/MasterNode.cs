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

        if (connector != null && connector.connectionName == "water")
        {
            bool hasWater = GetInputValue("water").GetValue<bool>();
            TerrainManager.Instance.SetActiveSea(hasWater);
        }
    }

    public GenerationStatistics GetPredictedStatistics()
    {
        GenerationStatistics predictedStats = new GenerationStatistics { terrainTime = 0f, erosionTime = 0f };
        PredictTimeForNode(this, predictedStats);
        return predictedStats;
    }

    void PredictTimeForNode(NodeBehaviour node, GenerationStatistics accumulatedStats)
    {
        float nodeTime = node.GetPredictedTime();

        switch (node.nodeTimeType)
        {
            case NodeTimeType.Terrain:
                accumulatedStats.terrainTime += nodeTime;
                break;
            case NodeTimeType.Erosion:
                accumulatedStats.erosionTime += nodeTime;
                break;
            default:
                break;
        }

        // Faire la boucle dans les connections pour calculer le temps total.
        foreach (ConnectorBehaviour input in node.inputConnections)
        {
            if (input.IsConnected())
            {
                NodeBehaviour connectedNode = input.multipleConnectedTo[0].node;
                PredictTimeForNode(connectedNode, accumulatedStats);
            }
        }
    }
}
