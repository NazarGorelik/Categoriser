using System.Data;
using Categoriser.Api.Models;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Categoriser.Api.Services;

public sealed class ExcelExportService
{
    public byte[] ExportSingleSheet(IEnumerable<JobRow> rows)
    {
        var table = BuildTable(rows);
        using var workbook = new XSSFWorkbook();
        WriteTableToSheet(workbook, "Kategorien", table);
        return Save(workbook);
    }

    public byte[] ExportByCategory(IEnumerable<JobRow> rows)
    {
        using var workbook = new XSSFWorkbook();
        foreach (var group in rows.GroupBy(row => row.Category))
        {
            var name = string.IsNullOrWhiteSpace(group.Key) ? "Unbekannt" : group.Key;
            var table = BuildTable(group);
            WriteTableToSheet(workbook, SanitizeSheetName(name), table);
        }

        return Save(workbook);
    }

    private static DataTable BuildTable(IEnumerable<JobRow> rows)
    {
        var table = new DataTable();
        table.Columns.Add("ISIN");
        table.Columns.Add("Name");
        table.Columns.Add("WKN");
        table.Columns.Add("Kategorie");
        table.Columns.Add("Unterkategorie");
        table.Columns.Add("Position");
        table.Columns.Add("Fehler");

        foreach (var row in rows)
        {
            var dataRow = table.NewRow();
            dataRow["ISIN"] = row.Isin;
            dataRow["Name"] = row.Name;
            dataRow["WKN"] = row.Wkn;
            dataRow["Kategorie"] = row.Category;
            dataRow["Unterkategorie"] = row.SubCategory;
            dataRow["Position"] = row.PositionType;
            dataRow["Fehler"] = row.Error;
            table.Rows.Add(dataRow);
        }

        return table;
    }

    private static void WriteTableToSheet(IWorkbook workbook, string sheetName, DataTable table)
    {
        var worksheet = workbook.CreateSheet(sheetName);
        var headerRow = worksheet.CreateRow(0);
        for (var col = 0; col < table.Columns.Count; col++)
        {
            headerRow.CreateCell(col).SetCellValue(table.Columns[col].ColumnName);
        }

        for (var rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
        {
            var worksheetRow = worksheet.CreateRow(rowIndex + 1);
            for (var col = 0; col < table.Columns.Count; col++)
            {
                worksheetRow.CreateCell(col).SetCellValue(table.Rows[rowIndex][col]?.ToString() ?? string.Empty);
            }
        }

        for (var col = 0; col < table.Columns.Count; col++)
        {
            worksheet.AutoSizeColumn(col);
        }
    }

    private static byte[] Save(IWorkbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.Write(stream, true);
        return stream.ToArray();
    }

    private static string SanitizeSheetName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = string.Concat(name.Where(ch => !invalid.Contains(ch)));
        return sanitized.Length > 31 ? sanitized[..31] : sanitized;
    }
}
