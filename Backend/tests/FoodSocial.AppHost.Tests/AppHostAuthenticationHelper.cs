using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Json.Nodes;

namespace FoodSocial.AppHost.Tests;

internal sealed class AppHostAuthenticationHelper(AppHostResourceDiscovery resources)
{
    private const string DemoUsername = "demo-user";
    private const string RealmName = "foodsocial";
    private const string TokenClientId = "foodsocial-cli";
    private static readonly Uri BrowserBffHttpsOrigin = new("https://localhost:7187", UriKind.Absolute);
    private static readonly TimeSpan BrowserLoginRetryDelay = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan TokenRetryDelay = TimeSpan.FromSeconds(2);

    private readonly AppHostResourceDiscovery _resources = resources ?? throw new ArgumentNullException(nameof(resources));
    private static string DemoPassword => AppHostFixture.TestKeycloakDemoUserPassword;

    public async Task<HttpClient> CreateBffClientAsync(CancellationToken cancellationToken)
    {
        var client = _resources.CreateHttpClient("bff", "https");
        client.DefaultRequestHeaders.Authorization = await CreateAuthorizationHeaderAsync(cancellationToken);
        return client;
    }

    public async Task<HttpClient> CreateAuthenticatedBffBearerClientAsync(CancellationToken cancellationToken)
    {
        var client = _resources.CreateHttpClient("bff", "https");
        client.DefaultRequestHeaders.Authorization = await CreateAuthorizationHeaderAsync(cancellationToken);
        return client;
    }

    public async Task<HttpClient> CreateIdentityApiClientAsync(CancellationToken cancellationToken)
    {
        var client = _resources.CreateHttpClient("identity-api", "https");
        client.DefaultRequestHeaders.Authorization = await CreateAuthorizationHeaderAsync(cancellationToken);
        return client;
    }

    public async Task<HttpClient> CreateAuthenticatedBffBrowserClientAsync(CancellationToken cancellationToken)
    {
        const int maxAttempts = 15;
        string? lastFailure = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var actualBffHttpsOrigin = _resources.GetEndpoint("bff", "https");
            var browserCookies = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false,
                CookieContainer = new CookieContainer(),
                UseCookies = true,
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            var client = new HttpClient(handler, disposeHandler: true)
            {
                BaseAddress = actualBffHttpsOrigin
            };

            try
            {
                using var loginEntryResponse = await SendBrowserGetAsync(
                    client,
                    actualBffHttpsOrigin,
                    new Uri("/api/auth/login?returnUrl=%2Fapi%2Fauth%2Fsession", UriKind.Relative),
                    browserCookies,
                    cancellationToken);

                using var loginPageResponse = await FollowRedirectsAsync(client, actualBffHttpsOrigin, loginEntryResponse, browserCookies, cancellationToken);

                var loginPageBody = await loginPageResponse.Content.ReadAsStringAsync(cancellationToken);
                if (!loginPageResponse.IsSuccessStatusCode)
                {
                    lastFailure = $"Login challenge returned {(int)loginPageResponse.StatusCode} {loginPageResponse.ReasonPhrase}: {loginPageBody}";
                    client.Dispose();
                    if (attempt < maxAttempts)
                    {
                        await Task.Delay(BrowserLoginRetryDelay, cancellationToken);
                        continue;
                    }

                    throw new HttpRequestException(lastFailure);
                }

                var loginPageUri = loginPageResponse.RequestMessage?.RequestUri
                    ?? throw new InvalidOperationException("The login page response did not expose a request URI.");
                var loginFormAction = ResolveLoginFormAction(loginPageUri, loginPageBody);
                var loginFormPayload = ExtractHiddenFields(loginPageBody);
                if (RequiresCredentialSubmission(loginFormAction, loginPageBody))
                {
                    loginFormPayload["username"] = DemoUsername;
                    loginFormPayload["password"] = DemoPassword;
                }

                using var loginResponse = await SendBrowserPostAsync(
                    client,
                    actualBffHttpsOrigin,
                    loginFormAction,
                    new FormUrlEncodedContent(loginFormPayload),
                    browserCookies,
                    cancellationToken);

                using var finalResponse = await FollowRedirectsAsync(client, actualBffHttpsOrigin, loginResponse, browserCookies, cancellationToken);

                var finalUri = finalResponse.RequestMessage?.RequestUri;
                if (finalUri is not null && !string.Equals(finalUri.AbsolutePath, "/api/auth/session", StringComparison.Ordinal))
                {
                    var loginResponseBody = await finalResponse.Content.ReadAsStringAsync(cancellationToken);
                    lastFailure = $"Login flow ended on unexpected URI '{finalUri}'. Response body: {loginResponseBody}";
                    client.Dispose();
                    if (attempt < maxAttempts)
                    {
                        await Task.Delay(BrowserLoginRetryDelay, cancellationToken);
                        continue;
                    }

                    throw new InvalidOperationException(lastFailure);
                }

                if (!finalResponse.IsSuccessStatusCode)
                {
                    var sessionResponseBody = await finalResponse.Content.ReadAsStringAsync(cancellationToken);
                    lastFailure = $"Login flow reached '/api/auth/session' but returned {(int)finalResponse.StatusCode} {finalResponse.ReasonPhrase}: {sessionResponseBody}";
                    client.Dispose();
                    if (attempt < maxAttempts)
                    {
                        await Task.Delay(BrowserLoginRetryDelay, cancellationToken);
                        continue;
                    }

                    throw new InvalidOperationException(lastFailure);
                }

                return client;
            }
            catch (Exception ex) when (attempt < maxAttempts && ex is not OperationCanceledException)
            {
                lastFailure = ex.ToString();
                client.Dispose();
                await Task.Delay(BrowserLoginRetryDelay, cancellationToken);
            }
        }

