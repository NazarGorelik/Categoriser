using System.Net.Http.Json;

namespace Categoriser.Api.Services;

public sealed class FinnhubService(HttpClient httpClient, IConfiguration configuration)
{
    public async Task<FinnhubProfile?> GetProfileAsync(string isin, CancellationToken cancellationToken)
    {
        var baseUrl = configuration["Finnhub:BaseUrl"] ?? "https://finnhub.io/api/v1";
        var secret = configuration["Finnhub:Secret"] ?? configuration["FINNHUB_SECRET"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            return null;
        }

        var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/stock/profile2?isin={Uri.EscapeDataString(isin)}");
        request.Headers.Add("X-Finnhub-Token", secret);
        var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var profile = await response.Content.ReadFromJsonAsync<FinnhubProfile>(cancellationToken: cancellationToken);
        if (profile is null || string.IsNullOrWhiteSpace(profile.Name))
        {
            return null;
        }

        return profile;
    }

    public async Task<FinnhubSearchResult?> SearchAsync(string query, CancellationToken cancellationToken)
    {
        var baseUrl = configuration["Finnhub:BaseUrl"] ?? "https://finnhub.io/api/v1";
        var secret = configuration["Finnhub:Secret"] ?? configuration["FINNHUB_SECRET"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            return null;
        }

        var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/search?q={Uri.EscapeDataString(query)}");
        request.Headers.Add("X-Finnhub-Token", secret);
        var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<FinnhubSearchResult>(cancellationToken: cancellationToken);
    }
}

public sealed record FinnhubProfile(
    string? Name,
    string? Ticker,
    string? Type,
    string? Description,
    string? AssetClass
);

public sealed record FinnhubSearchResult(
    int Count,
    List<FinnhubSearchItem> Result
);

public sealed record FinnhubSearchItem(
    string? Symbol,
    string? Description,
    string? DisplaySymbol,
    string? Type
);
