using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

if (builder.Environment.IsProduction())
{
    throw new InvalidOperationException(
        "FoodSocial.AppHost is for local orchestration only. Production must use environment-owned infrastructure and deployment manifests, not Aspire-managed local containers.");
}

var useStableDockerResourceNames = !bool.TryParse(
    builder.Configuration["FoodSocial:AppHost:UseStableDockerResourceNames"],
    out var configuredUseStableDockerResourceNames)
    || configuredUseStableDockerResourceNames;

var keycloakBffClientSecret = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(
    builder,
    "keycloak-bff-client-secret",
    special: false);
var keycloakDemoUserPassword = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(
    builder,
    "keycloak-demo-user-password",
    special: false);
var bffServiceClientSecret = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(
    builder,
    "keycloak-bff-service-client-secret",
    special: false);

//PostgreSQL
var postgres = builder.AddPostgres("postgres");
if (useStableDockerResourceNames)
{
    postgres
        .WithContainerName("foodsocial-postgres")
        .WithDataVolume("foodsocial-postgres-data")
        .WithLifetime(ContainerLifetime.Persistent);
}

var identityDb = postgres.AddDatabase("identitydb");
var userProfileDb = postgres.AddDatabase("userprofiledb");
var dishDb = postgres.AddDatabase("dishdb");
var placeDb = postgres.AddDatabase("placedb");
var postDb = postgres.AddDatabase("postdb");

//RabbitMQ
var rabbitMq = builder.AddRabbitMQ("rabbitmq");
if (useStableDockerResourceNames)
{
    rabbitMq
        .WithContainerName("foodsocial-rabbitmq")
        .WithDataVolume("foodsocial-rabbitmq-data")
        .WithLifetime(ContainerLifetime.Persistent);
}

// Redis
var redis = builder.AddRedis("redis");
if (useStableDockerResourceNames)
{
    redis
        .WithContainerName("foodsocial-redis")
        .WithDataVolume("foodsocial-redis-data")
        .WithLifetime(ContainerLifetime.Persistent);
}

// Keycloak
var keycloak = builder.AddKeycloak("keycloak");
if (useStableDockerResourceNames)
{
    keycloak
        .WithContainerName("foodsocial-keycloak")
        .WithLifetime(ContainerLifetime.Persistent);
}

keycloak
    .WithEnvironment("KEYCLOAK_BFF_CLIENT_SECRET", keycloakBffClientSecret)
    .WithEnvironment("KEYCLOAK_BFF_SERVICE_CLIENT_SECRET", bffServiceClientSecret)
    .WithEnvironment("KEYCLOAK_DEMO_USER_PASSWORD", keycloakDemoUserPassword)
    .WithRealmImport("./keycloak/realms");

// Services
var bff = builder
    .AddProject<Projects.FoodSocial_Bff>("bff", launchProfileName: "https")
    .WithEndpoint("http", endpoint => endpoint.Port = 5276)
    .WithEndpoint("https", endpoint => endpoint.Port = 7187)
    .WithEnvironment("Keycloak__BffClientSecret", keycloakBffClientSecret)
    .WithEnvironment("Keycloak__ServiceClientId", "foodsocial-bff-service")
    .WithEnvironment("Keycloak__ServiceClientSecret", bffServiceClientSecret)
    .WithReference(keycloak)
    .WithReference(redis)
    .WaitFor(keycloak)
    .WaitFor(redis);

var identityApi = builder
    .AddProject<Projects.FoodSocial_Identity_Api>("identity-api")
    .WithEnvironment("Identity__TrustedServiceClients__BffClientId", "foodsocial-bff-service")
    .WithReference(identityDb)
    .WithReference(keycloak)
    .WithReference(rabbitMq)
    .WaitFor(postgres)
    .WaitFor(keycloak)
    .WaitFor(rabbitMq);

var userProfileApi = builder
    .AddProject<Projects.FoodSocial_UserProfile_Api>("userprofile-api")
    .WithEnvironment("Keycloak__Audience", "foodsocial-api")
    .WithEnvironment("Keycloak__Realm", "foodsocial")
    .WithReference(userProfileDb)
    .WithReference(keycloak)
    .WithReference(rabbitMq)
    .WaitFor(postgres)
    .WaitFor(keycloak)
    .WaitFor(rabbitMq);

var dishApi = builder
    .AddProject<Projects.FoodSocial_Dish_Api>("dish-api")
    .WithReference(dishDb)
    .WithReference(rabbitMq)
    .WaitFor(postgres)
    .WaitFor(rabbitMq);

var placeApi = builder
    .AddProject<Projects.FoodSocial_Place_Api>("place-api")
    .WithReference(placeDb)
    .WithReference(rabbitMq)
    .WaitFor(postgres)
    .WaitFor(rabbitMq);

var postApi = builder
    .AddProject<Projects.FoodSocial_Post_Api>("post-api")
    .WithEnvironment("Keycloak__Audience", "foodsocial-api")
    .WithEnvironment("Keycloak__Realm", "foodsocial")
    .WithEnvironment("Post__TrustedServiceClients__BffClientId", "foodsocial-bff-service")
    .WithReference(postDb)
    .WithReference(keycloak)
    .WithReference(rabbitMq)
    .WithReference(dishApi)
    .WithReference(placeApi)
    .WaitFor(postgres)
    .WaitFor(keycloak)
    .WaitFor(rabbitMq)
    .WaitFor(dishApi)
    .WaitFor(placeApi);

bff.WithReference(identityApi)
    .WithReference(userProfileApi)
    .WithReference(dishApi)
    .WithReference(placeApi)
    .WithReference(postApi)
    .WaitFor(identityApi)
    .WaitFor(userProfileApi)
    .WaitFor(dishApi)
    .WaitFor(placeApi)
    .WaitFor(postApi);

var apiGateway = builder
    .AddProject<Projects.FoodSocial_ApiGateway>("api-gateway", launchProfileName: "https")
    .WithEndpoint("http", endpoint => endpoint.Port = 5280)
    .WithEndpoint("https", endpoint => endpoint.Port = 7280)
    .WithEnvironment("Gateway__Cors__AllowedOrigins__0", "http://localhost:4200")
    .WithReference(bff)
    .WaitFor(bff);

builder.Build().Run();
