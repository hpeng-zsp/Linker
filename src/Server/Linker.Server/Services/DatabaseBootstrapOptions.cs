namespace Linker.Server.Services;

public sealed class DatabaseBootstrapOptions
{
    public string Type { get; set; } = "Sqlite";
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 3306;
    public string DatabaseName { get; set; } = "linker";
    public string Username { get; set; } = "root";
    public string Password { get; set; } = "root";
    public string DataDirectory { get; set; } = "data";
    public string ConnectionString { get; set; } = "Data Source=data/linker.db";

    public static DatabaseBootstrapOptions From(
        string? databaseType,
        string? host,
        int? port,
        string? databaseName,
        string? username,
        string? password,
        string? dataDirectory = null)
    {
        var type = string.IsNullOrWhiteSpace(databaseType) ? "Sqlite" : databaseType.Trim();
        var database = string.IsNullOrWhiteSpace(databaseName) ? "linker" : databaseName.Trim();
        var config = new DatabaseBootstrapOptions
        {
            Type = type,
            Host = string.IsNullOrWhiteSpace(host) ? "127.0.0.1" : host.Trim(),
            Port = port ?? 3306,
            DatabaseName = database,
            Username = string.IsNullOrWhiteSpace(username) ? "root" : username.Trim(),
            Password = string.IsNullOrWhiteSpace(password) ? "root" : password.Trim(),
            DataDirectory = string.IsNullOrWhiteSpace(dataDirectory) ? "data" : dataDirectory.Trim(),
        };

        config.ConnectionString = config.BuildConnectionString();
        return config;
    }

    public string BuildConnectionString()
    {
        switch (Type)
        {
            case "Sqlite":
            {
                var rootDir = Directory.GetCurrentDirectory();
                var dir = string.IsNullOrWhiteSpace(DataDirectory) ? "data" : DataDirectory;
                var fullDir = Path.Combine(rootDir, dir);
                Directory.CreateDirectory(fullDir);
                return $"Data Source={Path.Combine(fullDir, $"{DatabaseName}.db")};";
            }
            case "MySql":
                return $"Server={Host};Port={Port};Database={DatabaseName};Uid={Username};Pwd={Password};Charset=utf8mb4;";
            case "PostgreSQL":
                return $"Host={Host};Port={Port};Database={DatabaseName};Username={Username};Password={Password};";
            case "SqlServer":
                return $"Server={Host},{Port};Database={DatabaseName};User Id={Username};Password={Password};TrustServerCertificate=True;";
            default:
                return $"Data Source={Path.Combine(Directory.GetCurrentDirectory(), "data", $"{DatabaseName}.db")};";
        }
    }
}
