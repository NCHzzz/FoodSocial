using Microsoft.Extensions.DependencyInjection;
using FoodSocial.Post.Application.Mapping;
using FoodSocial.Post.Application.Validation;
using FoodSocial.Post.Application.UseCases.Comments;

using FoodSocial.Post.Application.UseCases.Engagement;
using FoodSocial.Post.Application.UseCases.Media;
using FoodSocial.Post.Application.UseCases.Notifications;


using FoodSocial.Post.Application.UseCases.Publishing;
using FoodSocial.Post.Application.UseCases.Queries;
using FoodSocial.Post.Application.UseCases.Reposts;
using FoodSocial.Post.Application.UseCases.Reports;


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


        return services;
    }
}
