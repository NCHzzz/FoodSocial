using Microsoft.Extensions.DependencyInjection;

namespace FoodSocial.Identity.Application;

/// <summary>
/// Registers Identity application services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Identity application services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<EnsureIdentityCommandHandler>();
        services.AddScoped<CompleteUserProfileBootstrapService>();

        return services;
    }
}
