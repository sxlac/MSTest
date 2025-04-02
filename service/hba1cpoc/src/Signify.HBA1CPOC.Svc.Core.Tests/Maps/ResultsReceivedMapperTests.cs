using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Signify.HBA1CPOC.Messages.Events.Akka;
using Signify.HBA1CPOC.Svc.Core.BusinessRules;
using Signify.HBA1CPOC.Svc.Core.Maps;
using Signify.HBA1CPOC.Svc.Core.Models;
using System.Collections.Generic;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.Maps;

public class ResultsReceivedMapperTests
{
    [Theory]
    [MemberData(nameof(Convert_SetsAllProperties_TestData))]
    public void Convert_SetsAllProperties(string rawValue, string expectedNormalityIndicator, Normality normality, bool expectedBillability, string expectedException)
    {
        var source = new ResultsModel
        {
            Exception = expectedException,
            Normality = normality,
            RawValue = rawValue
        };

        // Cannot unit test the ResultsReceivedMapper directly in this case, since it needs the ResolutionContext,
        // which is not an interface so it cannot be mocked. Need to test using mapper.Map() instead.
        // https://github.com/AutoMapper/AutoMapper/discussions/3726

        // Still leaving this here in this test class instead of MappingProfileTests just so they're easier to find
            
        var services = new ServiceCollection().AddSingleton<IBillableRules>(new BillAndPayRules());
        var mapper= new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
            cfg.ConstructServicesUsing(
                type => ActivatorUtilities.CreateInstance(services.BuildServiceProvider(), type));
        }).CreateMapper();

        var actual = mapper.Map<ResultsReceived>(source);

        Assert.NotNull(actual.Results);

        Assert.Equal(rawValue, actual.Results.Result);
        Assert.Equal(expectedNormalityIndicator, actual.Determination);
        Assert.Equal(expectedNormalityIndicator, actual.Results.AbnormalIndicator);
        Assert.Equal(expectedBillability, actual.IsBillable);
        Assert.Equal(expectedException, actual.Results.Exception);
    }

    public static IEnumerable<object[]> Convert_SetsAllProperties_TestData()
    {
        yield return
        [
            "5", "N", Normality.Normal, true, null
        ];

        yield return
        [
            "<4", "A", Normality.Abnormal, true, null
        ];

        yield return
        [
            "abc", "U", Normality.Undetermined, false, "Result is malformed"
        ];
    }
}