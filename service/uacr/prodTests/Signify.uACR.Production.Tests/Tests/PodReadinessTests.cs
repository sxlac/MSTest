using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signify.Dps.Test.Utilities.Rancher.Actions;
using Signify.QE.MSTest.Attributes;
using Signify.QE.MSTest.Utilities;

namespace Signify.uACR.Production.Tests.Tests;

[TestClass]
public class PodReadinessTests
{
    private static string _clusterId;
    private static string _namespace;
    private static string _gitVersion;
    private static string _commitId;
    private static RancherActions _rancherActions;

    [AssemblyInitialize]
    public static void SetupEnvironment(TestContext testContext)
    {
        _rancherActions = new RancherActions(new LoggingHttpMessageHandler(testContext), Environment.GetEnvironmentVariable("RancherToken") ?? testContext.Properties["RancherToken"]!.ToString());
        
        _clusterId = Environment.GetEnvironmentVariable("ClusterId") ?? testContext.Properties["ClusterId"]!.ToString();
        
        _namespace = Environment.GetEnvironmentVariable("NameSpace") ?? testContext.Properties["NameSpace"]!.ToString();
        
        _gitVersion = Environment.GetEnvironmentVariable("GitVersion") ?? testContext.Properties["GitVersion"]!.ToString();
        
        _commitId = Environment.GetEnvironmentVariable("CommitId") ?? testContext.Properties["CommitId"]!.ToString();
    }

    [RetryableTestMethod, TestCategory("prod_readiness")]
    public async Task PodReadiness_Test()
    {
        var response = await _rancherActions.GetPodData(_clusterId, _namespace);
        foreach (var data in response.data)
        {
            var condition = data.status.conditions.Select(x => x).FirstOrDefault(x => x.type == "Ready");
            condition.status.Should().Be("True");
            condition.error.Should().BeFalse();
            condition.transitioning.Should().BeFalse();

            var containerStatus = data.status.containerStatuses.FirstOrDefault();
            containerStatus.image.Should().Contain(_gitVersion.Split("-")[0]);
            containerStatus.image.Should().Contain(_commitId[..7]);
            containerStatus.ready.Should().BeTrue();
            
        }
        
    }
}