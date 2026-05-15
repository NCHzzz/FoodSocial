using FoodSocial.Contracts.UserProfiles;
using FoodSocial.Messaging;
using FoodSocial.UserProfile.Application;
using FoodSocial.UserProfile.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FoodSocial.UserProfile.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core implementation of the profile shell store.
/// </summary>
public sealed class UserProfileShellStore(UserProfileDbContext dbContext) : IUserProfileShellStore
{
    private readonly UserProfileDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    /// <inheritdoc />
    public Task<UserProfileShell?> FindByIdentityIdAsync(Guid identityId, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(identityId, Guid.Empty);

        return _dbContext.UserProfileShells.SingleOrDefaultAsync(
            shell => shell.TenantId == UserProfileShell.DefaultTenantId && shell.IdentityId == identityId,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<UserProfileShell?> FindBySubjectAsync(string subject, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        return _dbContext.UserProfileShells.SingleOrDefaultAsync(
            shell => shell.TenantId == UserProfileShell.DefaultTenantId && shell.Subject == subject,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task AddAsync(UserProfileShell shell, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(shell);
        _dbContext.IntegrationOutboxMessages.Add(CreateOutboxMessage(shell.IdentityId, "ProfileShellCreated"));
        return _dbContext.UserProfileShells.AddAsync(shell, cancellationToken).AsTask();
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateIdentityViolation(exception))
        {
            var identityId = GetPendingIdentityId();
            throw new UserProfileBootstrapConflictException(identityId, exception);
        }
    }

    /// <inheritdoc />
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (_dbContext.Database.CurrentTransaction is not null)
        {
            return await operation(cancellationToken);
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var result = await operation(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private bool IsDuplicateIdentityViolation(DbUpdateException exception)
    {
        if (IsSqliteUniqueViolation(exception.InnerException))
        {
            return true;
        }

        return exception.InnerException is PostgresException postgresException
            && postgresException.SqlState == PostgresErrorCodes.UniqueViolation
            && (string.Equals(postgresException.ConstraintName, "IX_user_profile_shells_TenantId_IdentityId", StringComparison.Ordinal)
                || string.Equals(postgresException.ConstraintName, "IX_user_profile_shells_TenantId_Subject", StringComparison.Ordinal));
    }

    private static bool IsSqliteUniqueViolation(Exception? innerException)
    {
        if (innerException is null
            || !string.Equals(innerException.GetType().FullName, "Microsoft.Data.Sqlite.SqliteException", StringComparison.Ordinal))
        {
            return false;
        }

        var sqliteErrorCodeProperty = innerException.GetType().GetProperty("SqliteErrorCode");
        var sqliteErrorCode = sqliteErrorCodeProperty?.GetValue(innerException);
        return sqliteErrorCode is int code && code == 19;
    }

    private Guid GetPendingIdentityId() =>
        _dbContext.ChangeTracker.Entries<UserProfileShell>()
            .Where(entry => entry.State == EntityState.Added)
            .Select(entry => entry.Entity.IdentityId)
            .SingleOrDefault();

    private static IntegrationOutboxMessage CreateOutboxMessage(Guid identityId, string changeType, Guid? targetId = null, string? targetType = null)
    {
        var integrationEvent = UserProfileChangedIntegrationEvent.Create(
            identityId,
            UserProfileShell.DefaultTenantId,
            changeType,
            targetId,
            targetType);

        return IntegrationOutboxMessage.Create(
            integrationEvent,
            integrationEvent.EventId,
            integrationEvent.EventVersion,
            integrationEvent.OccurredAtUtc,
            integrationEvent.CorrelationId,
            "userprofile.events",
            $"userprofile.profile.{changeType.ToLowerInvariant()}.v1",
            identityId,
            "UserProfile");
    }
}
