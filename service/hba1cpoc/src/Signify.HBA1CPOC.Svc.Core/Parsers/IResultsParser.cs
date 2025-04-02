using Signify.HBA1CPOC.Svc.Core.Models;

namespace Signify.HBA1CPOC.Svc.Core.Parsers
{
    public interface IResultsParser
    {
        public ResultsModel Parse(string rawValue);
    }
}
