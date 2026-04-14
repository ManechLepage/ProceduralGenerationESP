using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

// ExportAll() pour tt exporter

public class DataToExcel : MonoBehaviour
{
    public AlgorithmRegistry algoData;
    private StatisticsReader statisticsReader;
    public string savePathExcel = "Assets/Data/statistics.xlsx";
    public string savePathCSV = "Assets/Data/statistics.csv";

//trouver les stats deja stoquées

    void Awake()
    {
        statisticsReader = Object.FindFirstObjectByType<StatisticsReader>();
        if (statisticsReader == null)
            Debug.LogWarning("DataToExcel: No StatisticsReader found in scene.");
    }

    public void ExportAll()
    {
        ExportToExcel();
        ExportToCSV();
    }

//fonction pour exporter en csv

    public void ExportToCSV()
    {
        if (statisticsReader == null) return;

        StringBuilder csv = new StringBuilder();
        csv.AppendLine("Type,Name,Value,Type Total,Overall Total");

        float overallTotal = statisticsReader.GetOverallTotalTime();

        foreach (var statType in statisticsReader.statistics.stats)
        {
            float typeTotal = statisticsReader.GetTotalTimeForType(statType.type);
            foreach (var stat in statType.statistics)
                csv.AppendLine($"{statType.type},{stat.name},{stat.value},{typeTotal},{overallTotal}");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(savePathCSV));
        File.WriteAllText(savePathCSV, csv.ToString());
        Debug.Log($"CSV exported to {savePathCSV}");
    }

//fonction por exporter en exel

    public void ExportToExcel()
    {
        if (statisticsReader == null) return;

        float overallTotal = statisticsReader.GetOverallTotalTime();
        StringBuilder rows = new StringBuilder();
        int rowIndex = 1;

        rows.Append(Row(rowIndex++, "Type", "Name", "Value", "Type Total", "Overall Total"));

        foreach (var statType in statisticsReader.statistics.stats)
        {
            float typeTotal = statisticsReader.GetTotalTimeForType(statType.type);
            foreach (var stat in statType.statistics)
                rows.Append(Row(rowIndex++,
                    statType.type.ToString(),
                    stat.name,
                    stat.value.ToString("F4"),
                    typeTotal.ToString("F4"),
                    overallTotal.ToString("F4")
                ));
        }

        string sheet = $@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<worksheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">
  <sheetData>{rows}</sheetData>
</worksheet>";

        string workbook = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main""
          xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">
  <sheets>
    <sheet name=""Statistics"" sheetId=""1"" r:id=""rId1""/>
  </sheets>
</workbook>";

        string workbookRels = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1""
    Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet""
    Target=""worksheets/sheet1.xml""/>
</Relationships>";

        string rootRels = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1""
    Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument""
    Target=""xl/workbook.xml""/>
</Relationships>";

        string contentTypes = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">
  <Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>
  <Default Extension=""xml"" ContentType=""application/xml""/>
  <Override PartName=""/xl/workbook.xml""
    ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml""/>
  <Override PartName=""/xl/worksheets/sheet1.xml""
    ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml""/>
</Types>";

        Directory.CreateDirectory(Path.GetDirectoryName(savePathExcel));
        if (File.Exists(savePathExcel)) File.Delete(savePathExcel);

        using (var zip = ZipFile.Open(savePathExcel, ZipArchiveMode.Create))
        {
            WriteEntry(zip, "[Content_Types].xml", contentTypes);
            WriteEntry(zip, "_rels/.rels", rootRels);
            WriteEntry(zip, "xl/_rels/workbook.xml.rels", workbookRels);
            WriteEntry(zip, "xl/workbook.xml", workbook);
            WriteEntry(zip, "xl/worksheets/sheet1.xml", sheet);
        }

        Debug.Log($"Excel exported to {savePathExcel}");
    }

    string Row(int rowIndex, params string[] values)
    {
        string[] cols = { "A", "B", "C", "D", "E", "F" };
        StringBuilder cells = new StringBuilder();
        for (int i = 0; i < values.Length; i++)
            cells.Append($@"<c r=""{cols[i]}{rowIndex}"" t=""inlineStr""><is><t>{values[i]}</t></is></c>");
        return $@"<row r=""{rowIndex}"">{cells}</row>";
    }

    void WriteEntry(ZipArchive zip, string path, string content)
    {
        var entry = zip.CreateEntry(path);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }
}