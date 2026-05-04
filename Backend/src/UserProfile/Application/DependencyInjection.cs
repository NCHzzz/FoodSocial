using Microsoft.Extensions.DependencyInjection;

namespace FoodSocial.UserProfile.Application;

/// <summary>
/// Registers UserProfile application services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds UserProfile application services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<UserProfileBootstrapService>();
        services.AddScoped<UserProfileService>();
        services.AddScoped<UserFollowService>();
        services.AddScoped<UserSavedItemsService>();
        services.AddScoped<UserNotificationService>();
        return services;
    }
}
