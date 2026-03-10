using Npgsql;

namespace ImageRecognition.Infrastructure.Database;

/// <summary>
/// Простой фабричный класс для создания подключений к PostgreSQL.
/// Не использует ORM и сложные фреймворки — только чистый Npgsql.
/// </summary>
public sealed class PostgresConnectionFactory
{
    private readonly PostgresOptions _options;

    public PostgresConnectionFactory(PostgresOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public NpgsqlConnection CreateOpenConnection()
    {
        var connection = new NpgsqlConnection(_options.ToConnectionString());
        connection.Open();
        return connection;
    }
}

