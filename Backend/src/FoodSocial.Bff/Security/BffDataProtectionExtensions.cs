using Microsoft.AspNetCore.DataProtection;

namespace FoodSocial.Bff;

internal static class BffDataProtectionExtensions
{
    private const string ApplicationName = "FoodSocial.Bff";

    public static IServiceCollection AddBffDataProtection(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var keyRingPath = configuration["Bff:DataProtection:KeyRingPath"];
        var builder = services.AddDataProtection().SetApplicationName(ApplicationName);

        if (!string.IsNullOrWhiteSpace(keyRingPath))
        {
            builder.PersistKeysToFileSystem(new DirectoryInfo(keyRingPath));
            return services;
        }

        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException("Bff:DataProtection:KeyRingPath must be configured outside development so BFF cookies and antiforgery tokens survive restarts and multiple replicas.");
        }

        return services;
    }
}
