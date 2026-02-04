using Categoriser.Api.Data;
using Categoriser.Api.Models;
using Categoriser.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlite(connectionString);
});

builder.Services.AddHttpClient<FinnhubService>();
builder.Services.AddScoped<ExcelService>();
builder.Services.AddScoped<CategorizationService>();
builder.Services.AddScoped<ExcelExportService>();

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}

app.MapPost("/api/upload", async (HttpRequest request, AppDbContext dbContext, ExcelService excelService) =>
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest("Erwarte multipart/form-data.");
    }

    var form = await request.ReadFormAsync();
    var file = form.Files.GetFile("file");
    if (file is null)
    {
        return Results.BadRequest("Datei fehlt.");
    }

    var maxMb = request.HttpContext.RequestServices
        .GetRequiredService<IConfiguration>()
        .GetValue<int>("Upload:MaxUploadMb", 10);

    if (file.Length > maxMb * 1024L * 1024L)
    {
        return Results.BadRequest($"Datei ist größer als {maxMb} MB.");
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
    await dbContext.SaveChangesAsync();

    var response = new UploadResponse(job.Id, rows, errors);
    return Results.Ok(response);
});

app.MapPost("/api/check", async (
    CheckRequest request,
    AppDbContext dbContext,
    FinnhubService finnhubService,
    CategorizationService categorizationService,
    CancellationToken cancellationToken) =>
{
    var job = await dbContext.Jobs
        .Include(j => j.Rows)
        .FirstOrDefaultAsync(j => j.Id == request.JobId, cancellationToken);

    if (job is null)
    {
        return Results.NotFound("Job nicht gefunden.");
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

    var response = new CheckResponse(summary, responseRows, errors);
    return Results.Ok(response);
});

app.MapGet("/api/download", async (
    Guid jobId,
    string mode,
    AppDbContext dbContext,
    ExcelExportService exportService,
    CancellationToken cancellationToken) =>
{
    var job = await dbContext.Jobs
        .Include(j => j.Rows)
        .FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);

    if (job is null)
    {
        return Results.NotFound("Job nicht gefunden.");
    }

    var bytes = mode == "sheetsByCategory"
        ? exportService.ExportByCategory(job.Rows)
        : exportService.ExportSingleSheet(job.Rows);

    var filename = mode == "sheetsByCategory" ? "Kategorien_Sheets.xlsx" : "Kategorien.xlsx";
    return Results.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
});

app.Run();

static bool IsValidIsin(string isin)
    => System.Text.RegularExpressions.Regex.IsMatch(isin, "^[A-Z]{2}[A-Z0-9]{10}$");

static void Increment(Dictionary<string, int> summary, string key)
{
    if (!summary.TryAdd(key, 1))
    {
        summary[key] += 1;
    }
}
