using Npgsql;

namespace FoodSocial.AppHost.Tests;

internal sealed class AppHostDatabaseVerifiers(string identityConnectionString, string userProfileConnectionString)
{
    public PostgreSqlDatabaseVerifier Identity { get; } = new(identityConnectionString, "identitydb");

    public PostgreSqlDatabaseVerifier UserProfile { get; } = new(userProfileConnectionString, "userprofiledb");
}

internal sealed class PostgreSqlDatabaseVerifier(string connectionString, string? databaseName = null)
{
    private readonly string _connectionString = BuildScopedConnectionString(connectionString, databaseName);

    private static string BuildScopedConnectionString(string connectionString, string? databaseName) => string.IsNullOrWhiteSpace(connectionString)
        ? throw new ArgumentException("A PostgreSQL connection string is required.", nameof(connectionString))
        : string.IsNullOrWhiteSpace(databaseName)
            ? connectionString
            : new NpgsqlConnectionStringBuilder(connectionString)
            {
                Database = databaseName
            }.ConnectionString;

    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection.FullState.HasFlag(System.Data.ConnectionState.Open);
    }

    public async Task<long> CountRowsAsync(string tableName, string? schemaName = null, CancellationToken cancellationToken = default)
    {
        var qualifiedTableName = QualifyTableName(tableName, schemaName);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand($"SELECT COUNT(*) FROM {qualifiedTableName};", connection);
        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(scalar, System.Globalization.CultureInfo.InvariantCulture);
    }

    public async Task<long> CountRowsByColumnValueAsync(
        string tableName,
        string columnName,
        object value,
        string? schemaName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(columnName);
        ArgumentNullException.ThrowIfNull(value);

        var qualifiedTableName = QualifyTableName(tableName, schemaName);
        var quotedColumnName = QuoteIdentifier(columnName);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(
            $"SELECT COUNT(*) FROM {qualifiedTableName} WHERE {quotedColumnName} = @value;",
            connection);
        command.Parameters.AddWithValue("value", value);

        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(scalar, System.Globalization.CultureInfo.InvariantCulture);
    }

    public async Task DeleteRowsByColumnValueAsync(
        string tableName,
        string columnName,
        object value,
        string? schemaName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(columnName);
        ArgumentNullException.ThrowIfNull(value);

        var qualifiedTableName = QualifyTableName(tableName, schemaName);
        var quotedColumnName = QuoteIdentifier(columnName);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(
            $"DELETE FROM {qualifiedTableName} WHERE {quotedColumnName} = @value;",
            connection);
        command.Parameters.AddWithValue("value", value);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> TableExistsAsync(string tableName, string? schemaName = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(
            """
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.tables
                WHERE table_schema = @schemaName
                  AND table_name = @tableName);
            """,
            connection);

        command.Parameters.AddWithValue("schemaName", schemaName ?? "public");
        command.Parameters.AddWithValue("tableName", tableName);

        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        return scalar is true;
    }

    public async Task<T?> ExecuteScalarAsync<T>(
        string sql,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        AddParameters(command, parameters);

        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        if (scalar is null || scalar is DBNull)
        {
            return default;
        }

        return (T?)Convert.ChangeType(scalar, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
    }

    public async Task<int> ExecuteNonQueryAsync(
        string sql,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        AddParameters(command, parameters);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string QualifyTableName(string tableName, string? schemaName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            throw new ArgumentException("A table name is required.", nameof(tableName));
        }

        return string.IsNullOrWhiteSpace(schemaName)
            ? QuoteIdentifier(tableName)
            : $"{QuoteIdentifier(schemaName)}.{QuoteIdentifier(tableName)}";
    }

    private static void AddParameters(NpgsqlCommand command, IReadOnlyDictionary<string, object?>? parameters)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (parameters is null)
        {
            return;
        }

        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }
    }

    private static string QuoteIdentifier(string identifier) =>
        $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}
