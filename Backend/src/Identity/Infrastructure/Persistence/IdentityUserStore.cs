using FoodSocial.Identity.Application;
using FoodSocial.Contracts.Identity;
using FoodSocial.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FoodSocial.Identity.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core implementation of the identity user store.
/// </summary>
public sealed class IdentityUserStore(IdentityDbContext dbContext) : IIdentityUserStore
{
    private readonly IdentityDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    /// <inheritdoc />
    public Task<IdentityUser?> FindBySubjectAsync(string subject, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        return _dbContext.IdentityUsers.SingleOrDefaultAsync(
            user => user.TenantId == IdentityUser.DefaultTenantId && user.Subject == subject,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<IdentityUser?> FindByIdAsync(Guid identityId, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(identityId, Guid.Empty);

        return _dbContext.IdentityUsers.SingleOrDefaultAsync(
            user => user.TenantId == IdentityUser.DefaultTenantId && user.Id == identityId,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task AddAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        return _dbContext.IdentityUsers.AddAsync(user, cancellationToken).AsTask();
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateSubjectViolation(exception))
        {
            var subject = GetPendingDuplicateSubject() ?? "unknown-subject";
            throw new DuplicateIdentitySubjectException(subject, exception);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetRoleNamesBySubjectAsync(string subject, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        var assignedRoles = await _dbContext.IdentityRoleAssignments
            .Where(assignment => assignment.TenantId == IdentityRoleAssignment.DefaultTenantId && assignment.Subject == subject)
            .Select(assignment => assignment.Role)
            .ToListAsync(cancellationToken);

        if (assignedRoles.Count == 0)
        {
            return [IdentityAppRoles.User];
        }

        return assignedRoles
            .Select(IdentityAppRoles.Normalize)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(role => role, StringComparer.Ordinal)
            .ToArray();
    }

    /// <inheritdoc />
    public async Task<bool> EnsureRoleAssignedAsync(string subject, string role, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        var normalizedRole = IdentityAppRoles.Normalize(role);

        var existingAssignment = await _dbContext.IdentityRoleAssignments
            .SingleOrDefaultAsync(
                assignment => assignment.TenantId == IdentityRoleAssignment.DefaultTenantId && assignment.Subject == subject && assignment.Role == normalizedRole,
                cancellationToken);

        if (existingAssignment is not null)
        {
            return false;
        }

        await _dbContext.IdentityRoleAssignments.AddAsync(new IdentityRoleAssignment(subject, normalizedRole), cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public Task AddOutboxMessageAsync(UserProvisionedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var outboxMessage = IdentityOutboxMessage.Create(integrationEvent);
        return _dbContext.IdentityOutboxMessages.AddAsync(outboxMessage, cancellationToken).AsTask();
    }

    /// <inheritdoc />
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
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

    private bool IsDuplicateSubjectViolation(DbUpdateException exception)
    {
        if (IsDuplicateRoleAssignmentViolation(exception.InnerException))
        {
            return false;
        }

        if (IsSqliteUniqueViolation(exception.InnerException))
        {
            return true;
        }

        return exception.InnerException is PostgresException postgresException
            && postgresException.SqlState == PostgresErrorCodes.UniqueViolation
            && string.Equals(postgresException.ConstraintName, "IX_identity_users_TenantId_Subject", StringComparison.Ordinal);
    }

    private static bool IsDuplicateRoleAssignmentViolation(Exception? innerException) =>
        innerException is PostgresException postgresException
        && postgresException.SqlState == PostgresErrorCodes.UniqueViolation
        && string.Equals(
            postgresException.ConstraintName,
            "IX_identity_role_assignments_TenantId_Subject_Role",
            StringComparison.Ordinal);

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

    private string? GetPendingDuplicateSubject() =>
        _dbContext.ChangeTracker.Entries<IdentityUser>()
            .Where(entry => entry.State == EntityState.Added)
            .Select(entry => entry.Entity.Subject)
            .SingleOrDefault();
}
