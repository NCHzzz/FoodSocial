using System.Text;
using System.Text.Json.Nodes;

namespace FoodSocial.AppHost.Tests;

internal static class JwtPayloadReader
{
    public static JsonObject Read(string jwt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jwt);

        var segments = jwt.Split('.');

        if (segments.Length < 2)
        {
            throw new ArgumentException("The supplied token is not a valid JWT.", nameof(jwt));
        }

        var payloadSegment = segments[1]
            .Replace('-', '+')
            .Replace('_', '/');

        payloadSegment = payloadSegment.PadRight(payloadSegment.Length + ((4 - payloadSegment.Length % 4) % 4), '=');

        var json = Encoding.UTF8.GetString(Convert.FromBase64String(payloadSegment));

        return JsonNode.Parse(json) as JsonObject
            ?? throw new InvalidOperationException("The JWT payload was not a JSON object.");
    }
}
