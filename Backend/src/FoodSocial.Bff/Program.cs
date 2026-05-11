using FoodSocial.Bff;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddAntiforgery(options => options.HeaderName = "X-XSRF-TOKEN");
builder.Services.AddOpenApi();
builder.Services.AddAuthorization();
builder.Services.AddBffIdentityOrchestrationClient();
builder.Services.AddBffUserProfileClient();
builder.Services.AddBffDishClient();
builder.Services.AddBffPlaceClient();
builder.Services.AddBffPostClient();
builder.Services.AddBffCatalogResponseCache(builder.Configuration);
builder.Services.AddBffRateLimiting(builder.Configuration);
builder.Services.Configure<BffRecommendationOptions>(builder.Configuration.GetSection(BffRecommendationOptions.SectionName));
builder.Services.AddScoped<IDiscoverRecommendationProvider, DeterministicDiscoverRecommendationProvider>();
builder.Services.AddBffAuthentication(builder.Configuration, builder.Environment);
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    foreach (var proxy in builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
    {
        if (IPAddress.TryParse(proxy, out var proxyAddress))
        {
            options.KnownProxies.Add(proxyAddress);
        }
    }
});

var app = builder.Build();

app.UseExceptionHandler();

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapBffAuthEndpoints();
app.MapBffUserProfileEndpoints();
app.MapBffDishEndpoints();
app.MapBffPlaceEndpoints();
app.MapBffPostEndpoints();
app.MapBffSearchAssistEndpoints();
app.MapBffRecommendationEndpoints();

app.MapDefaultEndpoints();

app.Run();

public partial class Program;
