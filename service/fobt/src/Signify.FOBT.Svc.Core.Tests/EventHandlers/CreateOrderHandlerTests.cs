using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Refit;
using Signify.FOBT.Messages.Events;
using Signify.FOBT.Svc.Core.ApiClient.Requests;
using Signify.FOBT.Svc.Core.ApiClient;
using Signify.FOBT.Svc.Core.Commands;
using Signify.FOBT.Svc.Core.EventHandlers;
using Signify.FOBT.Svc.Core.Exceptions;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.EventHandlers;

public class CreateOrderHandlerTests
{
    private readonly IMediator _mediator;
    private readonly CreateOrderHandler _handler;
    private readonly ILabsApi _labsApi;

    public CreateOrderHandlerTests()
    {
        var logger = A.Fake<ILogger<CreateOrderHandler>>();
        _mediator = A.Fake<IMediator>();
        var mapper = A.Fake<IMapper>();
        _labsApi = A.Fake<ILabsApi>();
        _handler = new CreateOrderHandler(logger, mapper, _mediator, _labsApi);
    }

    [Fact]
    public async Task Should_CreateOrder()
    {
        A.CallTo(() => _labsApi.CreateOrder(A<CreateOrder>._)).Returns(new ApiResponse<int>(new HttpResponseMessage(), 1234, new RefitSettings()));

        await _handler.Handle(new CreateOrderEvent(), new TestableInvokeHandlerContext());
        
        A.CallTo(() => _mediator.Send(A<CreateFOBTStatus>._, CancellationToken.None)).MustHaveHappened();
    }

    [Fact]
    public async Task Should_ThrowException_WhenLabsApiCallFails()
    {
        await Assert.ThrowsAsync<CreateOrderException>(async () => await _handler.Handle(new CreateOrderEvent(), new TestableInvokeHandlerContext()));
    }
}