using Categoriser.Api.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Categoriser.Api.Extensions;

public static class CategoriserOptionsExtensions
{
    public static IServiceCollection ConfigureCategoriserOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<UploadOptions>(configuration.GetSection("Upload"));
        return services;
    }
}
