namespace Categoriser.Api.Models;

public sealed class Job
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<JobRow> Rows { get; set; } = new List<JobRow>();
}
