using UnityEngine;
using System.IO;
using System.Text;

public class DataToCSV : MonoBehaviour
{

     private string filePath = Application.persistentDataPath + "/GenData.csv";

     public void WriteDataToCSV()
    {
        // Example data structure (e.g., from a list or array)
        string[] columnHeaders = { "Algo:", "nom algo" };
        string[] dataRow1 = { "Temps prevu:", "sec" };
        string[] dataRow2 = { "Temps gen", "temps" };

        StringBuilder csvContent = new StringBuilder();

        // Append headers
        csvContent.AppendLine(string.Join(",", columnHeaders));

        // Append data rows
        csvContent.AppendLine(string.Join(",", dataRow1));
        csvContent.AppendLine(string.Join(",", dataRow2));

        // Write the string to the file
        try
        {
            File.WriteAllText(filePath, csvContent.ToString());
            Debug.Log("CSV file successfully written to: " + filePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error writing CSV file: " + e.Message);
        }
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
