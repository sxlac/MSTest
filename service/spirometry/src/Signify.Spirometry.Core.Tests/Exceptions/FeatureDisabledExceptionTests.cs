using Signify.Spirometry.Core.Exceptions;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Exceptions;

public class FeatureDisabledExceptionTests
{
    [Fact]
    public void Constructor_SetsProperties_Test()
    {
        const long evaluationId = 1;
        const string featureName = "MyFeature";

        var ex = new FeatureDisabledException(evaluationId, featureName);

        Assert.Equal(evaluationId, ex.EvaluationId);
        Assert.Equal(featureName, ex.FeatureName);
    }

    [Fact]
    public void Constructor_SetsMessage_Test()
    {
        const long evaluationId = 1;
        const string featureName = "MyFeature";

        var ex = new FeatureDisabledException(evaluationId, featureName);

        Assert.Equal("Feature MyFeature is disabled, not processing event for EvaluationId=1", ex.Message);
    }
}