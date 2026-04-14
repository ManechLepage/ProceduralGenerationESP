using UnityEngine;

public class StatisticsReader : MonoBehaviour
{
    public GenerationStatistics statistics;

    public float GetOverallTotalTime()
    {
        return statistics.GetTotalTime();
    }

    public float GetTotalTimeForType(NodeTimeType type)
    {
        return statistics.GetTotalTime(type);
    }

    public void PrintAllStatistics()
    {
        foreach (var statType in statistics.stats)
        {
            Debug.Log($"--- Type: {statType.type} | Type Total: {statistics.GetTotalTime(statType.type)}s ---");
            foreach (var stat in statType.statistics)
                Debug.Log($"  {stat.name}: {stat.value}s");
        }
        Debug.Log($"Overall Total: {statistics.GetTotalTime()}s");
    }

    public string GetStatisticsSummary()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (var statType in statistics.stats)
        {
            sb.AppendLine($"[{statType.type}] Total: {statistics.GetTotalTime(statType.type)}s");
            foreach (var stat in statType.statistics)
                sb.AppendLine($"  - {stat.name}: {stat.value}s");
        }
        sb.AppendLine($"Overall Total: {statistics.GetTotalTime()}s");
        return sb.ToString();
    }
}