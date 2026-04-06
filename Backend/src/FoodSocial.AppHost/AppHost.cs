var builder = DistributedApplication.CreateBuilder(args);

//PostgreSQL
var postgres = builder
    .AddPostgres("postgres")
    .WithDataVolume();

var identityDb = postgres.AddDatabase("identitydb");
var userprofileDb = postgres.AddDatabase("userprofiledb");
var dishDb = postgres.AddDatabase("dishdb");
var postDb = postgres.AddDatabase("postdb");

//RabbitMQ
var rabbitMq = builder
    .AddRabbitMQ("rabbitmq")
    .WithDataVolume();

// Redis
var redis = builder
    .AddRedis("redis")
    .WithDataVolume();

// Services
var bff = builder.AddProject<Projects.FoodSocial_Bff>("bff");
var identityApi = builder
    .AddProject<Projects.FoodSocial_Identity_Api>("identity-api")
    .WithReference(identityDb)
    .WithReference(rabbitMq)
    .WaitFor(identityDb)
    .WaitFor(rabbitMq);

var userProfileApi = builder
    .AddProject<Projects.FoodSocial_UserProfile_Api>("userprofile-api")
    .WithReference(userprofileDb)
    .WithReference(rabbitMq)
    .WaitFor(userprofileDb)
    .WaitFor(rabbitMq);

var dishApi = builder
    .AddProject<Projects.FoodSocial_Dish_Api>("dish-api")
    .WithReference(dishDb)
    .WithReference(rabbitMq)
    .WaitFor(dishDb)
    .WaitFor(rabbitMq);

var postApi = builder
    .AddProject<Projects.FoodSocial_Post_Api>("post-api")
    .WithReference(postDb)
    .WithReference(rabbitMq)
    .WaitFor(postDb)
    .WaitFor(rabbitMq);

bff.WithReference(identityApi)
    .WithReference(userProfileApi)
    .WithReference(dishApi)
    .WithReference(postApi)
    .WithReference(redis)
    .WaitFor(identityApi)
    .WaitFor(userProfileApi)
    .WaitFor(dishApi)
    .WaitFor(postApi);

builder.Build().Run();
