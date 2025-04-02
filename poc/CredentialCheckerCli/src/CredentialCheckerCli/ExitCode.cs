namespace CredentialCheckerCli;


internal enum ExitCode : int
{
    UnexpectedError = -1,
    ProgramSuccess = 0,
    ParsingFailed = 1,
    UnsupportedPlatform = 2,
}
