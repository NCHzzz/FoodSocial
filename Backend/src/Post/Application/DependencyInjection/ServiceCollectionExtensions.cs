using Microsoft.Extensions.DependencyInjection;
using FoodSocial.Post.Application.Mapping;
using FoodSocial.Post.Application.UseCases;
using FoodSocial.Post.Application.Validation;

namespace FoodSocial.Post.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<PostPublishingUseCase>();
        services.AddScoped<PostQueryUseCase>();
        services.AddScoped<PostEngagementUseCase>();
        services.AddScoped<PostCommentUseCase>();
        services.AddScoped<PostRepostUseCase>();
        services.AddScoped<PostReportUseCase>();
        services.AddScoped<PostNotificationUseCase>();
        services.AddScoped<PostMediaUploadUseCase>();
        services.AddScoped<PostResponseMapper>();
        services.AddScoped<PostMediaAttachmentValidator>();
        services.AddScoped<PostService>();
        return services;
    }
}
