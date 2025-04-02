namespace CredentialCheckerCli;

public interface IPostgresCredentialChecker
{
    Task<bool> IsCredentialsValid(DatabaseServer databaseServer);

    DatabaseServer CreateDatabaseCredentials(string postgresFlexServer, string databaseName, string postgresUser,
        string postgresPassword,
        string sslMode, int port);
}