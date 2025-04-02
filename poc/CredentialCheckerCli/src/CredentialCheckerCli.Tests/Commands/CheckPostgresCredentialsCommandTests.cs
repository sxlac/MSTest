using CredentialCheckerCli.Commands;
using CredentialCheckerCli.Utilities;
using FakeItEasy;
using FluentValidation.TestHelper;
using Xunit;

namespace CredentialCheckerCli.Tests;

public class CheckPostgresCredentialsCommandTests
{
    private readonly IPostgresCredentialChecker _postgresCredentialChecker = A.Fake<IPostgresCredentialChecker>();
    private readonly IServiceProvider _serviceProvider = A.Fake<IServiceProvider>();
    private readonly PostgresValidator _postgresValidator =  new PostgresValidator();
    private const string PostgresFlexServer = "dev-test-signify";
    private const string DatabaseName = "datastore";
    private const string PostgresUser = "signify";
    private const string PostgresPassword = "password";
    private const string SslMode = "Require";
    private const int Port = 1234;
    private CheckPostgresCredentialsCommand CreateSubject() => new ();
    private CancellationToken _cancellationToken = CancellationToken.None;
    
    [Fact]
    public async Task Postgres_Credential_Command_HappyPath()
    {
        A.CallTo(() => _serviceProvider.GetService(null)).WithAnyArguments().Returns(_postgresCredentialChecker);

        A.CallTo(() => _postgresCredentialChecker.IsCredentialsValid(A<DatabaseServer>._)).Returns(true);
        
        var subject = CreateSubject();
        subject.PostgresFlexServer = PostgresFlexServer;
        subject.DatabaseName = DatabaseName;
        subject.User = PostgresUser;
        subject.Password = PostgresPassword;
        subject.SslMode = SslMode;
        subject.Port = Port;
        await subject.ExecuteAsync(_serviceProvider, _cancellationToken);

        A.CallTo(() => _postgresCredentialChecker.CreateDatabaseCredentials(PostgresFlexServer, DatabaseName,
            PostgresUser, PostgresPassword, SslMode, Port)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _postgresCredentialChecker.IsCredentialsValid(A<DatabaseServer>._)).MustHaveHappenedOnceExactly();
    }
    
    [Fact]
    public async Task Postgres_Credential_Command_Credentials_Invalid()
    {
        A.CallTo(() => _serviceProvider.GetService(null)).WithAnyArguments().Returns(_postgresCredentialChecker);

        A.CallTo(() => _postgresCredentialChecker.IsCredentialsValid(A<DatabaseServer>._)).Returns(false);
        
        var subject = CreateSubject();
        subject.PostgresFlexServer = PostgresFlexServer;
        subject.DatabaseName = DatabaseName;
        subject.User = PostgresUser;
        subject.Password = PostgresPassword;
        subject.SslMode = SslMode;
        subject.Port = Port;
        
        var result = await Assert.ThrowsAsync<Exception>(async () => 
            await subject.ExecuteAsync(_serviceProvider, _cancellationToken));
        A.CallTo(() => _postgresCredentialChecker.CreateDatabaseCredentials(PostgresFlexServer, DatabaseName,
            PostgresUser, PostgresPassword, SslMode, Port)).MustHaveHappenedOnceExactly();
        Assert.Equal("The credentials you provided are invalid.", result.Message);
    }
    
    [Fact]
    public async Task Postgres_Credential_Command_Credentials_Empty_And_Null()
    {
        A.CallTo(() => _serviceProvider.GetService(null)).WithAnyArguments().Returns(_postgresCredentialChecker);
        
        
        var subject = CreateSubject();
        subject.PostgresFlexServer = PostgresFlexServer;
        subject.DatabaseName = DatabaseName;
        subject.User = "";
        subject.Password = PostgresPassword;
        subject.SslMode = null;
        subject.Port = Port;
        
        var result = await Assert.ThrowsAsync<Exception>(async () => 
            await subject.ExecuteAsync(_serviceProvider, _cancellationToken));
        Assert.Equal("user field is empty, please add a value.\nsslMode field is empty, please add a value.", result.Message);
    }
    
    [Fact]
    public void Postgres_Credential_Command_Validate_Credentials_HappyPath()
    {
        var _databaseServer = new DatabaseServer();
        _databaseServer.Database = DatabaseName;
        _databaseServer.Server = PostgresFlexServer;
        _databaseServer.User = PostgresUser;
        _databaseServer.Password = PostgresPassword;
        _databaseServer.Mode = SslMode;
        _databaseServer.Port = Port;
        
        A.CallTo(() => _serviceProvider.GetService(null)).WithAnyArguments().Returns(_postgresValidator);
        
        var validationResult = _postgresValidator.TestValidate(_databaseServer);
        validationResult.ShouldNotHaveAnyValidationErrors();
    }
    
    [Fact]
    public void Postgres_Credential_Command_Validate_Credentials_Null_User()
    {
        var _databaseServer = new DatabaseServer();
        _databaseServer.Database = DatabaseName;
        _databaseServer.Server = PostgresFlexServer;
        _databaseServer.User = " ";
        _databaseServer.Password = PostgresPassword;
        _databaseServer.Mode = SslMode;
        _databaseServer.Port = Port;
        
        A.CallTo(() => _serviceProvider.GetService(null)).WithAnyArguments().Returns(_postgresValidator);
        
        var validationResult = _postgresValidator.TestValidate(_databaseServer);
        validationResult.ShouldHaveValidationErrorFor(x=> x.User);
    }
}