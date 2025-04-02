using System;
using System.Threading.Tasks;
using AutoMapper;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Signify.PAD.Messages.Events;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Data;
using Signify.PAD.Svc.Core.Events;
using Signify.PAD.Svc.Core.Tests.Utilities;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Commands;

public class CreatePADTests : IClassFixture<MockDbFixture>
{
    private readonly MockDbFixture _mockDbFixture;
    private readonly IMapper _mapper;
    private readonly IPublishObservability _publishObservability;
    private readonly CreatePadHandler _handler;

    public CreatePADTests(MockDbFixture mockDbFixture)
    {
        _mockDbFixture = mockDbFixture;
        var serviceProvider = A.Fake<IServiceProvider>();
        _mapper = A.Fake<IMapper>();
        _publishObservability = A.Fake<IPublishObservability>();

        var serviceScopeFactory = A.Fake<IServiceScopeFactory>();
        var serviceScope = A.Fake<IServiceScope>();
        var scopedServiceProvider = A.Fake<IServiceProvider>();

        A.CallTo(() => serviceProvider.GetService(typeof(IServiceScopeFactory))).Returns(serviceScopeFactory);
        A.CallTo(() => serviceScopeFactory.CreateScope()).Returns(serviceScope);
        A.CallTo(() => serviceScope.ServiceProvider).Returns(scopedServiceProvider);
        A.CallTo(() => scopedServiceProvider.GetService(typeof(PADDataContext))).Returns(mockDbFixture.Context);

        _handler = new CreatePadHandler(A.Dummy<ILogger<CreatePadHandler>>(), _mapper, serviceProvider, mockDbFixture.Context, _publishObservability);
    }

    [Fact]
    public async Task Handle_Pad_Evaluation_Received()
    {
        //324356
        var evalReceived = new CreatePad
        {
            EvaluationId = 1,
            EvaluationTypeId = 1,
            FormVersionId = 1,
            ProviderId = 1,
            UserName = "Amelia",
            AppointmentId = 1,
            ApplicationId = "1",
            MemberPlanId = 1,
            MemberId = 1,
            ClientId = 1,
            DocumentPath = "Path/Path",
            CreatedDateTime = DateTimeOffset.Now,
            ReceivedDateTime = DateTimeOffset.Now,
            DateOfService = DateTime.Now,
            Products =
            [
                new Product
                {
                    ProductCode = "PAD"
                }
            ]
        };
        await _handler.Handle(evalReceived, new TestableInvokeHandlerContext());

        A.CallTo(() => _mapper.Map<EvalReceived>(A<CreatePad>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Handle_Pad_Evaluation_UpdateDateOfService()
    {
        var evalReceived = new CreatePad
        {
            EvaluationId = 324356,
            EvaluationTypeId = 1,
            FormVersionId = 1,
            ProviderId = 1,
            UserName = "Amelia",
            AppointmentId = 1,
            ApplicationId = "1",
            MemberPlanId = 1,
            MemberId = 1,
            ClientId = 1,
            DocumentPath = "Path/Path",
            CreatedDateTime = DateTimeOffset.Now,
            ReceivedDateTime = DateTimeOffset.Now,
            DateOfService = DateTime.Now,
            Products =
            [
                new Product
                {
                    ProductCode = "PAD"
                }
            ]
        };
        await _handler.Handle(evalReceived, new TestableInvokeHandlerContext());

        A.CallTo(() => _mapper.Map<EvalReceived>(A<CreatePad>._)).MustNotHaveHappened();
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>._, false)).MustNotHaveHappened();
    }
    
    [Fact]
    public async Task Handle_Pad_Evaluation_AlreadyUpdatedDateOfService()
    {
        var testing = await _mockDbFixture.Context.PAD.FindAsync(1);
        var evalReceived = new CreatePad
        {
            EvaluationId = 324356,
            EvaluationTypeId = 1,
            FormVersionId = 1,
            ProviderId = 1,
            UserName = "Amelia",
            AppointmentId = 1,
            ApplicationId = "1",
            MemberPlanId = 1,
            MemberId = 1,
            ClientId = 1,
            DocumentPath = "Path/Path",
            CreatedDateTime = DateTimeOffset.Now,
            ReceivedDateTime = DateTimeOffset.Now,
            DateOfService = testing.DateOfService,
            Products =
            [
                new Product
                {
                    ProductCode = "PAD"
                }
            ]
        };
        await _handler.Handle(evalReceived, new TestableInvokeHandlerContext());

        A.CallTo(() => _mapper.Map<EvalReceived>(A<CreatePad>._)).MustNotHaveHappened();
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>._, true)).MustHaveHappened();
    }
}