        throw new InvalidOperationException(
            $"Unable to establish an authenticated BFF browser session for '{DemoUsername}'. Last failure: {lastFailure}");
    }

    public async Task<string> GetDemoUserAccessTokenAsync(CancellationToken cancellationToken)
    {
        const int maxAttempts = 15;
        string? lastFailure = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var keycloakHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
            using var keycloakClient = new HttpClient(keycloakHandler, disposeHandler: true)
            {
                BaseAddress = _resources.GetEndpoint("keycloak", "http")
            };
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/realms/{RealmName}/protocol/openid-connect/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = TokenClientId,
                    ["grant_type"] = "password",
                    ["username"] = DemoUsername,
                    ["password"] = DemoPassword
                })
            };

            using var response = await keycloakClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var payload = await response.Content.ReadFromJsonAsync<JsonObject>(cancellationToken);
                var accessToken = payload?["access_token"]?.GetValue<string>();

                if (!string.IsNullOrWhiteSpace(accessToken))
                {
                    var tokenPayload = JwtPayloadReader.Read(accessToken);
                    var claimKeys = string.Join(", ", tokenPayload.Select(property => property.Key).OrderBy(key => key, StringComparer.OrdinalIgnoreCase));

                    if (!tokenPayload.TryGetPropertyValue("sub", out _))
                    {
                        lastFailure = $"Token endpoint returned an access token without 'sub'. Payload keys: {claimKeys}.";

                        if (attempt < maxAttempts)
                        {
                            await Task.Delay(TokenRetryDelay, cancellationToken);
                            continue;
                        }

                        throw new InvalidOperationException(lastFailure);
                    }

                    return accessToken;
                }

                lastFailure = "Token endpoint response did not contain access_token.";
            }
            else
            {
                lastFailure = $"Token endpoint returned {(int)response.StatusCode} {response.ReasonPhrase}: {await response.Content.ReadAsStringAsync(cancellationToken)}";
            }

            if (attempt < maxAttempts)
            {
                await Task.Delay(TokenRetryDelay, cancellationToken);
            }
        }

        throw new InvalidOperationException($"Unable to acquire a Keycloak access token for '{DemoUsername}'. Last failure: {lastFailure}");
    }

    private async Task<AuthenticationHeaderValue> CreateAuthorizationHeaderAsync(CancellationToken cancellationToken)
    {
        var accessToken = await GetDemoUserAccessTokenAsync(cancellationToken);
        await WaitForIdentityApiAuthenticationAsync(accessToken, cancellationToken);
        return new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private async Task WaitForIdentityApiAuthenticationAsync(string accessToken, CancellationToken cancellationToken)
    {
        const int maxAttempts = 15;
        string? lastFailure = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var client = _resources.CreateHttpClient("identity-api", "https");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            try
            {
                using var response = await client.GetAsync("/identity/current-user", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }

                lastFailure = $"Identity API returned {(int)response.StatusCode} {response.ReasonPhrase}: {await response.Content.ReadAsStringAsync(cancellationToken)}";
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                lastFailure = exception.Message;
            }

            if (attempt < maxAttempts)
            {
                await Task.Delay(TokenRetryDelay, cancellationToken);
            }
        }

        throw new InvalidOperationException($"Identity API never accepted the demo bearer token. Last failure: {lastFailure}");
    }

    private static async Task<HttpResponseMessage> FollowRedirectsAsync(
        HttpClient client,
        Uri actualBffHttpsOrigin,
        HttpResponseMessage response,
        IReadOnlyDictionary<string, Dictionary<string, string>> browserCookies,
        CancellationToken cancellationToken)
    {
        const int maxRedirects = 10;
        var currentResponse = response;

        for (var redirectCount = 0; redirectCount < maxRedirects; redirectCount++)
        {
            if (IsRedirectStatusCode(currentResponse.StatusCode))
            {
                var location = currentResponse.Headers.Location
                    ?? throw new InvalidOperationException("A redirect response did not include a Location header.");

                var requestUri = currentResponse.RequestMessage?.RequestUri
                    ?? throw new InvalidOperationException("A redirect response did not expose its request URI.");
                var nextUri = location.IsAbsoluteUri ? location : new Uri(requestUri, location);

                currentResponse.Dispose();
                currentResponse = await SendBrowserGetAsync(client, actualBffHttpsOrigin, nextUri, browserCookies, cancellationToken);
                continue;
            }

            var autoSubmitForm = await TryCreateAutoSubmitFormAsync(currentResponse, actualBffHttpsOrigin, cancellationToken);
            if (autoSubmitForm is null)
            {
                break;
            }

            currentResponse.Dispose();
            currentResponse = await SendBrowserPostAsync(
                client,
                actualBffHttpsOrigin,
                autoSubmitForm.Action,
                new FormUrlEncodedContent(autoSubmitForm.Payload),
                browserCookies,
                cancellationToken);
        }

        return currentResponse;
    }

    private static bool IsRedirectStatusCode(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.Moved
            or HttpStatusCode.Redirect
            or HttpStatusCode.RedirectMethod
            or HttpStatusCode.TemporaryRedirect
            or HttpStatusCode.PermanentRedirect
            or HttpStatusCode.SeeOther;

    private static Task<HttpResponseMessage> SendBrowserGetAsync(
        HttpClient client,
        Uri actualBffHttpsOrigin,
        Uri requestUri,
        IReadOnlyDictionary<string, Dictionary<string, string>> browserCookies,
        CancellationToken cancellationToken) =>
        SendBrowserRequestAsync(client, actualBffHttpsOrigin, HttpMethod.Get, requestUri, content: null, browserCookies, cancellationToken);

    private static Task<HttpResponseMessage> SendBrowserPostAsync(
        HttpClient client,
        Uri actualBffHttpsOrigin,
        Uri requestUri,
        HttpContent content,
        IReadOnlyDictionary<string, Dictionary<string, string>> browserCookies,
        CancellationToken cancellationToken) =>
        SendBrowserRequestAsync(client, actualBffHttpsOrigin, HttpMethod.Post, requestUri, content, browserCookies, cancellationToken);

    private static async Task EnsureAuthenticatedSessionAsync(HttpClient client, Uri actualBffHttpsOrigin, CancellationToken cancellationToken)
    {
        const int maxAttempts = 15;
        string? lastBody = null;
        HttpStatusCode? lastStatusCode = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var sessionResponse = await SendBrowserGetAsync(
                client,
                actualBffHttpsOrigin,
                new Uri("/api/auth/session", UriKind.Relative),
                new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase),
                cancellationToken);
            lastStatusCode = sessionResponse.StatusCode;
            lastBody = await sessionResponse.Content.ReadAsStringAsync(cancellationToken);

            if (sessionResponse.IsSuccessStatusCode)
            {
                return;
            }

            if (attempt < maxAttempts)
            {
                await Task.Delay(BrowserLoginRetryDelay, cancellationToken);
            }
        }

        throw new InvalidOperationException(
            $"The BFF browser session was not established successfully. Final /api/auth/session response: {(int?)lastStatusCode} {lastStatusCode}: {lastBody}");
    }

    private static async Task<HttpResponseMessage> SendBrowserRequestAsync(
        HttpClient client,
        Uri actualBffHttpsOrigin,
        HttpMethod method,
        Uri requestUri,
        HttpContent? content,
        IReadOnlyDictionary<string, Dictionary<string, string>> browserCookies,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(actualBffHttpsOrigin);
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(requestUri);

        var target = requestUri.IsAbsoluteUri
            ? RewriteBffBrowserUri(requestUri, actualBffHttpsOrigin)
            : new Uri(actualBffHttpsOrigin, requestUri);

        using var request = new HttpRequestMessage(method, target)
        {
            Content = content
        };

        if (!requestUri.IsAbsoluteUri || IsSameOrigin(requestUri, BrowserBffHttpsOrigin))
        {
            request.Headers.Host = BrowserBffHttpsOrigin.Authority;
        }

        AttachBrowserCookies(request, target, browserCookies);

        var response = await client.SendAsync(request, cancellationToken);
        CaptureResponseCookies(response, target, browserCookies);
        return response;
    }

    private static void AttachBrowserCookies(
        HttpRequestMessage request,
        Uri target,
        IReadOnlyDictionary<string, Dictionary<string, string>> browserCookies)
    {
        if (!browserCookies.TryGetValue(target.Host, out var cookies) || cookies.Count == 0)
        {
            return;
        }

        request.Headers.Remove("Cookie");
        request.Headers.TryAddWithoutValidation(
            "Cookie",
            string.Join("; ", cookies.Select(pair => $"{pair.Key}={pair.Value}")));
    }

    private static void CaptureResponseCookies(
        HttpResponseMessage response,
        Uri target,
        IReadOnlyDictionary<string, Dictionary<string, string>> browserCookies)
    {
        if (browserCookies is not Dictionary<string, Dictionary<string, string>> mutableCookies)
        {
            return;
        }

        if (!response.Headers.TryGetValues("Set-Cookie", out var setCookieValues))
        {
            return;
        }

        if (!mutableCookies.TryGetValue(target.Host, out var cookiesForAuthority))
        {
            cookiesForAuthority = new Dictionary<string, string>(StringComparer.Ordinal);
            mutableCookies[target.Host] = cookiesForAuthority;
        }

        foreach (var setCookie in setCookieValues)
        {
            var cookiePair = setCookie.Split(';', 2, StringSplitOptions.TrimEntries)[0];
            var separatorIndex = cookiePair.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var name = cookiePair[..separatorIndex];
            var value = cookiePair[(separatorIndex + 1)..];
            cookiesForAuthority[name] = value;
        }
    }

    private static bool IsSameOrigin(Uri left, Uri right) =>
        string.Equals(left.Scheme, right.Scheme, StringComparison.OrdinalIgnoreCase)
        && string.Equals(left.Host, right.Host, StringComparison.OrdinalIgnoreCase)
        && left.Port == right.Port;

    private static Uri RewriteBffBrowserUri(Uri requestUri, Uri actualBffHttpsOrigin)
    {
        ArgumentNullException.ThrowIfNull(requestUri);
        ArgumentNullException.ThrowIfNull(actualBffHttpsOrigin);

        return IsSameOrigin(requestUri, BrowserBffHttpsOrigin)
            ? new Uri(actualBffHttpsOrigin, requestUri.PathAndQuery)
            : requestUri;
    }

    private static Dictionary<string, string> ExtractHiddenFields(string html)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(html);

        var fields = new Dictionary<string, string>(StringComparer.Ordinal);
        var inputMatches = Regex.Matches(
            html,
            "<input\\b[^>]*>",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        foreach (Match match in inputMatches)
        {
            var inputTag = match.Value;
            var type = GetAttributeValue(inputTag, "type");

            if (!string.Equals(type, "hidden", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var name = GetAttributeValue(inputTag, "name");
            var value = WebUtility.HtmlDecode(GetAttributeValue(inputTag, "value") ?? string.Empty);

            if (!string.IsNullOrWhiteSpace(name))
            {
                fields[name] = value;
            }
        }

        return fields;
    }

    private static Uri ResolveLoginFormAction(Uri loginPageUri, string html)
    {
        ArgumentNullException.ThrowIfNull(loginPageUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(html);

        var formMatches = Regex.Matches(
            html,
            "<form\\b[^>]*>",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        foreach (Match formMatch in formMatches)
        {
            var formTag = formMatch.Value;
            var formId = GetAttributeValue(formTag, "id");
            var action = GetAttributeValue(formTag, "action");

            if (string.IsNullOrWhiteSpace(action))
            {
                continue;
            }

            var resolvedAction = new Uri(loginPageUri, WebUtility.HtmlDecode(action));
            var hasExpectedFormId = string.Equals(formId, "kc-form-login", StringComparison.OrdinalIgnoreCase);
            var looksLikeKeycloakLoginAction = resolvedAction.AbsolutePath.Contains("/login-actions/", StringComparison.OrdinalIgnoreCase);
            var looksLikeBffSigninCallback = string.Equals(resolvedAction.AbsolutePath, "/signin-oidc", StringComparison.OrdinalIgnoreCase)
                && IsSameOrigin(resolvedAction, BrowserBffHttpsOrigin);

            if (hasExpectedFormId || looksLikeKeycloakLoginAction || looksLikeBffSigninCallback)
            {
                return resolvedAction;
            }
        }

        var snippetLength = Math.Min(html.Length, 1200);
        var htmlSnippet = html[..snippetLength].Replace("\r", " ").Replace("\n", " ");

        throw new InvalidOperationException(
            $"The Keycloak login page at '{loginPageUri}' did not contain a recognizable login form action. HTML snippet: {htmlSnippet}");
    }

    private static string? GetAttributeValue(string htmlTag, string attributeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(htmlTag);
        ArgumentException.ThrowIfNullOrWhiteSpace(attributeName);

        var attributeMatch = Regex.Match(
            htmlTag,
            $"{Regex.Escape(attributeName)}\\s*=\\s*(?:\"(?<value>[^\"]*)\"|'(?<value>[^']*)'|(?<value>[^\\s>]+))",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        return attributeMatch.Success
            ? attributeMatch.Groups["value"].Value
            : null;
    }

    private static bool RequiresCredentialSubmission(Uri loginFormAction, string html)
    {
        ArgumentNullException.ThrowIfNull(loginFormAction);
        ArgumentException.ThrowIfNullOrWhiteSpace(html);

        if (loginFormAction.AbsolutePath.Contains("/login-actions/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return html.Contains("name=\"username\"", StringComparison.OrdinalIgnoreCase)
            || html.Contains("name='username'", StringComparison.OrdinalIgnoreCase)
            || html.Contains("id=\"username\"", StringComparison.OrdinalIgnoreCase)
            || html.Contains("id='username'", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<AutoSubmitForm?> TryCreateAutoSubmitFormAsync(
        HttpResponseMessage response,
        Uri actualBffHttpsOrigin,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(actualBffHttpsOrigin);

        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!html.Contains("document.forms[0].submit()", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var requestUri = response.RequestMessage?.RequestUri;
        if (requestUri is null)
        {
            return null;
        }

        var action = ResolveLoginFormAction(requestUri, html);
        var payload = ExtractHiddenFields(html);
        return new AutoSubmitForm(action, payload);
    }

    private sealed record AutoSubmitForm(Uri Action, IReadOnlyDictionary<string, string> Payload);
}
