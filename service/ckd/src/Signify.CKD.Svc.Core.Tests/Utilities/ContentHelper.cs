using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace Signify.CKD.Svc.Core.Tests.Utilities
{
    public static class ContentHelper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static StringContent GetStringContent(object obj)
            => new StringContent(JsonConvert.SerializeObject(obj), Encoding.Default, "application/json");
    }
}
