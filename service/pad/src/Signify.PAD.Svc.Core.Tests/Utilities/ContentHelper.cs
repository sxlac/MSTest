using Newtonsoft.Json;
using System.Net.Http;
using System.Text;

namespace Signify.PAD.Svc.Core.Tests.Utilities;

public static class ContentHelper
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static StringContent GetStringContent(object obj)
        => new(JsonConvert.SerializeObject(obj), Encoding.Default, "application/json");
}