using CredentialCheckerCli.Commands;
using FakeItEasy;
using Npgsql;
using Xunit;

namespace CredentialCheckerCli.Tests.Services;

public class PostgresCredentialCheckerTests
{
    private readonly IPostgresCredentialChecker _postgresCredentialChecker = new PostgresCredentialChecker();
    private const string PostgresFlexServer = "dev-test-signify";
    private const string DatabaseName = "datastore";
    private const string PostgresUser = "signify";
    private const string PostgresPassword = "galway";
    private const string SslMode = "Require";
    private const int Port = 1234;
    private CheckPostgresCredentialsCommand CreateSubject() => new ();
    private CancellationToken _cancellationToken = CancellationToken.None;

    [Fact]
    public async Task Postgres_Credential_Checker__CreateDatabaseCredentials_HappyPath()
    {
        DatabaseServer server = _postgresCredentialChecker.CreateDatabaseCredentials(PostgresFlexServer, DatabaseName,
            PostgresUser, PostgresPassword, SslMode, Port);
        
        Assert.Equal(server.Server, PostgresFlexServer);
        Assert.Equal(server.Database, DatabaseName);
        Assert.Equal(server.User, PostgresUser);
        Assert.Equal(server.Password, PostgresPassword);
        Assert.Equal(server.Mode, SslMode);
        Assert.Equal(server.Port, Port);
    }
}