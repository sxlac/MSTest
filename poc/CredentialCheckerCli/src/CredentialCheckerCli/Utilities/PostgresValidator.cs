using System.Reflection.Metadata;
using FluentValidation;

namespace CredentialCheckerCli.Utilities;

public class PostgresValidator : AbstractValidator<DatabaseServer>
{
    public PostgresValidator()
    {
        RuleFor(dbServer => dbServer.User).NotEmpty().WithMessage("user field is empty, please add a value.");
        RuleFor(dbServer => dbServer.Password).NotEmpty().WithMessage("password field is empty, please add a value.");
        RuleFor(dbServer => dbServer.Port).NotEmpty().WithMessage("port field is empty, please add a value.");
        RuleFor(dbServer => dbServer.Server).NotEmpty().WithMessage("flexServer field is empty, please add a value.");
        RuleFor(dbServer => dbServer.Database).NotEmpty().WithMessage("databaseName field is empty, please add a value.");
        RuleFor(dbServer => dbServer.Mode).NotEmpty().WithMessage("sslMode field is empty, please add a value.");
    }
}