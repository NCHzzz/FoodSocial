using FoodSocial.ApiGateway;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var gatewayOptions = GatewayOptions.Load(builder.Configuration);
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = gatewayOptions.MaxRequestBodySizeBytes;
});

builder.Services.AddProblemDetails();
builder.Services.AddGatewayCors(builder.Configuration, builder.Environment);
builder.Services.AddGatewayRateLimiting(builder.Configuration);
builder.Services.AddHttpClient<GatewayBffHealthCheck>(client =>
{
    client.BaseAddress = gatewayOptions.BffAddress;
    client.Timeout = TimeSpan.FromSeconds(gatewayOptions.BffHealthTimeoutSeconds);
});
builder.Services.AddHealthChecks()
    .AddCheck<GatewayBffHealthCheck>("gateway-bff");
builder.Services.ConfigureGatewayForwardedHeaders(builder.Configuration, builder.Environment);

builder.Services
    .AddReverseProxy()
    .LoadFromMemory(
        GatewayProxyConfiguration.CreateRoutes(),
        GatewayProxyConfiguration.CreateClusters(gatewayOptions));

var app = builder.Build();

app.UseExceptionHandler();
app.UseForwardedHeaders();
app.UseGatewayStatusCodePages();
app.UseGatewaySecurityHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors(GatewayCorsExtensions.PolicyName);
app.UseGatewayCorrelation();
app.UseRateLimiter();
app.UseGatewayRequestBodyLimit(gatewayOptions);

app.MapReverseProxy();
app.MapDefaultEndpoints();

app.Run();

public partial class Program;
