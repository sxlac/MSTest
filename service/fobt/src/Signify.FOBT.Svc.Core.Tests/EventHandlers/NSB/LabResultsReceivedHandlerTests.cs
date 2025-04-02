using AutoMapper;
using FakeItEasy;
using FobtNsbEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using NsbEventHandlers;
using NServiceBus.Testing;
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.FOBT.Svc.Core.BusinessRules;
using Signify.FOBT.Svc.Core.Commands;
using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.Data;
using Signify.FOBT.Svc.Core.Events;
using Signify.FOBT.Svc.Core.Exceptions;
using Signify.FOBT.Svc.Core.FeatureFlagging;
using Signify.FOBT.Svc.Core.Messages.Queries;
using Signify.FOBT.Svc.Core.Models;
using Signify.FOBT.Svc.Core.Queries;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

using Fobt = Signify.FOBT.Svc.Core.Data.Entities.FOBT;

namespace Signify.FOBT.Svc.Core.Tests.EventHandlers.NSB;

public class LabResultsReceivedHandlerTests
{
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly ITransactionSupplier _transactionSupplier = A.Fake<ITransactionSupplier>();
    private readonly LabResultsReceivedHandler _handler;
    private readonly TestableMessageHandlerContext _messageSessionInstance = new();
    private readonly IBillableRules _billableRules = A.Fake<IBillableRules>();

    public LabResultsReceivedHandlerTests()
    {
        var logger = A.Dummy<ILogger<LabResultsReceivedHandler>>();
        var featureFlag = A.Fake<IFeatureFlags>();
        _handler = new LabResultsReceivedHandler(logger, _mediator, _mapper, _transactionSupplier, featureFlag, _billableRules);
    }

