namespace ImageRecognition.Infrastructure.Database;

/// <summary>
/// Параметры подключения к PostgreSQL.
/// </summary>
public sealed class PostgresOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5432;
    public string Database { get; set; } = "Image_recognition_db";
    public string Username { get; set; } = "postgres";
    public string Password { get; set; } = "10112275";

    public string ToConnectionString(string? databaseOverride = null)
    {
        string database = string.IsNullOrWhiteSpace(databaseOverride) ? Database : databaseOverride;
        return $"Host={Host};Port={Port};Database={database};Username={Username};Password={Password}";
    }
}

