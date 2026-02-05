using Categoriser.Api.Data;
using Categoriser.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Categoriser.Api.Extensions;

public static class CategoriserServiceCollectionExtensions
{
    public static IServiceCollection AddCategoriser(this IServiceCollection services, IConfiguration configuration, string? connectionString = null)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            var configString = configuration.GetConnectionString("DefaultConnection");
            options.UseSqlite(connectionString ?? configString ?? "Data Source=categoriser.db");
        });

        services.AddHttpClient("Finnhub");
        services.TryAddSingleton<FinnhubService>();
        services.TryAddSingleton<ExcelService>();
        services.TryAddSingleton<CategorizationService>();
        services.TryAddSingleton<ExcelExportService>();

        services.ConfigureCategoriserOptions(configuration);
        services.AddControllers();

        return services;
    }
}
