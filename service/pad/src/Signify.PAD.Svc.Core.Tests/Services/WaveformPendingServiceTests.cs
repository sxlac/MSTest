using FakeItEasy;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Signify.Dps.Observability.Library.Services;
using Signify.PAD.Svc.Core.Configs.WaveformConfigs;
using Signify.PAD.Svc.Core.Infrastructure;
using Signify.PAD.Svc.Core.Services;
using Signify.PAD.Svc.Core.Services.Interfaces;
using Signify.PAD.Svc.Core.Tests.Fakes.Infrastructure;
using System;
using System.Threading;
using System.Threading.Tasks;
using Signify.PAD.Svc.Core.Commands;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Services;

public class WaveformPendingServiceTests
{
    private const string Pending = "PENDING";

    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IDirectoryServices _directoryServices = A.Fake<IDirectoryServices>();
    private readonly IWaveformVendorsConfig _vendorsConfig = A.Fake<IWaveformVendorsConfig>();
    private readonly IWaveformThresholdConfig _thresholdConfig = A.Fake<IWaveformThresholdConfig>();
    private readonly IObservabilityService _observabilityService = A.Dummy<IObservabilityService>();
    private readonly IServiceProvider _serviceProvider = A.Fake<IServiceProvider>();
    private readonly FakeApplicationTime _applicationTime = new();

    public WaveformPendingServiceTests()
    {
        A.CallTo(() => _serviceProvider.GetService(typeof(IMediator)))
            .Returns(_mediator);

        A.CallTo(() => _serviceProvider.GetService(typeof(IDirectoryServices)))
            .Returns(_directoryServices);

        A.CallTo(() => _serviceProvider.GetService(typeof(IApplicationTime)))
            .Returns(_applicationTime);

        A.CallTo(() => _vendorsConfig.VendorConfigs)
            .Returns([
                new WaveformVendorConfig
                {
                    VendorName = "Semler Scientific"
                }
            ]);

        A.CallTo(() => _serviceProvider.GetService(typeof(IWaveformVendorsConfig)))
            .Returns(_vendorsConfig);

        A.CallTo(() => _serviceProvider.GetService(typeof(IWaveformThresholdConfig)))
            .Returns(_thresholdConfig);

        A.CallTo(() => _serviceProvider.GetService(typeof(IObservabilityService)))
            .Returns(_observabilityService);
    }

    private WaveformPendingService CreateSubject()
    {
        var countScopeCreated = 0;

        var serviceScope = A.Fake<IServiceScope>();
        A.CallTo(() => serviceScope.ServiceProvider)
            .Returns(_serviceProvider);

        var serviceScopeFactory = A.Fake<IServiceScopeFactory>();
        A.CallTo(() => serviceScopeFactory.CreateScope())
            .ReturnsLazily(() =>
            {
                if (++countScopeCreated > 1)
                    throw new OperationCanceledException("Stopping the background job execution after being run once");

                return serviceScope;
            });

        var backgroundConfig = A.Fake<IWaveformBackgroundServiceConfig>();
        A.CallTo(() => backgroundConfig.PollingPeriodSeconds)
            .Returns(10);

        return new WaveformPendingService(A.Dummy<ILogger<WaveformPendingService>>(),
            serviceScopeFactory,
            backgroundConfig);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoVendors_DoesNothing()
    {
        // Arrange
        A.CallTo(() => _vendorsConfig.VendorConfigs)
            .Returns(Array.Empty<IWaveformVendorConfig>());

        // Act
        await CreateSubject().StartAsync(CancellationToken.None);

        // Assert
        A.CallTo(_directoryServices)
            .MustNotHaveHappened();
        A.CallTo(_mediator)
            .MustNotHaveHappened();
        A.CallTo(_observabilityService)
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task ExecuteAsync_WithNoPendingFiles_DoesNothing()
    {
        // Arrange
        A.CallTo(() => _directoryServices.GetPendingDirectory(A<string>._))
            .Returns(Pending);
        A.CallTo(() => _directoryServices.GetFilesFromDirectory(A<string>._))
            .Returns([]);

        // Act
        await CreateSubject().StartAsync(CancellationToken.None);

        // Assert
        A.CallTo(() => _directoryServices.GetPendingDirectory(A<string>._))
            .MustHaveHappened();

        A.CallTo(() => _directoryServices.GetFilesFromDirectory(A<string>.That.Matches(d =>
                d == Pending)))
            .MustHaveHappened();

        A.CallTo(_mediator)
            .MustNotHaveHappened();
        A.CallTo(_observabilityService)
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task ExecuteAsync_WithFileOlderThanThreshold_DoesNothing()
    {
        // Arrange
        const string file = "file1";
        const string thresholdDir = nameof(thresholdDir);

        A.CallTo(() => _directoryServices.GetPendingDirectory(A<string>._))
            .Returns(Pending);
        A.CallTo(() => _directoryServices.GetFilesFromDirectory(A<string>._))
            .Returns([file]);

        A.CallTo(() => _thresholdConfig.FileAgeThresholdDays)
            .Returns(1);

        A.CallTo(() => _directoryServices.GetCreationTimeUtc(A<string>._))
            .Returns(_applicationTime.UtcNow().AddDays(-2));

        A.CallTo(() => _directoryServices.GetFileOlderThanThresholdDirectory(A<string>._))
            .Returns(thresholdDir);

        // Act
        await CreateSubject().StartAsync(CancellationToken.None);

        // Assert
        A.CallTo(() => _directoryServices.GetCreationTimeUtc(A<string>.That.Matches(p => p == Pending + file)))
            .MustHaveHappened();

        A.CallTo(() => _directoryServices.MoveFileDirectory(A<string>._,
                A<string>.That.Matches(t => t == thresholdDir),
                A<string>.That.Matches(f => f == file),
                A<bool>._))
            .MustHaveHappened();

        A.CallTo(() => _mediator.Send(A<ProcessPendingWaveform>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }
}
