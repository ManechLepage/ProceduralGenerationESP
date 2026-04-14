using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

public class DataToExcel : MonoBehaviour
{
    public AlgorithmRegistry algoData;
    public string savePath = "Assets/Data/GenData.xlsx";

    public void ExportToExcel()
    {
        List<string> algorithms = algoData.activeAlgorithms;

        StringBuilder rows = new StringBuilder();
        rows.Append(Row(1, "Index", "Algorithm Name"));
        for (int i = 0; i < algorithms.Count; i++)
            rows.Append(Row(i + 2, (i + 1).ToString(), algorithms[i]));

        string sheet = $@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<worksheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">
  <sheetData>{rows}</sheetData>
</worksheet>";

        string workbook = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main""
          xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">
  <sheets>
    <sheet name=""Algorithms"" sheetId=""1"" r:id=""rId1""/>
  </sheets>
</workbook>";

        string workbookRels = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1""
    Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet""
    Target=""worksheets/sheet1.xml""/>
</Relationships>";

        // THIS was missing - root level rels pointing to workbook
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

        Directory.CreateDirectory(Path.GetDirectoryName(savePath));

        if (File.Exists(savePath))
            File.Delete(savePath);

        using (var zip = ZipFile.Open(savePath, ZipArchiveMode.Create))
        {
            WriteEntry(zip, "[Content_Types].xml", contentTypes);
            WriteEntry(zip, "_rels/.rels", rootRels);           // <-- was missing
            WriteEntry(zip, "xl/_rels/workbook.xml.rels", workbookRels);
            WriteEntry(zip, "xl/workbook.xml", workbook);
            WriteEntry(zip, "xl/worksheets/sheet1.xml", sheet);
        }

        Debug.Log($"Exported {algorithms.Count} algorithms to {savePath}");
    }

    string Row(int rowIndex, string col1, string col2)
    {
        return $@"<row r=""{rowIndex}"">
      <c r=""A{rowIndex}"" t=""inlineStr""><is><t>{col1}</t></is></c>
      <c r=""B{rowIndex}"" t=""inlineStr""><is><t>{col2}</t></is></c>
    </row>";
    }

    void WriteEntry(ZipArchive zip, string path, string content)
    {
        var entry = zip.CreateEntry(path);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }
}