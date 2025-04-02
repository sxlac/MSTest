

using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CredentialCheckerCli;
using CredentialCheckerCli.Commands;
using CredentialCheckerCli.Utilities;
using Microsoft.Extensions.Logging;
using Npgsql;

using var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (sender, args) =>
{
    args.Cancel = true;

    if (!cancellationTokenSource.IsCancellationRequested)
    {
        cancellationTokenSource.Cancel();
    }
};

Type[] commands = typeof(Program).Assembly.GetTypes()
    .Where(t => typeof(ICommand).IsAssignableFrom(t))
    .ToArray();

var services = new ServiceCollection();
services.AddLogging(options => options.AddConsole());

services.AddSingleton(typeof(IPostgresCredentialChecker), typeof(PostgresCredentialChecker));
services.AddSingleton(typeof(IKafkaCredentialChecker), typeof(KafkaCredentialChecker));

Parser.Default.ParseArguments<CheckPostgresCredentialsCommand, CheckKafkaCredentialsCommand>(args)
    .WithParsed<ICommand>(c =>
        CallAndHandleReturnCode(services, c, cancellationTokenSource.Token));

static ExitCode CallAndHandleReturnCode(IServiceCollection services, object commandObj, CancellationToken ct)
{
    if (commandObj is not ICommand command)
    {
        return ExitCode.UnexpectedError;
    }
    
    try
    {
        var provider = services.BuildServiceProvider();
        
        command.ExecuteAsync(provider, ct).Wait();
        return ExitCode.ProgramSuccess;
    }
    catch (ProgramException e)
    {
        Console.Error.WriteLine(e.ToString());
        return e.ExitCode;
    }
    catch (Exception e)
    {
        Console.Error.WriteLine(e.ToString());
        return ExitCode.UnexpectedError;
    }
}

// parser automatically prints usage before calling this
static ExitCode OnParseFailure(IEnumerable<Error> errors)
{
    foreach (Error error in errors)
    {
        Console.Error.WriteLine(error.ToString());
    }
    return ExitCode.ParsingFailed;
}

internal class ProgramException : Exception
{
    public ExitCode ExitCode { get; }

    public ProgramException(
        ExitCode exitCode,
        string? message = default,
        Exception? innerException = default)
        : base(message, innerException)
    {
        ExitCode = exitCode;
    }
}