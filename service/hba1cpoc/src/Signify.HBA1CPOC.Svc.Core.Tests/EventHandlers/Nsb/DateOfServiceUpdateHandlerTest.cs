using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.HBA1CPOC.Messages.Events;
using Signify.HBA1CPOC.Svc.Core.Commands;
using Signify.HBA1CPOC.Svc.Core.EventHandlers;
using Signify.HBA1CPOC.Svc.Core.Tests.Mocks.StaticEntity;
using Signify.HBA1CPOC.Svc.Core.Tests.Utilities;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.EventHandlers.Nsb;

public class DateOfServiceUpdateHandlerTest : IClassFixture<MockDbFixture>
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly DateOfServiceUpdateHandler _dateOfServiceUpdateHandler;
    private readonly TestableMessageHandlerContext _messageHandlerContext;
    private readonly MockDbFixture _mockDbFixture;
    private readonly CreateOrUpdateHBA1CPOCHandler _subject;

    public DateOfServiceUpdateHandlerTest(MockDbFixture mockDbFixture)
    {
        var logger = A.Fake<ILogger<DateOfServiceUpdateHandler>>();
        _mediator = A.Fake<IMediator>();
        _mapper = A.Fake<IMapper>();
        _mockDbFixture = mockDbFixture;
        _dateOfServiceUpdateHandler = new DateOfServiceUpdateHandler(logger, _mediator, mockDbFixture.Context, _mapper);
        _messageHandlerContext = new TestableMessageHandlerContext();
        _subject = new CreateOrUpdateHBA1CPOCHandler(mockDbFixture.Context, _mapper, A.Dummy<ILogger<CreateOrUpdateHBA1CPOCHandler>>());
    }

    [Fact]
    public async Task DateOfServiceUpdateHandler_Update()
    {
        //Arrange
        //Act
        await _dateOfServiceUpdateHandler.Handle(new DateOfServiceUpdated(324357, DateTime.UtcNow), _messageHandlerContext);

        //Assert
        A.CallTo(() => _mapper.Map<CreateOrUpdateHBA1CPOC>(A<Core.Data.Entities.HBA1CPOC>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateOrUpdateHBA1CPOC>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task DateOfServiceUpdateHandler_CreateOrUpdateHBA1CPocHandler_FullTest()
    {
        //Arrange
        var now = DateTime.UtcNow.AddDays(1);
        var exam = _mockDbFixture.Context.HBA1CPOC.AsNoTracking().FirstOrDefault();
        exam!.DateOfService = now;
        var dos = new DateOfServiceUpdated(exam.EvaluationId ?? 324356, now);
        var create = StaticMockEntities.CreateOrUpdateHba1Cpoc;
        create.HBA1CPOCId = exam.HBA1CPOCId;
        if (exam.EvaluationId != null) create.EvaluationId = exam.EvaluationId.Value;
        A.CallTo(() => _mapper.Map<CreateOrUpdateHBA1CPOC>(A<Core.Data.Entities.HBA1CPOC>._)).Returns(create);
        A.CallTo(() => _mapper.Map<Core.Data.Entities.HBA1CPOC>(A<CreateOrUpdateHBA1CPOC>._)).Returns(exam);
        _mockDbFixture.Context.ChangeTracker.Clear();

        //Act
        await _dateOfServiceUpdateHandler.Handle(dos, _messageHandlerContext);
        var createResult = await _subject.Handle(create, CancellationToken.None);

        //Assert
        createResult.DateOfService.Should().Be(now);
        A.CallTo(() => _mediator.Send(A<CreateOrUpdateHBA1CPOC>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
        var result = _mockDbFixture.Context.HBA1CPOC.AsNoTracking().FirstOrDefault();
        result!.DateOfService.Should().Be(now);
    }
}