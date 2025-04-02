using System.Diagnostics;
using DpsOps.Core.Algorithms;
using DpsOps.Core.Builders;
using DpsOps.Core.Exceptions;
using DpsOps.Core.Formatters;
using DpsOps.Core.HeaderExtractors;
using DpsOps.Core.Models.ColumnDefinitions;
using DpsOps.Core.Models.Records;

namespace DpsOps.Core.Services;

public interface IProviderSupplyRequestService
{
    /// <summary>
    /// Creates a file just like the Supply Request file, with a new column containing
    /// the corresponding Ship To number and potential mismatch reason
    /// </summary>
    /// <param name="supplyRequestFilename">Path to the Supply Request file</param>
    /// <param name="masterListFilename">Path to the Master List file</param>
    /// <param name="outputFilename">Path for the output file</param>
    /// /// <exception cref="ArgumentNullException">
    /// Thrown if
    /// <see cref="supplyRequestFilename"/> and/or
    /// <see cref="masterListFilename"/> and/or
    /// <see cref="outputFilename"/>
    /// are null or empty
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Thrown if either
    /// <see cref="supplyRequestFilename"/> and/or
    /// <see cref="masterListFilename"/>
    /// do not exist
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown if the output file already exists</exception>
    /// <exception cref="DuplicateHeaderException{TColumns}"></exception>
    /// <exception cref="MissingHeaderException"></exception>
    public Task AddShipToNumbers(string supplyRequestFilename, string masterListFilename, string outputFilename);
}

public class ProviderSupplyRequestService : IProviderSupplyRequestService
{
    private readonly string _delimiter;

    /// <param name="delimiter">Delimiter for both the input and output files</param>
    /// <exception cref="ArgumentNullException"></exception>
    public ProviderSupplyRequestService(string delimiter)
    {
        if (string.IsNullOrEmpty(delimiter))
            throw new ArgumentNullException(nameof(delimiter));

        _delimiter = delimiter;
    }

    /// <inheritdoc />
    public async Task AddShipToNumbers(string supplyRequestFilename, string masterListFilename, string outputFilename)
    {
        ValidateArgs(supplyRequestFilename, masterListFilename, outputFilename);

        var sw = Stopwatch.StartNew();

        var requestEnumerator = File.ReadLinesAsync(supplyRequestFilename)
            .GetAsyncEnumerator();

        try
        {
            var masterEnumerator = File.ReadLinesAsync(masterListFilename).GetAsyncEnumerator();

            try
            {
                await ProcessFiles(requestEnumerator, masterEnumerator, outputFilename);
            }
            finally
            {
                await masterEnumerator.DisposeAsync();
            }
        }
        finally
        {
            await requestEnumerator.DisposeAsync();
        }

        sw.Stop();

        Console.WriteLine();
        Console.WriteLine($"Total processing completed in {sw.ElapsedMilliseconds}ms");
    }

    private async Task ProcessFiles(IAsyncEnumerator<string> requestEnumerator, IAsyncEnumerator<string> masterEnumerator, string outputFilePath)
    {
        var requestHeaderExtractor = new ProviderSupplyRequestHeaderExtractor();

        if (!await requestEnumerator.MoveNextAsync())
            throw new InvalidOperationException($"{requestHeaderExtractor.FileType} file is empty");

        var requestHeader = requestEnumerator.Current;

        var requestColumns = await ExtractHeaderContents(requestHeaderExtractor, requestEnumerator, false);

        var masterColumns = await ExtractHeaderContents(new MasterListHeaderExtractor(), masterEnumerator, true);

        // ideally later (further SRP) - split this logic out into a different
        // class to separate I/O with the actual implementation here; this is
        // good enough for now
        var results = await GetResults(requestEnumerator, masterEnumerator, requestColumns, masterColumns);

        // Insert header columns
        results.Insert(0, string.Join(_delimiter, "Mismatch Reason", "Ship To Number", requestHeader));

        await File.AppendAllLinesAsync(outputFilePath, results);
    }

