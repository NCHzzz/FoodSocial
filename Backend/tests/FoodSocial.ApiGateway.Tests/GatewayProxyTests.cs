using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace FoodSocial.ApiGateway.Tests;

public sealed class GatewayProxyTests
{
    [Fact]
    public async Task ApiRoute_ForwardsPathAndQueryToBff()
    {
        var observedTargets = new List<string>();
        await using var bff = await FakeBffServer.StartAsync(context =>
        {
            observedTargets.Add(context.Request.Path + context.Request.QueryString);
            context.Response.StatusCode = StatusCodes.Status200OK;
            return context.Response.WriteAsync("""{"ok":true}""");
        });
        using var factory = new GatewayTestApplicationFactory(bff.BaseAddress);
        using var client = factory.CreateGatewayClient();

        using var response = await client.GetAsync("/api/dishes?cursor=abc&limit=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        observedTargets.Should().ContainSingle("/api/dishes?cursor=abc&limit=10");
    }

    [Theory]
    [InlineData("/api")]
    [InlineData("/signin-oidc")]
    [InlineData("/signout-callback-oidc")]
    public async Task GatewayCriticalRoutes_ProxyToBff(string route)
    {
        var observedTargets = new List<string>();
        await using var bff = await FakeBffServer.StartAsync(context =>
        {
            observedTargets.Add(context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });
        using var factory = new GatewayTestApplicationFactory(bff.BaseAddress);
        using var client = factory.CreateGatewayClient();

        using var response = await client.GetAsync(route);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        observedTargets.Should().ContainSingle(route);
    }

    [Fact]
    public async Task AspireServiceDiscoveryAddress_TakesPrecedenceOverConfiguredDevelopmentAddress()
    {
        var observedTargets = new List<string>();
        await using var bff = await FakeBffServer.StartAsync(context =>
        {
            observedTargets.Add(context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status200OK;
            return context.Response.WriteAsync("""{"ok":true}""");
        });
        using var factory = new GatewayTestApplicationFactory(
            new Uri("https://localhost:7187"),
            builder => builder.UseSetting("Services:bff:http:0", bff.BaseAddress.AbsoluteUri));
        using var client = factory.CreateGatewayClient();

        using var response = await client.GetAsync("/api/dishes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        observedTargets.Should().ContainSingle("/api/dishes");
    }

    [Fact]
    public async Task GatewayCorrelation_PropagatesInboundHeaderToBffAndResponse()
    {
        string? downstreamCorrelationId = null;
        await using var bff = await FakeBffServer.StartAsync(context =>
        {
            downstreamCorrelationId = context.Request.Headers["X-Correlation-ID"].ToString();
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });
        using var factory = new GatewayTestApplicationFactory(bff.BaseAddress);
        using var client = factory.CreateGatewayClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/profile/me");
        request.Headers.Add("X-Correlation-ID", "test-correlation-id");

        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        downstreamCorrelationId.Should().Be("test-correlation-id");
        response.Headers.GetValues("X-Correlation-ID").Should().ContainSingle("test-correlation-id");
    }

    [Fact]
    public async Task GatewayCorrelation_WhenInboundHeaderIsOversized_ReplacesItBeforeForwarding()
    {
        string? downstreamCorrelationId = null;
        await using var bff = await FakeBffServer.StartAsync(context =>
        {
            downstreamCorrelationId = context.Request.Headers["X-Correlation-ID"].ToString();
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });
        using var factory = new GatewayTestApplicationFactory(bff.BaseAddress);
        using var client = factory.CreateGatewayClient();
        var oversizedCorrelationId = new string('x', 129);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/profile/me");
        request.Headers.Add("X-Correlation-ID", oversizedCorrelationId);

        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        downstreamCorrelationId.Should().NotBe(oversizedCorrelationId);
        downstreamCorrelationId.Should().NotBeNullOrWhiteSpace();
        response.Headers.GetValues("X-Correlation-ID").Should().NotContain(oversizedCorrelationId);
    }

    [Fact]
    public async Task GatewaySecurityHeaders_AreAppliedToGatewayResponses()
    {
        await using var bff = await FakeBffServer.StartAsync(_ => Task.CompletedTask);
        using var factory = new GatewayTestApplicationFactory(bff.BaseAddress);
        using var client = factory.CreateGatewayClient();

        using var response = await client.GetAsync("/not-routed");

        response.Headers.GetValues("X-Content-Type-Options").Should().ContainSingle("nosniff");
        response.Headers.GetValues("Referrer-Policy").Should().ContainSingle("no-referrer");
        response.Headers.GetValues("X-Frame-Options").Should().ContainSingle("DENY");
        response.Headers.GetValues("Permissions-Policy").Should().ContainSingle("camera=(), microphone=(), geolocation=()");
        response.Headers.GetValues("Cross-Origin-Opener-Policy").Should().ContainSingle("same-origin");
        response.Headers.GetValues("Cross-Origin-Resource-Policy").Should().ContainSingle("same-site");
        response.Headers.GetValues("X-Permitted-Cross-Domain-Policies").Should().ContainSingle("none");
        response.Headers.GetValues("X-FoodSocial-Edge").Should().ContainSingle("api-gateway");
    }

    [Fact]
    public async Task GatewaySecurityHeaders_OverrideConflictingBffHeadersOnProxiedResponses()
    {
        await using var bff = await FakeBffServer.StartAsync(context =>
        {
            context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
            context.Response.Headers["Referrer-Policy"] = "origin";
            context.Response.Headers["X-Content-Type-Options"] = "invalid";
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });
        using var factory = new GatewayTestApplicationFactory(bff.BaseAddress);
        using var client = factory.CreateGatewayClient();

        using var response = await client.GetAsync("/api/profile/me");

        response.Headers.GetValues("X-Content-Type-Options").Should().ContainSingle("nosniff");
        response.Headers.GetValues("Referrer-Policy").Should().ContainSingle("no-referrer");
        response.Headers.GetValues("X-Frame-Options").Should().ContainSingle("DENY");
        response.Headers.GetValues("X-FoodSocial-Edge").Should().ContainSingle("api-gateway");
    }

    [Fact]
    public async Task Gateway_ForwardsOriginalHostAndSchemeToBff()
    {
        string? forwardedHost = null;
        string? forwardedProto = null;
        await using var bff = await FakeBffServer.StartAsync(context =>
        {
            forwardedHost = context.Request.Headers["X-Forwarded-Host"].ToString();
            forwardedProto = context.Request.Headers["X-Forwarded-Proto"].ToString();
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });
        using var factory = new GatewayTestApplicationFactory(bff.BaseAddress);
        using var client = factory.CreateGatewayClient();

        using var response = await client.GetAsync("/api/auth/login");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        forwardedHost.Should().Be("localhost");
        forwardedProto.Should().Be("https");
    }

    [Fact]
    public async Task UnknownRoute_ReturnsGatewayProblemDetails()
    {
        await using var bff = await FakeBffServer.StartAsync(_ => Task.CompletedTask);
        using var factory = new GatewayTestApplicationFactory(bff.BaseAddress);
        using var client = factory.CreateGatewayClient();

        using var response = await client.GetAsync("/not-routed");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Type.Should().Be("https://foodsocial/errors/gateway/route-not-found");
        problem.Title.Should().Be("Gateway route not found");
    }

    [Fact]
    public async Task GatewayGeneratedBadGateway_ReturnsGatewayProblemDetails()
    {
        using var factory = new GatewayTestApplicationFactory(new Uri("http://127.0.0.1:1"));
        using var client = factory.CreateGatewayClient();

        using var response = await client.GetAsync("/api/downstream-failure");

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Type.Should().Be("https://foodsocial/errors/gateway/downstream-unavailable");
        problem.Extensions.Should().ContainKey("traceId");
    }

    [Fact]
    public async Task RateLimit_WhenExceeded_ReturnsGatewayProblemDetails()
    {
        await using var bff = await FakeBffServer.StartAsync(context =>
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            return context.Response.WriteAsync("""{"ok":true}""");
        });
        using var factory = new GatewayTestApplicationFactory(
            bff.BaseAddress,
            builder =>
            {
                builder.UseSetting("Gateway:RateLimiting:Default:PermitLimit", "1");
                builder.UseSetting("Gateway:RateLimiting:Default:WindowSeconds", "60");
            });
        using var client = factory.CreateGatewayClient();

        using var first = await client.GetAsync("/api/rate-limited");
        using var second = await client.GetAsync("/api/rate-limited");

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        second.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var problem = await second.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Type.Should().Be("https://foodsocial/errors/gateway/rate-limit-exceeded");
        Convert.ToString(problem.Extensions["rateLimitPolicy"]).Should().Be("default");
        problem.Extensions.Should().ContainKey("traceId");
    }

    [Theory]
    [InlineData("/api/auth/login", "Auth")]
    [InlineData("/api/media/uploads", "MediaUpload")]
    [InlineData("/api/posts", "RiskyWrite")]
    public async Task RateLimit_AppliesSpecificPolicyForRoute(string path, string policyName)
    {
        await using var bff = await FakeBffServer.StartAsync(context =>
        {
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });
        using var factory = new GatewayTestApplicationFactory(
            bff.BaseAddress,
            builder =>
            {
                builder.UseSetting($"Gateway:RateLimiting:{policyName}:PermitLimit", "1");
                builder.UseSetting($"Gateway:RateLimiting:{policyName}:WindowSeconds", "60");
            });
        using var client = factory.CreateGatewayClient();

        using var first = await client.PostAsync(path, JsonContent.Create(new { ok = true }));
        using var second = await client.PostAsync(path, JsonContent.Create(new { ok = true }));

        first.StatusCode.Should().Be(HttpStatusCode.NoContent);
        second.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task RateLimit_BypassPathsAreNotLimited()
    {
        await using var bff = await FakeBffServer.StartAsync(_ => Task.CompletedTask);
        using var factory = new GatewayTestApplicationFactory(
            bff.BaseAddress,
            builder =>
            {
                builder.UseSetting("Gateway:RateLimiting:Default:PermitLimit", "1");
                builder.UseSetting("Gateway:RateLimiting:Default:WindowSeconds", "60");
            });
        using var client = factory.CreateGatewayClient();

        using var first = await client.GetAsync("/alive");
        using var second = await client.GetAsync("/alive");

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RateLimit_DoesNotUseSpoofedForwardedForAsClientPartition()
    {
        await using var bff = await FakeBffServer.StartAsync(context =>
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            return context.Response.WriteAsync("""{"ok":true}""");
        });
        using var factory = new GatewayTestApplicationFactory(
            bff.BaseAddress,
            builder =>
            {
                builder.UseSetting("Gateway:RateLimiting:Default:PermitLimit", "1");
                builder.UseSetting("Gateway:RateLimiting:Default:WindowSeconds", "60");
            });
        using var client = factory.CreateGatewayClient();
        using var firstRequest = new HttpRequestMessage(HttpMethod.Get, "/api/rate-limited");
        firstRequest.Headers.Add("X-Forwarded-For", "203.0.113.10");
        using var secondRequest = new HttpRequestMessage(HttpMethod.Get, "/api/rate-limited");
        secondRequest.Headers.Add("X-Forwarded-For", "203.0.113.11");

        using var first = await client.SendAsync(firstRequest);
        using var second = await client.SendAsync(secondRequest);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public void ProductionGateway_WhenCorsOriginsAreMissing_FailsFast()
    {
        using var factory = new GatewayTestApplicationFactory(
            new Uri("https://localhost:7187"),
            builder => builder.UseEnvironment(Environments.Production));

        var action = () => factory.CreateGatewayClient();

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Gateway CORS allowed origins must be configured outside Development.");
    }

    [Fact]
    public void ProductionGateway_WhenForwardedHeaderTrustIsMissing_FailsFast()
    {
        using var factory = new GatewayTestApplicationFactory(
            new Uri("https://localhost:7187"),
            builder =>
            {
                builder.UseEnvironment(Environments.Production);
                builder.UseSetting("Gateway:Cors:AllowedOrigins:0", "https://foodsocial.example.com");
            });

        var action = () => factory.CreateGatewayClient();

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Gateway forwarded headers trusted proxies or networks must be configured outside Development.");
    }

    [Fact]
    public void ProductionGateway_WhenForwardedHeaderNetworkIsInvalid_FailsFast()
    {
        using var factory = new GatewayTestApplicationFactory(
            new Uri("https://localhost:7187"),
            builder =>
            {
                builder.UseEnvironment(Environments.Production);
                builder.UseSetting("Gateway:Cors:AllowedOrigins:0", "https://foodsocial.example.com");
                builder.UseSetting("ForwardedHeaders:KnownNetworks:0", "not-a-network");
            });

        var action = () => factory.CreateGatewayClient();

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Gateway forwarded headers known network 'not-a-network' must be a valid CIDR network.");
    }

    [Fact]
    public async Task Cors_WhenOriginIsAllowed_ReturnsCorsHeaders()
    {
        await using var bff = await FakeBffServer.StartAsync(context =>
        {
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });
        using var factory = new GatewayTestApplicationFactory(
            bff.BaseAddress,
            builder => builder.UseSetting("Gateway:Cors:AllowedOrigins:0", "https://app.foodsocial.test"));
        using var client = factory.CreateGatewayClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/posts");
        request.Headers.Add("Origin", "https://app.foodsocial.test");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.GetValues("Access-Control-Allow-Origin").Should().ContainSingle("https://app.foodsocial.test");
        response.Headers.GetValues("Access-Control-Allow-Credentials").Should().ContainSingle("true");
    }

    [Fact]
    public async Task GatewayHealth_UsesBffReadinessEndpoint()
    {
        var observedTargets = new List<string>();
        await using var bff = await FakeBffServer.StartAsync(context =>
        {
            observedTargets.Add(context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        });
        using var factory = new GatewayTestApplicationFactory(bff.BaseAddress);
        using var client = factory.CreateGatewayClient();

        using var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        observedTargets.Should().Contain("/health");
    }

    [Fact]
    public async Task GatewayHealth_WhenBffReadinessFails_ReturnsServiceUnavailable()
    {
        await using var bff = await FakeBffServer.StartAsync(context =>
        {
            context.Response.StatusCode = context.Request.Path.StartsWithSegments("/health")
                ? StatusCodes.Status503ServiceUnavailable
                : StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });
        using var factory = new GatewayTestApplicationFactory(bff.BaseAddress);
        using var client = factory.CreateGatewayClient();

        using var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task OversizedRequest_WhenBodyExceedsConfiguredLimit_IsRejectedAtGateway()
    {
        await using var bff = await FakeBffServer.StartAsync(context =>
        {
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });
        using var factory = new GatewayTestApplicationFactory(
            bff.BaseAddress,
            builder => builder.UseSetting("Gateway:MaxRequestBodySizeBytes", "1048576"));
        using var client = factory.CreateGatewayClient();
        using var content = new ByteArrayContent(new byte[1048577]);

        using var response = await client.PostAsync("/api/posts", content);

        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
    }

    private sealed class GatewayTestApplicationFactory(
        Uri bffAddress,
        Action<IWebHostBuilder>? configure = null) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("Gateway:Clusters:Bff:Address", bffAddress.AbsoluteUri);
            builder.UseSetting("Gateway:RateLimiting:Enabled", bool.TrueString);
            builder.UseSetting("ServiceDefaults:ExposeHealthEndpoints", bool.TrueString);
            configure?.Invoke(builder);
        }

        public HttpClient CreateGatewayClient() =>
            CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost")
            });
    }

    private sealed class FakeBffServer : IAsyncDisposable
    {
        private readonly WebApplication _application;

        private FakeBffServer(WebApplication application, Uri baseAddress)
        {
            _application = application;
            BaseAddress = baseAddress;
        }

        public Uri BaseAddress { get; }

        public static async Task<FakeBffServer> StartAsync(Func<HttpContext, Task> handler)
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = Environments.Development
            });
            builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");

            var app = builder.Build();
            app.MapGet("/alive", () => Results.Ok());
            app.Use(async (HttpContext context, RequestDelegate _) => await handler(context));
            await app.StartAsync();

            var addresses = app.Services
                .GetRequiredService<IServer>()
                .Features
                .Get<IServerAddressesFeature>()
                ?.Addresses;
            var address = addresses?.Single()
                ?? throw new InvalidOperationException("Fake BFF server did not publish an address.");

            return new FakeBffServer(app, new Uri(address));
        }

        public async ValueTask DisposeAsync()
        {
            await _application.DisposeAsync();
        }
    }
}
