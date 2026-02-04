using Categoriser.Api.Models;
using ClosedXML.Excel;

namespace Categoriser.Api.Services;

public sealed class ExcelService
{
    private static readonly string[] IsinHeaders = ["isin", "isincode", "isin code"];
    private static readonly string[] NameHeaders = ["name", "bezeichnung", "titel", "instrumentname", "instrument name"];
    private static readonly string[] WknHeaders = ["wkn", "wertpapierkennnummer"]; 

    public IReadOnlyList<UploadRow> ExtractRows(Stream stream, out List<string> errors)
    {
        errors = new List<string>();
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet is null)
        {
            errors.Add("Keine Tabellenblätter gefunden.");
            return Array.Empty<UploadRow>();
        }

        var headerRow = worksheet.FirstRowUsed();
        if (headerRow is null)
        {
            errors.Add("Keine Header-Zeile gefunden.");
            return Array.Empty<UploadRow>();
        }

        var headerMap = headerRow.Cells()
            .Where(cell => !cell.IsEmpty())
            .ToDictionary(cell => NormalizeHeader(cell.GetString()), cell => cell.Address.ColumnNumber);

        if (!TryFindColumn(headerMap, IsinHeaders, out var isinColumn))
        {
            errors.Add("ISIN-Spalte nicht gefunden.");
            return Array.Empty<UploadRow>();
        }

        _ = TryFindColumn(headerMap, NameHeaders, out var nameColumn);
        _ = TryFindColumn(headerMap, WknHeaders, out var wknColumn);

        var rows = new List<UploadRow>();
        foreach (var row in headerRow.RowBelow().RowsUsed())
        {
            var isin = row.Cell(isinColumn).GetString().Trim();
            if (string.IsNullOrWhiteSpace(isin))
            {
                continue;
            }

            var name = nameColumn > 0 ? row.Cell(nameColumn).GetString().Trim() : string.Empty;
            var wkn = wknColumn > 0 ? row.Cell(wknColumn).GetString().Trim() : string.Empty;

            rows.Add(new UploadRow(isin, name, wkn));
        }

        return rows;
    }

    private static bool TryFindColumn(Dictionary<string, int> headerMap, string[] candidates, out int column)
    {
        foreach (var candidate in candidates)
        {
            if (headerMap.TryGetValue(NormalizeHeader(candidate), out column))
            {
                return true;
            }
        }

        column = 0;
        return false;
    }

    private static string NormalizeHeader(string header)
        => header.Replace(" ", string.Empty).Trim().ToLowerInvariant();
}
