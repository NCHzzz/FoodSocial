using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace FoodSocial.Bff;

internal static class BffSearchAssistEndpoints
{
    private const int MaxQueryLength = 200;
    private const int MaxLocationLength = 120;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IEndpointRouteBuilder MapBffSearchAssistEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapPost("/api/search/assist", AssistAsync);
        return endpoints;
    }

    private static async Task<IResult> AssistAsync(HttpContext httpContext, IConfiguration configuration, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(configuration);

        SearchAssistRequest? request;
        try
        {
            request = await JsonSerializer.DeserializeAsync<SearchAssistRequest>(httpContext.Request.Body, JsonOptions, cancellationToken);
        }
        catch (JsonException)
        {
            return CreateInvalidRequestProblem(httpContext, "Search assist request body must be valid JSON.");
        }

        if (request is null || string.IsNullOrWhiteSpace(request.Query))
        {
            return CreateInvalidRequestProblem(httpContext, "Search query is required.");
        }

        var normalizedQuery = Normalize(request.Query);
        var location = Normalize(request.Location);
        if (normalizedQuery.Length > MaxQueryLength)
        {
            return CreateInvalidRequestProblem(httpContext, $"Search query must be {MaxQueryLength} characters or fewer.");
        }

        if (location.Length > MaxLocationLength)
        {
            return CreateInvalidRequestProblem(httpContext, $"Search location must be {MaxLocationLength} characters or fewer.");
        }

        var aiEnabled = configuration.GetValue("SearchAssist:AiEnabled", false);
        var tokens = normalizedQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Take(6).ToArray();
        var suggestions = BuildSuggestions(normalizedQuery, location, tokens);
        var filters = InferFilters(location, tokens);

        return Results.Ok(new SearchAssistResponse(
            NormalizedQuery: normalizedQuery,
            AssistStatus: aiEnabled ? "deterministic-fallback" : "ai-disabled-fallback",
            FallbackUsed: true,
            Suggestions: suggestions,
            Filters: filters,
            Explanation: "AI provider calls are not on the critical path; deterministic dish, place, tag, and location hints keep search usable."));
    }

    private static IReadOnlyList<SearchAssistSuggestion> BuildSuggestions(string query, string? location, IReadOnlyList<string> tokens)
    {
        var suggestions = new List<SearchAssistSuggestion>
        {
            new("dish", query, $"Search dishes matching '{query}'."),
            new("post", query, $"Find posts mentioning '{query}'.")
        };

        if (!string.IsNullOrWhiteSpace(location))
        {
            suggestions.Add(new("place", location, $"Prioritize places around {location}."));
        }

        foreach (var token in tokens.Where(token => token.Length >= 3).Take(3))
        {
            suggestions.Add(new("tag", token.TrimStart('#'), $"Try tag #{token.TrimStart('#')} as a food discovery filter."));
        }

        return suggestions;
    }

    private static IReadOnlyDictionary<string, string> InferFilters(string? location, IReadOnlyList<string> tokens)
    {
        var filters = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(location)) filters["location"] = location;
        var cuisineToken = tokens.FirstOrDefault(token => string.Equals(token, "viet", StringComparison.OrdinalIgnoreCase) || string.Equals(token, "vietnamese", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(cuisineToken)) filters["cuisineType"] = "Vietnamese";
        var spicyToken = tokens.FirstOrDefault(token => string.Equals(token, "spicy", StringComparison.OrdinalIgnoreCase) || string.Equals(token, "cay", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(spicyToken)) filters["tag"] = "spicy";
        return filters;
    }

    private static string Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    private static IResult CreateInvalidRequestProblem(HttpContext httpContext, string detail) =>
        Results.Problem(new ProblemDetails
        {
            Type = "https://foodsocial/errors/bff/invalid-search-assist-request",
            Title = "Invalid search assist request",
            Detail = detail,
            Status = StatusCodes.Status400BadRequest,
            Extensions = { ["traceId"] = httpContext.TraceIdentifier }
        });

    private sealed record SearchAssistRequest(string Query, string? Location = null);
    private sealed record SearchAssistSuggestion(string Type, string Value, string Reason);
    private sealed record SearchAssistResponse(string NormalizedQuery, string AssistStatus, bool FallbackUsed, IReadOnlyList<SearchAssistSuggestion> Suggestions, IReadOnlyDictionary<string, string> Filters, string Explanation);
}
