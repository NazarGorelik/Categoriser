namespace Categoriser.Api.Models;

public sealed record UploadRow(string Isin, string Name, string Wkn);

public sealed record UploadResponse(Guid JobId, IReadOnlyList<UploadRow> Rows, IReadOnlyList<string> Errors);

public sealed record CheckRequest(Guid JobId);

public sealed record CategorizedRow(
    string Isin,
    string Name,
    string Wkn,
    string Category,
    string SubCategory,
    string PositionType,
    string Error
);

public sealed record CheckResponse(
    IReadOnlyDictionary<string, int> Summary,
    IReadOnlyList<CategorizedRow> Rows,
    IReadOnlyList<string> Errors
);
