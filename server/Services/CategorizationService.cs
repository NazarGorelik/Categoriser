using System.Text.RegularExpressions;

namespace Categoriser.Api.Services;

public sealed class CategorizationService
{
    public CategorizationResult Categorize(FinnhubProfile? profile, string fallbackName)
    {
        var text = string.Join(" ", new[]
        {
            profile?.Name,
            profile?.Description,
            profile?.Type,
            profile?.AssetClass,
            fallbackName
        }.Where(value => !string.IsNullOrWhiteSpace(value))).ToLowerInvariant();

        var category = "Aktie";
        var subCategory = string.Empty;
        var position = string.Empty;

        if (IsCommodity(text))
        {
            category = "Rohstoff";
            subCategory = CommoditySubCategory(text);
        }
        else if (text.Contains("etf") || text.Contains("etp") || text.Contains("exchange traded"))
        {
            category = "ETP";
        }

        if (Regex.IsMatch(text, "\\bshort\\b|\\bbear\\b"))
        {
            position = "Short";
        }
        else if (Regex.IsMatch(text, "\\blong\\b|\\bbull\\b"))
        {
            position = "Long";
        }

        return new CategorizationResult(category, subCategory, position);
    }

    private static bool IsCommodity(string text)
    {
        var keywords = new[]
        {
            "commodity", "rohstoff", "gold", "silver", "silber", "platinum", "platin", "oil", "crude",
            "brent", "wti", "copper", "kupfer", "nickel", "zinc", "aluminum", "aluminium"
        };

        return keywords.Any(keyword => text.Contains(keyword));
    }

    private static string CommoditySubCategory(string text)
    {
        if (text.Contains("gold"))
        {
            return "Gold";
        }

        if (text.Contains("silver") || text.Contains("silber"))
        {
            return "Silber";
        }

        if (text.Contains("platinum") || text.Contains("platin"))
        {
            return "Platin";
        }

        return "Andere";
    }
}

public sealed record CategorizationResult(string Category, string SubCategory, string PositionType);
