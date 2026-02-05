using System.Net.Http.Headers;
using System.Text.Json;

var isin = "JE00B78NPY84";
var baseUrl = Environment.GetEnvironmentVariable("FINNHUB_BASE_URL") ?? "https://finnhub.io/api/v1";
var token = Environment.GetEnvironmentVariable("FINNHUB_SECRET");

if (string.IsNullOrWhiteSpace(token))
{
    Console.Error.WriteLine("FINNHUB_SECRET is not set.");
    return;
}

using var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
httpClient.DefaultRequestHeaders.Add("X-Finnhub-Token", token);

var profileUrl = $"{baseUrl}/stock/profile2?isin={Uri.EscapeDataString(isin)}";
var response = await httpClient.GetAsync(profileUrl);
if (!response.IsSuccessStatusCode)
{
    Console.Error.WriteLine($"Request failed: {(int)response.StatusCode} {response.ReasonPhrase}");
    var errorBody = await response.Content.ReadAsStringAsync();
    Console.Error.WriteLine(errorBody);
    return;
}

var json = await response.Content.ReadAsStringAsync();
using var doc = JsonDocument.Parse(json);
Console.WriteLine(doc.RootElement.ToString());
