using Categoriser.Api.Models;
using ClosedXML.Excel;

namespace Categoriser.Api.Services;

public sealed class ExcelExportService
{
    public byte[] ExportSingleSheet(IEnumerable<JobRow> rows)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Kategorien");
        WriteHeaders(worksheet);
        WriteRows(worksheet, rows);
        return Save(workbook);
    }

    public byte[] ExportByCategory(IEnumerable<JobRow> rows)
    {
        using var workbook = new XLWorkbook();
        var grouped = rows.GroupBy(row => row.Category);
        foreach (var group in grouped)
        {
            var name = string.IsNullOrWhiteSpace(group.Key) ? "Unbekannt" : group.Key;
            var worksheet = workbook.AddWorksheet(SanitizeSheetName(name));
            WriteHeaders(worksheet);
            WriteRows(worksheet, group);
        }

        return Save(workbook);
    }

    private static void WriteHeaders(IXLWorksheet worksheet)
    {
        worksheet.Cell(1, 1).Value = "ISIN";
        worksheet.Cell(1, 2).Value = "Name";
        worksheet.Cell(1, 3).Value = "WKN";
        worksheet.Cell(1, 4).Value = "Kategorie";
        worksheet.Cell(1, 5).Value = "Unterkategorie";
        worksheet.Cell(1, 6).Value = "Position";
        worksheet.Cell(1, 7).Value = "Fehler";
    }

    private static void WriteRows(IXLWorksheet worksheet, IEnumerable<JobRow> rows)
    {
        var rowIndex = 2;
        foreach (var row in rows)
        {
            worksheet.Cell(rowIndex, 1).Value = row.Isin;
            worksheet.Cell(rowIndex, 2).Value = row.Name;
            worksheet.Cell(rowIndex, 3).Value = row.Wkn;
            worksheet.Cell(rowIndex, 4).Value = row.Category;
            worksheet.Cell(rowIndex, 5).Value = row.SubCategory;
            worksheet.Cell(rowIndex, 6).Value = row.PositionType;
            worksheet.Cell(rowIndex, 7).Value = row.Error;
            rowIndex++;
        }

        worksheet.Columns().AdjustToContents();
    }

    private static byte[] Save(XLWorkbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string SanitizeSheetName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = string.Concat(name.Where(ch => !invalid.Contains(ch)));
        return sanitized.Length > 31 ? sanitized[..31] : sanitized;
    }
}
