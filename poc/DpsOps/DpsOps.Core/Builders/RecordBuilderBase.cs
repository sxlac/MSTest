using System.Collections.ObjectModel;
using System.Text;

namespace DpsOps.Core.Builders;

internal class RecordBuilderBase<TColumnTypes>
{
    private readonly IReadOnlyDictionary<TColumnTypes, int> _columns;

    /// <param name="columns">Key: column, Value: index of column</param>
    protected RecordBuilderBase(IDictionary<TColumnTypes, int> columns)
    {
        _columns = new ReadOnlyDictionary<TColumnTypes, int>(columns);
    }

    /// <summary>
    /// Extracts a formatted value of the contents of the record at column <see cref="column"/>
    /// </summary>
    /// <param name="parts">All parts of the input string</param>
    /// <param name="column">Column to extract</param>
    protected string Extract(IReadOnlyList<string> parts, TColumnTypes column)
    {
        var part = parts[_columns[column]];

        var sb = new StringBuilder(part.Length);

        var isLastCharWhitespace = false;

        foreach (var ch in part)
        {
            switch (ch)
            {
                case '.':
                case ',':
                    continue; // Ignore these characters, we don't want to include them in any comparisons
                case '#':
                    // Replace # with whitespace to make address matching more successful, ex:
                    // "Unit #2" -> "Unit 2"
                    // "Unit#2" -> "Unit 2"
                case ' ':
                    if (isLastCharWhitespace || sb.Length == 0)
                        continue;

                    isLastCharWhitespace = true;
                    sb.Append(' ');
                    continue;
                default:
                    isLastCharWhitespace = false;
                    sb.Append(char.ToUpper(ch));
                    continue;
            }
        }

        return sb.ToString().TrimEnd();
    }
}