    private static void ValidateArgs(string supplyRequestFilePath, string masterListFilePath, string outputFilePath)
    {
        if (string.IsNullOrEmpty(supplyRequestFilePath))
            throw new ArgumentNullException(nameof(supplyRequestFilePath), "Supply request filename not supplied");
        if (string.IsNullOrEmpty(masterListFilePath))
            throw new ArgumentNullException(nameof(masterListFilePath), "Master list filename not supplied");

        if (!File.Exists(supplyRequestFilePath))
            throw new FileNotFoundException("Provider supply request file does not exist", supplyRequestFilePath);

        if (!File.Exists(masterListFilePath))
            throw new FileNotFoundException("Master list file does not exist", "masterListFilename}");

        if (File.Exists(outputFilePath))
            throw new InvalidOperationException($"Output file already exists at path {outputFilePath}");
    }

    private async Task<IDictionary<THeader, int>> ExtractHeaderContents<THeader>(IHeaderExtractor<THeader> headerExtractor,
        IAsyncEnumerator<string> fileContentsEnumerator,
        bool advanceEnumerator)
        where THeader : struct, Enum
    {
        var hasContents = advanceEnumerator
            ? await fileContentsEnumerator.MoveNextAsync() && !string.IsNullOrEmpty(fileContentsEnumerator.Current)
            : !string.IsNullOrEmpty(fileContentsEnumerator.Current);

        if (!hasContents)
            throw new InvalidOperationException($"{headerExtractor.FileType} file is empty");

        var headerParts = fileContentsEnumerator.Current.Split(_delimiter);

        return headerExtractor.ExtractHeaders(headerParts);
    }

    private async Task<List<string>> GetResults(IAsyncEnumerator<string> requestEnumerator, IAsyncEnumerator<string> masterEnumerator,
        IDictionary<ProviderSupplyRequestColumns, int> requestColumns,
        IDictionary<MasterListColumns, int> masterColumns)
    {
        var masterLookup = await BuildNpiLookup(masterEnumerator, masterColumns);

        var outputLines = new List<string>();

        var builder = new SupplyRequestRecordBuilder(_delimiter, requestColumns)
            .AddFormatter(new AddressFormatter())
            .AddFormatter(new UsStateFormatter())
            .AddFormatter(new ZipCodeFormatter());

        var algo = new SupplyRequestShipToAlgorithm();

        while (await requestEnumerator.MoveNextAsync())
        {
            var record = builder.BuildRecord(requestEnumerator.Current);

            if (!masterLookup.TryGetValue(record.Npi, out var npiMasterList))
            {
                outputLines.Add(string.Join(_delimiter, "NPI not in Master List", string.Empty, requestEnumerator.Current));
                continue;
            }

            var result = algo.TryMatch(record, npiMasterList);

            outputLines.Add(string.Join(_delimiter, result.Reason, result.ShipToNumber, requestEnumerator.Current));
        }

        Console.WriteLine($"{outputLines.Count} total records in supply request file");

        return outputLines;
    }

    private async Task<IDictionary<string, ICollection<MasterListRecord>>> BuildNpiLookup(IAsyncEnumerator<string> enumerator, IDictionary<MasterListColumns, int> columns)
    {
        var lookup = new Dictionary<string, ICollection<MasterListRecord>>();

        var builder = new MasterListRecordBuilder(_delimiter, columns)
            .AddFormatter(new AddressFormatter())
            .AddFormatter(new UsStateFormatter())
            .AddFormatter(new ZipCodeFormatter());

        while (await enumerator.MoveNextAsync())
        {
            var record = builder.BuildRecord(enumerator.Current);

            if (!lookup.TryGetValue(record.Npi, out var records))
            {
                records = new List<MasterListRecord>();
                lookup.Add(record.Npi, records);
            }

            records.Add(record);
        }

        Console.WriteLine($"{lookup.Count} total NPIs in master file");
        Console.WriteLine($"{lookup.Sum(kvp => kvp.Value.Count)} total addresses in master file");

        return lookup;
    }
}
