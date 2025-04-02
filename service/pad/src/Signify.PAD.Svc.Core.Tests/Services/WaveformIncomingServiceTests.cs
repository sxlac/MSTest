using FakeItEasy;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Signify.Dps.Observability.Library.Services;
using Signify.PAD.Svc.Core.Configs.WaveformConfigs;
using Signify.PAD.Svc.Core.Constants;
using Signify.PAD.Svc.Core.Services.Interfaces;
using Signify.PAD.Svc.Core.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Services;

public class WaveformIncomingServiceTests
{
    private readonly IMediator _mediator = A.Dummy<IMediator>();
    private readonly IDirectoryServices _directoryServices = A.Dummy<IDirectoryServices>();
    private readonly IWaveformBackgroundServiceConfig _backgroundConfig = A.Fake<IWaveformBackgroundServiceConfig>();
    private readonly IWaveformVendorsConfig _vendorConfig = A.Fake<IWaveformVendorsConfig>();
    private readonly IObservabilityService _observabilityService = A.Dummy<IObservabilityService>();
        
    /// <summary>
    /// How long to run the exam status monitor background service before timing out
    /// </summary>
    private const int TimeoutMs = 1_000;

    public WaveformIncomingServiceTests()
    {
        A.CallTo(() => _backgroundConfig.PollingPeriodSeconds)
            .Returns(10);

        A.CallTo(() => _vendorConfig.VendorConfigs)
            .Returns([
                new WaveformVendorConfig
                {
                    VendorName = "Semler Scientific",
                    VendorDirectory = @"SemlerScientific\",
                    FileNameFormat = @"<lastName>_<memberPlanId>_<testPerformed>_<dateOfExam:MMDDYY>.PDF"
                }
            ]);
    }

    [Theory]
    [InlineData(1, 0, new[] {"pass"})]
    [InlineData(0, 1, new[] {"fail"})]
    [InlineData(1, 1, new[] {"pass", "fail"})]
    [InlineData(5, 3, new[] {"pass", "pass", "fail", "fail", "pass", "fail", "pass", "pass"})]
    public async Task Start_Processes_Files_And_Publishes_Results(int pass, int fail, string[] values)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_mediator);
        services.AddSingleton(A.Dummy<ILogger<WaveformIncomingService>>());
        services.AddSingleton(_directoryServices);
        services.AddSingleton(_backgroundConfig);
        services.AddSingleton(_vendorConfig);
        services.AddSingleton(typeof(IHostedService), typeof(WaveformIncomingService));
        services.AddSingleton(_observabilityService);
        await using var serviceProvider = services.BuildServiceProvider();
        var hostedService = serviceProvider.GetService<IHostedService>();

        A.CallTo(() => _directoryServices.GetFilesFromDirectory(A<string>._))
            .Returns(new List<string>(values));
        A.CallTo(() => _directoryServices.MoveFileDirectory(A<string>._, A<string>._, "pass", false)).Returns(true);
        A.CallTo(() => _directoryServices.MoveFileDirectory(A<string>._, A<string>._, "fail", false)).Returns(false);
        
        // Act
        using var tokenSource = new CancellationTokenSource();
        tokenSource.CancelAfter(TimeoutMs);
        await hostedService!.StartAsync(tokenSource.Token);
        await hostedService.StopAsync(CancellationToken.None);
        // Assert
        true.Should().BeTrue();
        
        A.CallTo(() => _observabilityService.AddEvent(
            Observability.Waveform.WaveformDocumentIncomingEvent,
            A<Dictionary<string, object>>.That.Matches(i => 
                (int)i["IncomingSuccessCount"] == pass &&
                (int)i["IncomingFailedCount"] == fail
            )
        )).MustHaveHappened();
    }
}