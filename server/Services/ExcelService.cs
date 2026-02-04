using System.Data;
using Categoriser.Api.Models;
using NPOI.SS.UserModel;

namespace Categoriser.Api.Services;

public sealed class ExcelService
{
    private static readonly string[] IsinHeaders = ["isin", "isincode", "isin code"];
    private static readonly string[] NameHeaders = ["name", "bezeichnung", "titel", "instrumentname", "instrument name"];
    private static readonly string[] WknHeaders = ["wkn", "wertpapierkennnummer"];

    public IReadOnlyList<UploadRow> ExtractRows(Stream stream, out List<string> errors)
    {
        errors = new List<string>();
        var table = ReadToDataTable(stream, errors);
        if (table.Rows.Count == 0)
        {
            return Array.Empty<UploadRow>();
        }

        var rows = new List<UploadRow>();
        foreach (DataRow row in table.Rows)
        {
            var isin = row["ISIN"]?.ToString()?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(isin))
            {
                continue;
            }

            var name = row.Table.Columns.Contains("Name") ? row["Name"]?.ToString()?.Trim() ?? string.Empty : string.Empty;
            var wkn = row.Table.Columns.Contains("WKN") ? row["WKN"]?.ToString()?.Trim() ?? string.Empty : string.Empty;
            rows.Add(new UploadRow(isin, name, wkn));
        }

        return rows;
    }

    private DataTable ReadToDataTable(Stream stream, List<string> errors)
    {
        var table = new DataTable();
        using var workbook = WorkbookFactory.Create(stream);
        var sheet = workbook.GetSheetAt(0);
        if (sheet is null)
        {
            errors.Add("Keine Tabellenblätter gefunden.");
            return table;
        }

        var headerRow = sheet.GetRow(sheet.FirstRowNum);
        if (headerRow is null)
        {
            errors.Add("Keine Header-Zeile gefunden.");
            return table;
        }

        var headerMap = new Dictionary<string, int>();
        for (var i = 0; i < headerRow.LastCellNum; i++)
        {
            var cell = headerRow.GetCell(i);
            var header = cell?.ToString();
            if (string.IsNullOrWhiteSpace(header))
            {
                continue;
            }

            headerMap[NormalizeHeader(header)] = i;
        }

        if (!TryFindColumn(headerMap, IsinHeaders, out var isinColumn))
        {
            errors.Add("ISIN-Spalte nicht gefunden.");
            return table;
        }

        _ = TryFindColumn(headerMap, NameHeaders, out var nameColumn);
        _ = TryFindColumn(headerMap, WknHeaders, out var wknColumn);

        table.Columns.Add("ISIN");
        if (nameColumn >= 0)
        {
            table.Columns.Add("Name");
        }

        if (wknColumn >= 0)
        {
            table.Columns.Add("WKN");
        }

        for (var rowIndex = sheet.FirstRowNum + 1; rowIndex <= sheet.LastRowNum; rowIndex++)
        {
            var row = sheet.GetRow(rowIndex);
            if (row is null)
            {
                continue;
            }

            var isin = row.GetCell(isinColumn)?.ToString()?.Trim() ?? string.Empty;
            var name = nameColumn >= 0 ? row.GetCell(nameColumn)?.ToString()?.Trim() ?? string.Empty : string.Empty;
            var wkn = wknColumn >= 0 ? row.GetCell(wknColumn)?.ToString()?.Trim() ?? string.Empty : string.Empty;

            if (string.IsNullOrWhiteSpace(isin) && string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(wkn))
            {
                continue;
            }

            var dataRow = table.NewRow();
            dataRow["ISIN"] = isin;
            if (table.Columns.Contains("Name"))
            {
                dataRow["Name"] = name;
            }

            if (table.Columns.Contains("WKN"))
            {
                dataRow["WKN"] = wkn;
            }

            table.Rows.Add(dataRow);
        }

        return table;
    }

    private static bool TryFindColumn(IReadOnlyDictionary<string, int> headerMap, string[] candidates, out int column)
    {
        foreach (var candidate in candidates)
        {
            if (headerMap.TryGetValue(NormalizeHeader(candidate), out column))
            {
                return true;
            }
        }

        column = -1;
        return false;
    }

    private static string NormalizeHeader(string header)
        => header.Replace(" ", string.Empty).Trim().ToLowerInvariant();
}
