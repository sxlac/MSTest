using Npgsql;

namespace CredentialCheckerCli;

public class PostgresCredentialChecker : IPostgresCredentialChecker
{
    public DatabaseServer CreateDatabaseCredentials(string postgresFlexServer, string databaseName, string postgresUser, string postgresPassword,
        string sslMode, int port)
    {
        var _databaseServer = new DatabaseServer();
        _databaseServer.Database = databaseName;
        _databaseServer.Server = postgresFlexServer;
        _databaseServer.User = postgresUser;
        _databaseServer.Password = postgresPassword;
        _databaseServer.Mode = sslMode;
        _databaseServer.Port = port;

        return _databaseServer;
    }
    
    public async Task<bool> IsCredentialsValid(DatabaseServer databaseServer)
    {
        try
        {
            var connectionString = $"host={databaseServer.Server}.postgres.database.azure.com;database={databaseServer.Database};username={databaseServer.User};password={databaseServer.Password};SslMode={databaseServer.Mode};Port={databaseServer.Port}";

            using var connection = new NpgsqlConnection();
            connection.ConnectionString = connectionString;
            connection.Open();
            await connection.CloseAsync();
            return true;
        } 
        catch (Exception ex) {
            Console.WriteLine($"Exception while trying to connect to database {ex}");
            return false;
        }
    }
}