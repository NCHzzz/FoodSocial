using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace FoodSocial.Bff;

internal sealed class PostHttpClient(HttpClient httpClient, IInternalServiceTokenProvider serviceTokenProvider, ILogger<PostHttpClient> logger)
{
    private const string IdentityIdHeaderName = "X-FoodSocial-Identity-Id";
    private const string RolesHeaderName = "X-FoodSocial-Identity-Roles";
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly IInternalServiceTokenProvider _serviceTokenProvider = serviceTokenProvider ?? throw new ArgumentNullException(nameof(serviceTokenProvider));
    private readonly ILogger<PostHttpClient> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// GET-only forward. Use this from deep modules (composers, proxies) that do not
    /// have an inbound <see cref="HttpRequest"/> in hand. For body-bearing calls,
    /// use <see cref="ForwardAsync"/> or <see cref="ForwardJsonAsync{TRequest}"/>.
    /// </summary>
    public async Task<HttpResponseMessage> ForwardGetAsync(string targetPathAndQuery, Guid identityId = default, IReadOnlyList<string>? roles = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPathAndQuery);
        using var request = new HttpRequestMessage(HttpMethod.Get, targetPathAndQuery);
        await AddInternalServiceHeadersAsync(request, identityId, roles, cancellationToken);
        return await SendAsync(request, cancellationToken);
    }

    public async Task<HttpResponseMessage> ForwardAsync(HttpRequest sourceRequest, string targetPathAndQuery, Guid identityId = default, IReadOnlyList<string>? roles = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceRequest);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPathAndQuery);
        using var request = new HttpRequestMessage(new HttpMethod(sourceRequest.Method), targetPathAndQuery);
        await AddInternalServiceHeadersAsync(request, identityId, roles, cancellationToken);
        if (RequiresBody(sourceRequest.Method))
        {
            request.Content = new StreamContent(sourceRequest.Body);
            if (!string.IsNullOrWhiteSpace(sourceRequest.ContentType))
            {
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(sourceRequest.ContentType);
            }
        }
        return await SendAsync(request, cancellationToken);
    }

    public async Task<HttpResponseMessage> ForwardJsonAsync<TRequest>(string method, string targetPathAndQuery, TRequest body, Guid identityId, IReadOnlyList<string>? roles, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPathAndQuery);

        using var request = new HttpRequestMessage(new HttpMethod(method), targetPathAndQuery)
        {
            Content = JsonContent.Create(body)
        };
        await AddInternalServiceHeadersAsync(request, identityId, roles, cancellationToken);
        return await SendAsync(request, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            return await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            var targetPathAndQuery = request.RequestUri?.PathAndQuery ?? string.Empty;
            _logger.LogWarning(exception, "Post request to {TargetPathAndQuery} timed out after {Timeout}.", targetPathAndQuery, _httpClient.Timeout);
            throw new InvalidOperationException($"Post request to '{targetPathAndQuery}' timed out after {_httpClient.Timeout}.", exception);
        }
    }

    private async Task AddInternalServiceHeadersAsync(HttpRequestMessage request, Guid identityId, IReadOnlyList<string>? roles, CancellationToken cancellationToken)
    {
        if (identityId == Guid.Empty)
        {
            return;
        }

        var roleHeader = roles is null
            ? string.Empty
            : string.Join(',', roles
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Select(role => role.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(role => role, StringComparer.Ordinal));

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken));
        request.Headers.TryAddWithoutValidation(IdentityIdHeaderName, identityId.ToString());
        if (!string.IsNullOrWhiteSpace(roleHeader))
        {
            request.Headers.TryAddWithoutValidation(RolesHeaderName, roleHeader);
        }
    }

    private static bool RequiresBody(string method) => HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsPatch(method) || HttpMethods.IsDelete(method);
}

internal static class PostHttpClientRegistration
{
    public static IServiceCollection AddBffPostClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddHttpClient<PostHttpClient>((serviceProvider, client) =>
        {
            client.BaseAddress = ResolvePostApiBaseAddress(serviceProvider.GetRequiredService<IConfiguration>());
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        return services;
    }

    private static Uri ResolvePostApiBaseAddress(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var discoveredAddress = configuration["Services:post-api:https:0"]
            ?? configuration["Services:post-api:http:0"]
            ?? throw new InvalidOperationException(
                "Post API base address is not configured. Set an Aspire service reference for 'post-api'.");
        return new Uri(discoveredAddress.TrimEnd('/'), UriKind.Absolute);
    }
}
