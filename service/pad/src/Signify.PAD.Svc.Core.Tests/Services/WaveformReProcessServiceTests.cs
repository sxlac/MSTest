using FakeItEasy;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Configs.WaveformConfigs;
using Signify.PAD.Svc.Core.Data;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Models;
using Signify.PAD.Svc.Core.Queries;
using Signify.PAD.Svc.Core.Services;
using Signify.PAD.Svc.Core.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Services;

public class WaveformReProcessServiceTests
{
    private const string Semler = "Semler Scientific";
    private readonly DateTime _startDateTime = DateTime.Today.AddDays(-1).ToUniversalTime();
    private readonly DateTime _endDateTime = DateTime.Today.ToUniversalTime();

    private readonly IWaveformReProcessConfig _reprocessConfig = A.Fake<IWaveformReProcessConfig>();
    private readonly IServiceProvider _serviceProvider = A.Fake<IServiceProvider>();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IDirectoryServices _directory = A.Fake<IDirectoryServices>();
    private readonly FakeTransactionSupplier _fakeTransactionSupplier = new();

    public WaveformReProcessServiceTests()
    {
        A.CallTo(() => _serviceProvider.GetService(typeof(IMediator)))
            .Returns(_mediator);

        A.CallTo(() => _serviceProvider.GetService(typeof(IDirectoryServices)))
            .Returns(_directory);

        A.CallTo(() => _reprocessConfig.VendorName)
            .Returns(Semler);

        var semlerConfig = A.Fake<IWaveformVendorConfig>();
        A.CallTo(() => semlerConfig.VendorName)
            .Returns(Semler);

        var vendorConfigs = A.Fake<IWaveformVendorsConfig>();
        A.CallTo(() => vendorConfigs.VendorConfigs)
            .Returns(new List<IWaveformVendorConfig>
            {
                semlerConfig
            });

        A.CallTo(() => _serviceProvider.GetService(typeof(IWaveformVendorsConfig)))
            .Returns(vendorConfigs);

        A.CallTo(() => _serviceProvider.GetService(typeof(ITransactionSupplier)))
            .Returns(_fakeTransactionSupplier);
    }

    private WaveformReProcessService CreateSubject()
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

        var backgroundServiceConfig = A.Fake<IWaveformBackgroundServiceConfig>();
        A.CallTo(() => backgroundServiceConfig.PollingPeriodSeconds)
            .Returns(10);

        A.CallTo(() => _serviceProvider.GetService(typeof(IWaveformReProcessConfig)))
            .Returns(_reprocessConfig);

        return new WaveformReProcessService(A.Dummy<ILogger<WaveformReProcessService>>(),
            serviceScopeFactory,
            backgroundServiceConfig);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownVendor_Throws()
    {
        // Arrange
        A.CallTo(() => _reprocessConfig.VendorName)
            .Returns("Unknown vendor");

        // Act
        await CreateSubject().StartAsync(CancellationToken.None);

        // Assert
        A.CallTo(_mediator)
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task ExecuteAsync_WithNoWaveforms_DoesNothing()
    {
        // Arrange
        A.CallTo(() => _mediator.Send(A<QueryWaveforms>.That.Matches(q =>
                q.VendorName == Semler && q.StartDateTime == _startDateTime && q.EndDateTime == _endDateTime), A<CancellationToken>._))
            .Returns(new List<WaveformDocument>());

        // Act
        await CreateSubject().StartAsync(CancellationToken.None);

        // Assert
        A.CallTo(_directory)
            .MustNotHaveHappened();
        _fakeTransactionSupplier.AssertNoTransactionCreated();
    }

    [Fact]
    public async Task ExecuteAsync_WhenFailsToDeleteWaveformDocument_DoesNotMoveFile()
    {
        // Arrange
        A.CallTo(() => _mediator.Send(A<DeleteWaveformDocument>._, A<CancellationToken>._))
            .Throws(new Exception());

        A.CallTo(() => _mediator.Send(A<QueryWaveforms>._, A<CancellationToken>._))
            .Returns(new List<WaveformDocument>{ new() });

        // Act
        await CreateSubject().StartAsync(CancellationToken.None);

        // Assert
        A.CallTo(_directory)
            .MustNotHaveHappened();
        _fakeTransactionSupplier.AssertRollback();
    }

    [Fact]
    public async Task ExecuteAsync_WhenFailsToDeleteStatuses_DoesNotMoveFile()
    {
        // Arrange
        A.CallTo(() => _mediator.Send(A<DeleteStatuses>._, A<CancellationToken>._))
            .Throws(new Exception());

        A.CallTo(() => _mediator.Send(A<QueryWaveforms>._, A<CancellationToken>._))
            .Returns(new List<WaveformDocument>{ new() });

        // Act
        await CreateSubject().StartAsync(CancellationToken.None);

        // Assert
        A.CallTo(_directory)
            .MustNotHaveHappened();
        _fakeTransactionSupplier.AssertRollback();
    }

    [Fact]
    public async Task ExecuteAsync_WhenFailsToMoveFile_RollsBackTransaction()
    {
        // Arrange
        A.CallTo(() => _directory.MoveFileDirectory(A<string>._, A<string>._, A<string>._, A<bool>._))
            .Returns(false);

        A.CallTo(() => _mediator.Send(A<QueryWaveforms>._, A<CancellationToken>._))
            .Returns(new List<WaveformDocument>{ new() });

        // Act
        await CreateSubject().StartAsync(CancellationToken.None);

        // Assert
        A.CallTo(() => _mediator.Send(A<DeleteWaveformDocument>._, A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<DeleteStatuses>._, A<CancellationToken>._))
            .MustHaveHappened();
        _fakeTransactionSupplier.AssertRollback();
    }

    [Fact]
    public async Task ExecuteAsync_WithOneWaveform_Test()
    {
        // Arrange
        const int padId = 5;

        var waveform = new WaveformDocument
        {
            MemberPlanId = 1,
            DateOfExam = DateTime.Today
        };

        A.CallTo(() => _mediator.Send(A<GetPadByMemberPlanId>.That.Matches(g =>
                    g.MemberPlanId == waveform.MemberPlanId && g.DateOfService == waveform.DateOfExam),
                A<CancellationToken>._))
            .Returns(new Core.Data.Entities.PAD
            {
                PADId = padId
            });

        A.CallTo(() => _directory.MoveFileDirectory(A<string>._, A<string>._, A<string>._, A<bool>._))
            .Returns(true);

        A.CallTo(() => _mediator.Send(A<QueryWaveforms>._, A<CancellationToken>._))
            .Returns(new List<WaveformDocument>{ waveform });

        // Act
        await CreateSubject().StartAsync(CancellationToken.None);

        // Assert
        A.CallTo(() => _mediator.Send(A<DeleteWaveformDocument>.That.Matches(d =>
                d.Waveform == waveform), A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(() => _mediator.Send(A<DeleteStatuses>.That.Matches(d =>
                    d.PadId == padId &&
                    d.StatusCodeIds.Count == 2 &&
                    d.StatusCodeIds.Contains((int)StatusCodes.WaveformDocumentDownloaded) &&
                    d.StatusCodeIds.Contains((int)StatusCodes.WaveformDocumentUploaded)),
                A<CancellationToken>._))
            .MustHaveHappened();

        _fakeTransactionSupplier.AssertCommit();
    }
}
