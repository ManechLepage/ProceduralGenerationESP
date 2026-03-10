using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class DataToExcel : MonoBehaviour
{
    public AlgorithmRegistry algoData;
    public string savePath = "Assets/Data/algorithms.csv";

    public void ExportToCSV()
    {
        List<string> algorithms = algoData.GetAlgorithmList();

        StringBuilder csv = new StringBuilder();

        // Header
        csv.AppendLine("Index,Algorithm Name");

        // Rows
        for (int i = 0; i < algorithms.Count; i++)
            csv.AppendLine($"{i + 1},{algorithms[i]}");

        File.WriteAllText(savePath, csv.ToString());
        Debug.Log($"Exported {algorithms.Count} algorithms to {savePath}");
    }
}