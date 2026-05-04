using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace FoodSocial.Bff;

internal sealed class BffCatalogResponseCache(
    IDistributedCache cache,
    IOptions<BffCatalogCacheOptions> options,
    ILogger<BffCatalogResponseCache> logger)
{
    private readonly IDistributedCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    private readonly BffCatalogCacheOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger<BffCatalogResponseCache> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<CachedBffResponse?> TryGetAsync(
        string catalogName,
        HttpRequest request,
        string targetPathAndQuery,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogName);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPathAndQuery);

        if (!CanUseCache(request))
        {
            return null;
        }

        var cacheKey = CreateCacheKey(catalogName, request.Method, targetPathAndQuery);
        try
        {
            var cachedBytes = await _cache.GetAsync(cacheKey, cancellationToken);
            return cachedBytes is null
                ? null
                : JsonSerializer.Deserialize<CachedBffResponse>(cachedBytes);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(
                exception,
                "Catalog response cache lookup failed for {CatalogName} request {TargetPathAndQuery}.",
                catalogName,
                targetPathAndQuery);

            return null;
        }
    }

    public async Task StoreAsync(
        string catalogName,
        HttpRequest request,
        string targetPathAndQuery,
        int statusCode,
        string? contentType,
        byte[] body,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogName);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPathAndQuery);
        ArgumentNullException.ThrowIfNull(body);

        if (!CanUseCache(request) || !CanStoreResponse(statusCode, contentType))
        {
            return;
        }

        var cacheKey = CreateCacheKey(catalogName, request.Method, targetPathAndQuery);
        var cachedResponse = new CachedBffResponse(statusCode, contentType, body);
        var serializedResponse = JsonSerializer.SerializeToUtf8Bytes(cachedResponse);
        var cacheEntryOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_options.SuccessfulResponseTtlSeconds)
        };

        try
        {
            await _cache.SetAsync(cacheKey, serializedResponse, cacheEntryOptions, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(
                exception,
                "Catalog response cache store failed for {CatalogName} request {TargetPathAndQuery}.",
                catalogName,
                targetPathAndQuery);
        }
    }

    private bool CanUseCache(HttpRequest request) =>
        _options.Enabled
        && _options.SuccessfulResponseTtlSeconds > 0
        && HttpMethods.IsGet(request.Method);

    private static bool CanStoreResponse(int statusCode, string? contentType) =>
        statusCode == StatusCodes.Status200OK
        && !string.IsNullOrWhiteSpace(contentType)
        && (contentType.Contains(MediaTypeNames.Application.Json, StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("+json", StringComparison.OrdinalIgnoreCase));

    private static string CreateCacheKey(string catalogName, string method, string targetPathAndQuery)
    {
        var rawKey = $"{method}:{targetPathAndQuery}";
        var keyHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawKey)));
        return $"bff:catalog-response:{catalogName}:v1:{keyHash}";
    }
}
