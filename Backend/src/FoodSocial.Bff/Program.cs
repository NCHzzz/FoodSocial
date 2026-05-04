using FoodSocial.Bff;

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
builder.Services.AddBffAuthentication(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapBffAuthEndpoints();
app.MapBffUserProfileEndpoints();
app.MapBffDishEndpoints();
app.MapBffPlaceEndpoints();
app.MapBffPostEndpoints();
app.MapBffSearchAssistEndpoints();

app.MapDefaultEndpoints();

app.Run();

public partial class Program;
