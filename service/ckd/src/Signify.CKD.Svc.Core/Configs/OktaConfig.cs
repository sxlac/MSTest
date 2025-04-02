using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.CKD.Svc.Core.Configs
{
    [ExcludeFromCodeCoverage]
    public class OktaConfig
    {
        public Uri Domain { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Scopes { get; set; }

    }
}