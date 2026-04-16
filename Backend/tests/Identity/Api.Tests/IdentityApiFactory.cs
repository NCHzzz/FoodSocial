using FoodSocial.Identity.Application;
using FoodSocial.Identity.Domain;
using FoodSocial.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodSocial.Identity.Api.Tests;

internal sealed class IdentityApiFactory : WebApplicationFactory<Program>
{
    private const string TestIdentityInternalTrustSharedSecret = "foodsocial-bff-test-shared-secret";
    private readonly TestIdentityUserStore _testIdentityUserStore = new();

    public IdentityApiFactory()
    {
        Environment.SetEnvironmentVariable("Keycloak__Realm", "foodsocial");
        Environment.SetEnvironmentVariable("Keycloak__Authority", "https://identity.example.test/realms/foodsocial");
        Environment.SetEnvironmentVariable("ConnectionStrings__identitydb", "Host=localhost;Database=identity_api_tests;Username=test;Password=test");
        Environment.SetEnvironmentVariable("Identity__ApplyMigrationsOnStartup", bool.FalseString);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Identity:InternalTrust:ServiceId", "foodsocial-bff");
        builder.UseSetting("Identity:InternalTrust:SharedSecret", TestIdentityInternalTrustSharedSecret);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IIdentityUserStore>();
            services.AddSingleton<IIdentityUserStore>(_testIdentityUserStore);

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.SchemeName,
                    _ => { });
        });
    }

    internal void SeedRolesForSubject(string subject, params string[] roles) =>
        _testIdentityUserStore.SeedRoles(subject, roles);

    internal void SeedIdentity(string subject, string? preferredUsername, string? email, IdentityLifecycleState state) =>
        _testIdentityUserStore.SeedIdentity(subject, preferredUsername, email, state);

    private sealed class TestIdentityUserStore : IIdentityUserStore
    {
        private readonly Lock _syncRoot = new();
        private readonly List<IdentityUser> _users = [];
        private readonly Dictionary<string, IReadOnlyList<string>> _subjectRoles = new(StringComparer.Ordinal);

        public Task<IdentityUser?> FindBySubjectAsync(string subject, CancellationToken cancellationToken)
        {
            lock (_syncRoot)
            {
                var user = _users.SingleOrDefault(candidate => string.Equals(candidate.Subject, subject, StringComparison.Ordinal));
                return Task.FromResult(user);
            }
        }

        public Task<IdentityUser?> FindByIdAsync(Guid identityId, CancellationToken cancellationToken)
        {
            lock (_syncRoot)
            {
                var user = _users.SingleOrDefault(candidate => candidate.Id == identityId);
                return Task.FromResult(user);
            }
        }

        public Task AddAsync(IdentityUser user, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(user);

            lock (_syncRoot)
            {
                _users.Add(user);
            }

            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public void SeedRoles(string subject, IReadOnlyList<string> roles)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(subject);

            lock (_syncRoot)
            {
                _subjectRoles[subject] = [.. roles];

                var existing = _users.SingleOrDefault(candidate => string.Equals(candidate.Subject, subject, StringComparison.Ordinal));

                if (existing is null)
                {
                    _users.Add(new IdentityUser(subject, preferredUsername: null, email: null));
                }
            }
        }

        public void SeedIdentity(string subject, string? preferredUsername, string? email, IdentityLifecycleState state)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(subject);

            lock (_syncRoot)
            {
                var existing = _users.SingleOrDefault(candidate => string.Equals(candidate.Subject, subject, StringComparison.Ordinal));
                if (existing is not null)
                {
                    existing.ReconcileProfile(preferredUsername, email);
                    ApplyState(existing, state);
                    return;
                }

                var user = new IdentityUser(subject, preferredUsername, email);
                ApplyState(user, state);
                _users.Add(user);
            }
        }

        public Task<IReadOnlyList<string>> GetRoleNamesBySubjectAsync(string subject, CancellationToken cancellationToken)
        {
            lock (_syncRoot)
            {
                if (_subjectRoles.TryGetValue(subject, out var roles))
                {
                    return Task.FromResult(roles);
                }

                return Task.FromResult<IReadOnlyList<string>>(["user"]);
            }
        }

        public Task<bool> EnsureRoleAssignedAsync(string subject, string role, CancellationToken cancellationToken) =>
            StageRoleAssignmentAsync(subject, role);

        public Task AddOutboxMessageAsync(FoodSocial.Contracts.Identity.UserProvisionedIntegrationEvent integrationEvent, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(operation);
            return operation(cancellationToken);
        }

        private Task<bool> StageRoleAssignmentAsync(string subject, string role)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(subject);

            lock (_syncRoot)
            {
                var normalizedRole = IdentityAppRoles.Normalize(role);

                if (!_subjectRoles.TryGetValue(subject, out var roles))
                {
                    _subjectRoles[subject] = [IdentityAppRoles.User, normalizedRole];
                    return Task.FromResult(true);
                }

                if (roles.Contains(normalizedRole, StringComparer.Ordinal))
                {
                    return Task.FromResult(false);
                }

                _subjectRoles[subject] = [.. roles, normalizedRole];
                return Task.FromResult(true);
            }
        }

        private static void ApplyState(IdentityUser user, IdentityLifecycleState targetState)
        {
            ArgumentNullException.ThrowIfNull(user);

            while (user.State != targetState)
            {
                var transition = (user.State, targetState) switch
                {
                    (IdentityLifecycleState.PendingBootstrap, IdentityLifecycleState.Active) => IdentityLifecycleTransition.Activate,
                    (IdentityLifecycleState.PendingBootstrap, IdentityLifecycleState.Suspended) => IdentityLifecycleTransition.Suspend,
                    (IdentityLifecycleState.PendingBootstrap, IdentityLifecycleState.Deleted) => IdentityLifecycleTransition.Delete,
                    (IdentityLifecycleState.Active, IdentityLifecycleState.Suspended) => IdentityLifecycleTransition.Suspend,
                    (IdentityLifecycleState.Active, IdentityLifecycleState.Deleted) => IdentityLifecycleTransition.Delete,
                    (IdentityLifecycleState.Suspended, IdentityLifecycleState.Active) => IdentityLifecycleTransition.Activate,
                    (IdentityLifecycleState.Suspended, IdentityLifecycleState.Deleted) => IdentityLifecycleTransition.Delete,
                    _ => throw new InvalidOperationException($"Unsupported test state transition from '{user.State}' to '{targetState}'.")
                };

                user.TransitionTo(transition);
            }
        }
    }
}
