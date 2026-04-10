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

        return _dbContext.UserProfileShells.SingleOrDefaultAsync(shell => shell.IdentityId == identityId, cancellationToken);
    }

    /// <inheritdoc />
    public Task AddAsync(UserProfileShell shell, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(shell);
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
            throw new UserProfileBootstrapDuplicateIdentityException(identityId, exception);
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
            && (string.Equals(postgresException.ConstraintName, "IX_user_profile_shells_IdentityId", StringComparison.Ordinal)
                || string.Equals(postgresException.ConstraintName, "IX_user_profile_shells_Subject", StringComparison.Ordinal));
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
}
