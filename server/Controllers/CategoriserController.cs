using Categoriser.Api.Data;
using Categoriser.Api.Models;
using Categoriser.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Categoriser.Api.Controllers;

[Tags("Categoriser")]
[Route("api")]
[ApiController]
public sealed class CategoriserController(
    AppDbContext dbContext,
    ExcelService excelService,
    FinnhubService finnhubService,
    CategorizationService categorizationService,
    ExcelExportService exportService,
    IOptions<UploadOptions> uploadOptions) : ControllerBase
{
    [HttpPost("upload")]
    public async Task<ActionResult<UploadResponse>> Upload([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return BadRequest("Datei fehlt.");
        }

        var maxBytes = uploadOptions.Value.MaxUploadMb * 1024L * 1024L;
        if (file.Length > maxBytes)
        {
            return BadRequest($"Datei ist größer als {uploadOptions.Value.MaxUploadMb} MB.");
        }

        await using var stream = file.OpenReadStream();
        var rows = excelService.ExtractRows(stream, out var errors);

        var job = new Job { Id = Guid.NewGuid() };
        foreach (var row in rows)
        {
            var normalizedIsin = row.Isin.Trim().ToUpperInvariant();
            var error = IsValidIsin(normalizedIsin) ? string.Empty : "Ungültige ISIN";
            job.Rows.Add(new JobRow
            {
                Id = Guid.NewGuid(),
                JobId = job.Id,
                Isin = normalizedIsin,
                Name = row.Name,
                Wkn = row.Wkn,
                Error = error
            });
        }

        dbContext.Jobs.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new UploadResponse(job.Id, rows, errors));
    }

    [HttpPost("check")]
    public async Task<ActionResult<CheckResponse>> Check([FromBody] CheckRequest request, CancellationToken cancellationToken)
    {
        var job = await dbContext.Jobs
            .Include(j => j.Rows)
            .FirstOrDefaultAsync(j => j.Id == request.JobId, cancellationToken);

        if (job is null)
        {
            return NotFound("Job nicht gefunden.");
        }

        var errors = new List<string>();
        var summary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in job.Rows)
        {
            if (!string.IsNullOrWhiteSpace(row.Error))
            {
                Increment(summary, "Unbekannt/Fehler");
                continue;
            }

            var profile = await finnhubService.GetProfileAsync(row.Isin, cancellationToken);
            if (profile is null)
            {
                var search = await finnhubService.SearchAsync(row.Isin, cancellationToken);
                var match = search?.Result?.FirstOrDefault();
                if (match is not null)
                {
                    profile = new FinnhubProfile(match.Description, match.Symbol, match.Type, match.Description, null);
                }
            }

            if (profile is null)
            {
                row.Category = "Unbekannt/Fehler";
                row.Error = "Finnhub-Daten nicht gefunden";
                Increment(summary, row.Category);
                continue;
            }

            var result = categorizationService.Categorize(profile, row.Name);
            row.Category = result.Category;
            row.SubCategory = result.SubCategory;
            row.PositionType = result.PositionType;
            Increment(summary, result.Category);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var responseRows = job.Rows.Select(row => new CategorizedRow(
            row.Isin,
            row.Name,
            row.Wkn,
            row.Category,
            row.SubCategory,
            row.PositionType,
            row.Error
        )).ToList();

        return Ok(new CheckResponse(summary, responseRows, errors));
    }

    [HttpGet("download")]
    [ProducesResponseType<FileResult>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Download([FromQuery] Guid jobId, [FromQuery] string mode, CancellationToken cancellationToken)
    {
        var job = await dbContext.Jobs
            .Include(j => j.Rows)
            .FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);

        if (job is null)
        {
            return NotFound("Job nicht gefunden.");
        }

        var bytes = mode == "sheetsByCategory"
            ? exportService.ExportByCategory(job.Rows)
            : exportService.ExportSingleSheet(job.Rows);

        var filename = mode == "sheetsByCategory" ? "Kategorien_Sheets.xlsx" : "Kategorien.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
    }

    private static bool IsValidIsin(string isin)
        => System.Text.RegularExpressions.Regex.IsMatch(isin, "^[A-Z]{2}[A-Z0-9]{10}$");

    private static void Increment(IDictionary<string, int> summary, string key)
    {
        if (!summary.TryAdd(key, 1))
        {
            summary[key] += 1;
        }
    }
}
