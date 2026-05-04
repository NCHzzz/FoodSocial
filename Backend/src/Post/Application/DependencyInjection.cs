using Microsoft.Extensions.DependencyInjection;

namespace FoodSocial.Post.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<PostService>();
        return services;
    }
}
