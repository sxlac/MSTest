using DpsOps.Core.Exceptions;
using DpsOps.Core.Services;

namespace DpsOps.ConsoleApplication;

public static class Program
{
    public static async Task Main(string[] args)
    {
        GetInput(args, out var supplyRequestFilePath, out var masterListFilePath, out var delimiter, out var outputFilePath);

        var service = new ProviderSupplyRequestService(delimiter);

        try
        {
            await service.AddShipToNumbers(supplyRequestFilePath, masterListFilePath, outputFilePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);

            if (ShouldIncludeStackTrace(ex))
                Console.WriteLine(ex.StackTrace);
        }
    }

    private static void GetInput(IReadOnlyList<string> args,
        out string supplyRequestFilePath,
        out string masterListFilePath,
        out string delimiter,
        out string outputFilePath)
    {
        supplyRequestFilePath = args.Count > 0 ? args[0] : "provider.txt";
        masterListFilePath = args.Count > 1 ? args[1] : "master.txt";
        outputFilePath = args.Count > 2 ? args[2] : $"output_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";

        // Probably best to use Tab as the delimiter, as providers may include a
        // comma in one of the address or notes fields, whereas tabs are not able
        // to be entered into the text boxes they use on the iPad
        delimiter = args.Count > 3 ? args[2] : "\t";

        string delimiterToDisplay;
        if (!string.IsNullOrWhiteSpace(delimiter))
            delimiterToDisplay = delimiter;
        else
        {
            switch (delimiter)
            {
                case "\t":
                    delimiterToDisplay = "[tab]";
                    break;
                case " ":
                    delimiterToDisplay = "[space]";
                    break;
                default:
                    delimiterToDisplay = "[whitespace]";
                    break;
            }
        }
        Console.WriteLine();
        Console.WriteLine($"Provider Supply Request file: {supplyRequestFilePath}");
        Console.WriteLine($"Master Ship To file: {masterListFilePath}");
        Console.WriteLine($"File delimiter: {delimiterToDisplay}");
        Console.WriteLine($"Output file will be: {outputFilePath}");
        Console.WriteLine();
    }

    private static bool ShouldIncludeStackTrace(Exception ex) // cleaner than a bunch of copy-paste catch blocks
    {
        // These exceptions already have sufficient context in the message
        return ex is not ArgumentNullException &&
               ex is not FileNotFoundException &&
               ex is not InvalidOperationException &&
               ex is not DuplicateHeaderException &&
               ex is not MissingHeaderException;
    }
}
