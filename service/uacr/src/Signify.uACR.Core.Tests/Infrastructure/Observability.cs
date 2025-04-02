using System;
using System.Collections.Generic;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Xunit;

namespace Signify.uACR.Core.Tests.Infrastructure;

public class PublishObservabilityTests
{
    private readonly IObservabilityService _observabilityService = A.Fake<IObservabilityService>();
    private readonly ILogger<PublishObservability> _logger = A.Fake<ILogger<PublishObservability>>();

    private PublishObservability CreateSubject() => new(_logger, _observabilityService);
    
    private static ObservabilityEvent CreateObservabilityEvent()
    {
        var eventValue = new Dictionary<string, object>
        {
            { "ID", 1 },
            { "Type", 2 }
        };
            
        return new ObservabilityEvent
        {
            EvaluationId = 12345,
            EventId = Guid.NewGuid(),
            EventType = "x",
            EventValue = eventValue
        };
    }

    [Fact]
    public void RegisterEvent_Null_EventNotRegistered()
    {
        var p = CreateSubject();

        p.RegisterEvent(null!);

        A.CallTo(() => _observabilityService.AddEvent(null, null)).WithAnyArguments().MustNotHaveHappened();
    }

    [Fact]
    public void RegisterEvent_Event_AddToList()
    {
        var createObservabilityEvent = CreateObservabilityEvent();
        var p = CreateSubject();

        p.RegisterEvent(createObservabilityEvent);

        A.CallTo(() => _observabilityService.AddEvent(null, null)).WithAnyArguments().MustNotHaveHappened();
    }

    [Fact]
    public void RegisterEvent_Event_Registered()
    {
        var createObservabilityEvent = CreateObservabilityEvent();
        var p = CreateSubject();

        p.RegisterEvent(createObservabilityEvent, true);

        A.CallTo(() => _observabilityService.AddEvent(null, null)).WithAnyArguments().MustHaveHappened();
    }

    [Fact]
    public void Commit_NullEvent_AddEventNotCalled()
    {
        var p = CreateSubject();
        p.RegisterEvent(null!);

        p.Commit();

        A.CallTo(() => _observabilityService.AddEvent(null, null)).WithAnyArguments().MustNotHaveHappened();
    }

    [Fact]
    public void Commit_Event_AddEventCalledAndListEmptied()
    {
        var p = CreateSubject();
        p.RegisterEvent(CreateObservabilityEvent());

        p.Commit();

        A.CallTo(() => _observabilityService.AddEvent(null, null)).WithAnyArguments().MustHaveHappenedOnceExactly();
    }
}