    [Fact]
    public async Task Handle_WhenUnableToMatchToFobtRecord_ThrowsForNsbRetry()
    {
        // Arrange
        var message = new HomeAccessResultsReceived
        {
            OrderCorrelationId = Guid.NewGuid()
        };

        A.CallTo(() => _mediator.Send(A<GetFobtByOrderCorrelationId>._, A<CancellationToken>._))
            .Returns(Task.FromResult<Fobt>(null));
        A.CallTo(() => _mediator.Send(A<GetFobtByHistory>._, A<CancellationToken>._))
            .Returns(Task.FromResult<Fobt>(null));

        // Act / Assert
        // Will be later changed to not be a generic ApplicationException
        await Assert.ThrowsAsync<UnableToFindFobtException>(async () =>
            await _handler.Handle(message, _messageSessionInstance)
        );

        A.CallTo(() => _mediator.Send(A<GetFobtByOrderCorrelationId>.That.Matches(g =>
                    g.OrderCorrelationId == message.OrderCorrelationId),
                A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(() => _mediator.Send(A<GetFobtByHistory>.That.Matches(g =>
                    g.OrderCorrelationId == message.OrderCorrelationId &&
                    g.Barcode == message.Barcode),
                A<CancellationToken>._))
            .MustHaveHappened();

        Assert.Empty(_messageSessionInstance.PublishedMessages);
        Assert.Empty(_messageSessionInstance.SentMessages);
    }

    [Fact]
    public async Task Handle_WhenOrderCorrelationIdNotOnFobtRecordFindFromHistory_ProcessHandlerSuccessfully()
    {
        // Arrange
        var message = new HomeAccessResultsReceived
        {
            OrderCorrelationId = Guid.NewGuid(),
            Barcode = "01234567890"
        };

        A.CallTo(() => _mediator.Send(A<GetFobtByOrderCorrelationId>._, A<CancellationToken>._))
            .Returns(Task.FromResult<Fobt>(null));
        A.CallTo(() => _mediator.Send(A<GetFobtByHistory>._, A<CancellationToken>._))
            .Returns(Task.FromResult(new Fobt()));
        A.CallTo(() => _mediator.Send(A<GetLabResult>._, A<CancellationToken>._))
            .Returns(new LabResults());

        // Act
        await _handler.Handle(message, _messageSessionInstance);

        // Assert
        A.CallTo(() => _mediator.Send(A<GetLabResult>.That.Matches(g =>
                    g.OrderCorrelationId == message.OrderCorrelationId),
                A<CancellationToken>._))
            .MustHaveHappened();

        Assert.Empty(_messageSessionInstance.PublishedMessages);
        Assert.Empty(_messageSessionInstance.SentMessages);
    }

    [Fact]
    public async Task Handle_WhenLabResultAlreadyExistsForOrderCorrelationId_DoesNothing()
    {
        // Arrange
        var message = new HomeAccessResultsReceived
        {
            OrderCorrelationId = Guid.NewGuid()
        };

        A.CallTo(() => _mediator.Send(A<GetFobtByOrderCorrelationId>._, A<CancellationToken>._))
            .Returns(Task.FromResult(new Fobt()));
        A.CallTo(() => _mediator.Send(A<GetLabResult>._, A<CancellationToken>._))
            .Returns(new LabResults());

        // Act
        await _handler.Handle(message, _messageSessionInstance);

        // Assert
        A.CallTo(() => _mediator.Send(A<GetLabResult>.That.Matches(g =>
                    g.OrderCorrelationId == message.OrderCorrelationId),
                A<CancellationToken>._))
            .MustHaveHappened();

        Assert.Empty(_messageSessionInstance.PublishedMessages);
        Assert.Empty(_messageSessionInstance.SentMessages);
    }

    [Fact]
    public async Task Handle_WhenLabResultsAreNotSaved_DoesNothing()
    {
        // Arrange
        var message = new HomeAccessResultsReceived
        {
            OrderCorrelationId = Guid.NewGuid()
        };

        var fobt = new Fobt
        {
            FOBTId = 1
        };

        var transaction = A.Fake<IBufferedTransaction>();

        A.CallTo(() => _mediator.Send(A<GetFobtByOrderCorrelationId>._, A<CancellationToken>._))
            .Returns(Task.FromResult(fobt));
        A.CallTo(() => _mediator.Send(A<GetLabResult>._, A<CancellationToken>._))
            .Returns(Task.FromResult((LabResults) null));
        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .Returns(transaction);
        A.CallTo(() => _mediator.Send(A<CreateLabResult>._, A<CancellationToken>._))
            .Returns(Task.FromResult((LabResults) null));

        // Act
        await _handler.Handle(message, _messageSessionInstance);

        // Assert
        A.CallTo(() => _mapper.Map<CreateLabResult>(A<HomeAccessResultsReceived>.That.Matches(h => h == message)))
            .MustHaveHappened();
        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreateLabResult>.That.Matches(c =>
                    c.FOBTId == fobt.FOBTId),
                A<CancellationToken>._))
            .MustHaveHappened();

        Assert.Empty(_messageSessionInstance.PublishedMessages);
        Assert.Empty(_messageSessionInstance.SentMessages);

        A.CallTo(() => transaction.CommitAsync(A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => transaction.Dispose())
            .MustHaveHappened();
    }

    [Fact]
    public async Task Handle_WithInvalidResults_Test()
    {
        // Arrange
        var data = CreateSetupData(false);

        // Act
        await _handler.Handle(data.Message, _messageSessionInstance);

        // Assert
        A.CallTo(() => _billableRules.IsLabResultValid(A<BillableRuleAnswers>._))
            .MustHaveHappened();

        // Statuses
        VerifyStatusesSent(data.Fobt, new []
        {
            FOBTStatusCode.InvalidLabResultsReceived,
            FOBTStatusCode.BillRequestNotSent
        });
        VerifyStatusesNotSent(new[]
        {
            FOBTStatusCode.ValidLabResultsReceived
        });

        // Results, and not billable
        A.CallTo(() => _mediator.Send(A<PublishResults>.That.Matches(p =>
                !p.IsBillable), A<CancellationToken>._))
            .MustHaveHappened();

        Assert.Empty(_messageSessionInstance.PublishedMessages);
        Assert.Empty(_messageSessionInstance.SentMessages);

        // Ensure transaction was committed
        A.CallTo(() => data.Transaction.CommitAsync(A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_WithValidResults_ThatAreBillable_Test(bool hasPdfDelivery)
    {
        // Arrange
        var data = CreateSetupData(true);

        A.CallTo(() => _mediator.Send(A<GetPDFToClient>._, A<CancellationToken>._))
            .Returns(hasPdfDelivery ? new PDFToClient() : null);
        var businessRulesMet = new BusinessRuleStatus
        {
            IsMet = true
        };
        A.CallTo(() => _billableRules.IsBillableForResults(A<BillableRuleAnswers>._))
            .Returns(businessRulesMet);
        A.CallTo(() => _billableRules.IsLabResultValid(A<BillableRuleAnswers>._))
            .Returns(businessRulesMet);

        // Act
        await _handler.Handle(data.Message, _messageSessionInstance);

        // Assert
        A.CallTo(() => _billableRules.IsLabResultValid(A<BillableRuleAnswers>._))
            .MustHaveHappened();

        // Statuses
        VerifyStatusesSent(data.Fobt, new []
        {
            FOBTStatusCode.ValidLabResultsReceived
        });
        VerifyStatusesNotSent(new[]
        {
            FOBTStatusCode.InvalidLabResultsReceived,
            FOBTStatusCode.BillRequestNotSent
        });

        // Results, and billable
        A.CallTo(() => _mediator.Send(A<PublishResults>.That.Matches(p =>
                p.IsBillable), A<CancellationToken>._))
            .MustHaveHappened();

        // Billing
        if (hasPdfDelivery)
        {
            Assert.Single(_messageSessionInstance.PublishedMessages);
            var billEvent = _messageSessionInstance.PublishedMessages.First().Message<RCMRequestEvent>();
            Assert.Equal("FOBT-Results", billEvent.RcmProductCode);
            // Verify we send appointmentId
            Assert.Equal("4", billEvent.AdditionalDetails["appointmentId"]);
        }
        else
            Assert.Empty(_messageSessionInstance.PublishedMessages);
            
        // Ensure transaction was committed
        A.CallTo(() => data.Transaction.CommitAsync(A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_WithValidResults_ThatAreNotBillable_Tests(bool hasPdfDelivery)
    {
        // Arrange
        var data = CreateSetupData(true);

        A.CallTo(() => _mediator.Send(A<GetPDFToClient>._, A<CancellationToken>._))
            .Returns(hasPdfDelivery ? new PDFToClient() : null);
        BusinessRuleStatus businessRulesNotMet = new BusinessRuleStatus();
        businessRulesNotMet.IsMet = false;
        A.CallTo(() => _billableRules.IsBillableForResults(A<BillableRuleAnswers>._))
            .Returns(businessRulesNotMet);
        BusinessRuleStatus businessRulesValidResults = new BusinessRuleStatus();
        businessRulesValidResults.IsMet = true;
        A.CallTo(() => _billableRules.IsLabResultValid(A<BillableRuleAnswers>._))
            .Returns(businessRulesValidResults);

        // Act
        await _handler.Handle(data.Message, _messageSessionInstance);

        // Assert
        A.CallTo(() => _billableRules.IsLabResultValid(A<BillableRuleAnswers>._))
            .MustHaveHappened();

        // Statuses
        VerifyStatusesSent(data.Fobt, new []
        {
            FOBTStatusCode.ValidLabResultsReceived
        });
        VerifyStatusesNotSent(new[]
        {
            FOBTStatusCode.InvalidLabResultsReceived
        });

        if (hasPdfDelivery)
            VerifyStatusesSent(data.Fobt, new [] { FOBTStatusCode.BillRequestNotSent });
        else
            VerifyStatusesNotSent(new [] { FOBTStatusCode.BillRequestNotSent });

        // Results, and NOT billable
        A.CallTo(() => _mediator.Send(A<PublishResults>.That.Matches(p =>
                !p.IsBillable), A<CancellationToken>._))
            .MustHaveHappened();

        // Billing
        Assert.Empty(_messageSessionInstance.PublishedMessages);
        Assert.Empty(_messageSessionInstance.SentMessages);

        // Ensure transaction was committed
        A.CallTo(() => data.Transaction.CommitAsync(A<CancellationToken>._))
            .MustHaveHappened();
    }

    private void VerifyStatusesSent(Fobt fobt, IEnumerable<FOBTStatusCode> statusCodes)
    {
        foreach (var statusCode in statusCodes)
        {
            A.CallTo(() => _mediator.Send(A<CreateFOBTStatus>.That.Matches(s =>
                        s.FOBT == fobt &&
                        s.StatusCode == statusCode),
                    A<CancellationToken>._))
                .MustHaveHappened();
        }
    }

    private void VerifyStatusesNotSent(IEnumerable<FOBTStatusCode> statusCodes)
    {
        foreach (var statusCode in statusCodes)
        {
            A.CallTo(() => _mediator.Send(A<CreateFOBTStatus>.That.Matches(s =>
                        s.StatusCode == statusCode),
                    A<CancellationToken>._))
                .MustNotHaveHappened();
        }
    }

    private SetupData CreateSetupData(bool areValidResults)
    {
        var data = new SetupData(_mediator, _transactionSupplier, _billableRules)
        {
        };
        return data;
    }

    private class SetupData
    {
        public HomeAccessResultsReceived Message { get; }
        public Fobt Fobt { get; }
        private LabResults Results { get; } = new();
        public IBufferedTransaction Transaction { get; } = A.Fake<IBufferedTransaction>();

        public SetupData(IMediator mediator,
            ITransactionSupplier transactionSupplier,
            IBillableRules billableRules)
        {
            Message = new HomeAccessResultsReceived
            {
                OrderCorrelationId = Guid.NewGuid()
            };

            Fobt = new Fobt
            {
                FOBTId = 1,
                EvaluationId = 2,
                ClientId = 3,
                AppointmentId = 4,
            };

            var businessRuleStatus = new BusinessRuleStatus
            {
                IsMet = true
            };


            A.CallTo(() => mediator.Send(A<GetFobtByOrderCorrelationId>._, A<CancellationToken>._))
                .ReturnsLazily(() => Task.FromResult(Fobt));
            A.CallTo(() => mediator.Send(A<GetLabResult>._, A<CancellationToken>._))
                .Returns(Task.FromResult((LabResults) null));
            A.CallTo(() => transactionSupplier.BeginTransaction())
                .Returns(Transaction);
            A.CallTo(() => mediator.Send(A<CreateLabResult>._, A<CancellationToken>._))
                .Returns(Task.FromResult(Results));
            A.CallTo(() => billableRules.IsBillableForResults(A<BillableRuleAnswers>._)).Returns(businessRuleStatus);
        }
    }
}