var builder = DistributedApplication.CreateBuilder(args);

var keycloakBffClientSecret = builder.AddParameter("keycloak-bff-client-secret", secret: true);
var keycloakDemoUserPassword = builder.AddParameter("keycloak-demo-user-password", secret: true);
var identityInternalTrustSharedSecret = builder.AddParameter("identity-internal-trust-shared-secret", secret: true);

//PostgreSQL
var postgres = builder
    .AddPostgres("postgres")
    .WithLifetime(ContainerLifetime.Session);

var identityDb = postgres.AddDatabase("identitydb");
var userprofileDb = postgres.AddDatabase("userprofiledb");
var dishDb = postgres.AddDatabase("dishdb");
var postDb = postgres.AddDatabase("postdb");

identityDb.WithCreationScript(
    """
    CREATE DATABASE identitydb
        ENCODING = 'UTF8';
    """);

userprofileDb.WithCreationScript(
    """
    CREATE DATABASE userprofiledb
        ENCODING = 'UTF8';
    """);

dishDb.WithCreationScript(
    """
    CREATE DATABASE dishdb
        ENCODING = 'UTF8';
    """);

postDb.WithCreationScript(
    """
    CREATE DATABASE postdb
        ENCODING = 'UTF8';
    """);

//RabbitMQ
var rabbitMq = builder
    .AddRabbitMQ("rabbitmq")
    .WithLifetime(ContainerLifetime.Session);

// Redis
var redis = builder
    .AddRedis("redis")
    .WithLifetime(ContainerLifetime.Session);

// Keycloak
var keycloak = builder
    .AddKeycloak("keycloak", port: 8080)
    .WithEnvironment("KEYCLOAK_BFF_CLIENT_SECRET", keycloakBffClientSecret)
    .WithEnvironment("KEYCLOAK_DEMO_USER_PASSWORD", keycloakDemoUserPassword)
    .WithRealmImport("./keycloak/realms");

// Services
var bff = builder
    .AddProject<Projects.FoodSocial_Bff>("bff", launchProfileName: "https")
    .WithEndpoint("http", endpoint => endpoint.Port = 5276)
    .WithEndpoint("https", endpoint => endpoint.Port = 7187)
    .WithEnvironment("Identity__InternalTrust__ServiceId", "foodsocial-bff")
    .WithEnvironment("Identity__InternalTrust__SharedSecret", identityInternalTrustSharedSecret)
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
    .WithReference(userprofileDb)
    .WithReference(rabbitMq)
    .WaitFor(postgres)
    .WaitFor(rabbitMq);

var dishApi = builder
    .AddProject<Projects.FoodSocial_Dish_Api>("dish-api")
    .WithReference(dishDb)
    .WithReference(rabbitMq)
    .WaitFor(postgres)
    .WaitFor(rabbitMq);

var postApi = builder
    .AddProject<Projects.FoodSocial_Post_Api>("post-api")
    .WithReference(postDb)
    .WithReference(rabbitMq)
    .WaitFor(postgres)
    .WaitFor(rabbitMq);

bff.WithReference(identityApi)
    .WithReference(userProfileApi)
    .WithReference(dishApi)
    .WithReference(postApi);

builder.Build().Run();
