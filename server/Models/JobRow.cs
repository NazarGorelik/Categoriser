namespace Categoriser.Api.Models;

public sealed class JobRow
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public Job? Job { get; set; }

    public string Isin { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Wkn { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;
    public string SubCategory { get; set; } = string.Empty;
    public string PositionType { get; set; } = string.Empty;

    public string Error { get; set; } = string.Empty;
}
