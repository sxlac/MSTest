using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.Validators;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Validators;

public class LabResultsValidatorTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("  ", true)]
    [InlineData("anything else", false)]
    public void AreValidResults_Tests(string exception, bool expected)
    {
        var labResults = new LabResults
        {
            Exception = exception
        };

        var subject = new LabResultsValidator();

        var actual = subject.AreValidResults(labResults);

        Assert.Equal(expected, actual);
    }
}