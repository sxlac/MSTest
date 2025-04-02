using CommandLine;
using CredentialCheckerCli.Utilities;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;

namespace CredentialCheckerCli.Commands;


[Verb("postgresFlex", HelpText = "Checks the credentials of a Postgres flex server")]

public class CheckPostgresCredentialsCommand : ICommand
{
    [Option("flexServer", Required = true, HelpText = "The name of the Postgres flex server")]
    public string PostgresFlexServer { get; set; } = string.Empty;
    [Option("databaseName", HelpText = "The name of the Postgres database")]
    public string DatabaseName { get; set; } = string.Empty;
    [Option("user", HelpText = "The user for the PostgresDB")]
    public string User { get; set; } = string.Empty;
    [Option("password", HelpText = "The password for the PostgresDB")]
    public string Password { get; set; } = string.Empty;
    [Option("sslMode", HelpText = "The SSL Mode for the PostgresDB connection")]
    public string SslMode { get; set; } = string.Empty;
    [Option("port", HelpText = "The port for the PostgresDB connection")]
    public int Port { get; set; }

    public async Task ExecuteAsync(IServiceProvider services, CancellationToken ct)
    {
        var _databaseServer = new DatabaseServer
        {
            Database = DatabaseName,
            Mode = SslMode,
            Password = Password,
            Port = Port,
            Server = PostgresFlexServer,
            User = User
        };
        
        PostgresValidator _postgresValidator = new PostgresValidator();
        
        ValidationResult validationResult = _postgresValidator.Validate(_databaseServer);

        if (validationResult.IsValid)
        {
            var _checker = services.GetRequiredService<IPostgresCredentialChecker>();

            var databaseServer = _checker.CreateDatabaseCredentials(PostgresFlexServer, DatabaseName, User, Password, SslMode, Port);

            var credentialOutcome = await _checker.IsCredentialsValid(databaseServer);

            if (!credentialOutcome)
            {
                throw new Exception("The credentials you provided are invalid.");
            }
            Console.WriteLine($"Postgres credentials are valid.");
        }
        else
        {
            throw new Exception($"{validationResult}");
        }
    }
}