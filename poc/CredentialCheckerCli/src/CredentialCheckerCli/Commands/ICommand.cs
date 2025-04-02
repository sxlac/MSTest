namespace CredentialCheckerCli.Commands;

internal interface ICommand
{
    Task ExecuteAsync(IServiceProvider services, CancellationToken ct);
}