using Castle.Core.Internal;
using Signify.A1C.Svc.Core.Configs;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Signify.A1C.Svc.Core.Tests.Utilities
{
    public class ExcludedFilesTests
    {

        [Fact]
        public void Should_Exclude_Only_Allowed_Classes()
        {
            string[] allowedClasses =
            {
                "OktaConfig", "RedisConfig", "ServiceBusConfig", "UriHealthCheckConfig", "WebApiConfig",
                "KafkaPublishException", "MailSendException"
            };
            List<string> missedClasses = new List<string>();
            var classLst = GetClasses();
            foreach (var cls in classLst)
            {
                if (cls.IsClass && cls.IsVisible
                                && !allowedClasses.Any(o => string.Equals(o, cls.Name, StringComparison.OrdinalIgnoreCase)))
                    missedClasses.Add(cls.Name);
            }

            Assert.Empty(missedClasses);
        }

        private List<Type> GetClasses()
        {
            List<Type> lst = new List<Type>();
            var assembly = typeof(OktaConfig).GetTypeInfo().Assembly;
            foreach (Type type in assembly.GetTypes())
            {
                if (type.GetAttributes<ExcludeFromCodeCoverageAttribute>().Any())
                //if(Attribute.IsDefined(type, typeof(ExcludeFromCodeCoverageAttribute)))
                {
                    lst.Add(type);
                }

            }

            return lst;
        }
    }
}
