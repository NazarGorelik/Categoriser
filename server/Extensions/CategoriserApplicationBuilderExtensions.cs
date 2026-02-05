using Categoriser.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Categoriser.Api.Extensions;

public static class CategoriserApplicationBuilderExtensions
{
    public static IApplicationBuilder UseCategoriser(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.EnsureCreated();

        app.UseRouting();
        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });

        return app;
    }
}
