using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

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
var identityInternalTrustSharedSecret = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(
    builder,
    "identity-internal-trust-shared-secret",
    special: false);

//PostgreSQL
var postgres = builder.AddPostgres("postgres");
if (useStableDockerResourceNames)
{
    postgres
        .WithContainerName("foodsocial-postgres")
        .WithDataVolume("foodsocial-postgres-data");
}
else
{
    postgres.WithDataVolume();
}

postgres.WithLifetime(ContainerLifetime.Persistent);

var identityDb = postgres.AddDatabase("identitydb");
var userprofileDb = postgres.AddDatabase("userprofiledb");
var dishDb = postgres.AddDatabase("dishdb");
var placeDb = postgres.AddDatabase("placedb");
var postDb = postgres.AddDatabase("postdb");

//RabbitMQ
var rabbitMq = builder.AddRabbitMQ("rabbitmq");
if (useStableDockerResourceNames)
{
    rabbitMq
        .WithContainerName("foodsocial-rabbitmq")
        .WithDataVolume("foodsocial-rabbitmq-data");
}
else
{
    rabbitMq.WithDataVolume();
}

rabbitMq.WithLifetime(ContainerLifetime.Persistent);

// Redis
var redis = builder.AddRedis("redis");
if (useStableDockerResourceNames)
{
    redis
        .WithContainerName("foodsocial-redis")
        .WithDataVolume("foodsocial-redis-data");
}
else
{
    redis.WithDataVolume();
}

redis.WithLifetime(ContainerLifetime.Persistent);

// Keycloak
var keycloak = builder.AddKeycloak("keycloak");
if (useStableDockerResourceNames)
{
    keycloak.WithContainerName("foodsocial-keycloak");
}

keycloak
    .WithEnvironment("KEYCLOAK_BFF_CLIENT_SECRET", keycloakBffClientSecret)
    .WithEnvironment("KEYCLOAK_DEMO_USER_PASSWORD", keycloakDemoUserPassword)
    .WithRealmImport("./keycloak/realms")
    .WithLifetime(ContainerLifetime.Persistent);

// Services
var bff = builder
    .AddProject<Projects.FoodSocial_Bff>("bff", launchProfileName: "https")
    .WithEndpoint("http", endpoint => endpoint.Port = 5276)
    .WithEndpoint("https", endpoint => endpoint.Port = 7187)
    .WithEnvironment("Identity__InternalTrust__ServiceId", "foodsocial-bff")
    .WithEnvironment("Identity__InternalTrust__SharedSecret", identityInternalTrustSharedSecret)
    .WithEnvironment("Post__InternalTrust__SharedSecret", identityInternalTrustSharedSecret)
    .WithEnvironment("Keycloak__BffClientSecret", keycloakBffClientSecret)
    .WithReference(keycloak)
    .WithReference(redis)
    .WaitFor(keycloak);

var identityApi = builder
    .AddProject<Projects.FoodSocial_Identity_Api>("identity-api")
    .WithEnvironment("Identity__InternalTrust__ServiceId", "foodsocial-bff")
    .WithEnvironment("Identity__InternalTrust__SharedSecret", identityInternalTrustSharedSecret)
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
    .WithReference(userprofileDb)
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
    .WithEnvironment("Post__InternalTrust__SharedSecret", identityInternalTrustSharedSecret)
    .WithReference(postDb)
    .WithReference(dishApi)
    .WithReference(placeApi)
    .WithReference(rabbitMq)
    .WaitFor(postgres)
    .WaitFor(dishApi)
    .WaitFor(placeApi)
    .WaitFor(rabbitMq);

bff.WithReference(identityApi)
    .WithReference(userProfileApi)
    .WithReference(dishApi)
    .WithReference(placeApi)
    .WithReference(postApi);

builder.Build().Run();
