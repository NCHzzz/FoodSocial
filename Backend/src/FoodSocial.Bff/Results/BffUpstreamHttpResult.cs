namespace FoodSocial.Bff;

internal sealed class BffUpstreamHttpResult(int statusCode, string? contentType, byte[] body) : IResult
{
    private readonly byte[] _body = body ?? throw new ArgumentNullException(nameof(body));

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        httpContext.Response.StatusCode = statusCode;
        if (!string.IsNullOrWhiteSpace(contentType))
        {
            httpContext.Response.ContentType = contentType;
        }

        if (_body.Length > 0)
        {
            await httpContext.Response.Body.WriteAsync(_body, httpContext.RequestAborted);
        }
    }
}
