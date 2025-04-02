using FakeItEasy;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Events;
using Signify.DEE.Svc.Core.Filters;
using Signify.DEE.Svc.Core.Infrastructure;
using Signify.DEE.Svc.Core.Maps;
using System;
using System.Collections.Generic;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Maps;

public class HoldMapperTests
{
    private const string Dee = "dee";

    private readonly DateTime _productHoldExpiresAt = DateTime.UtcNow;

    private readonly IApplicationTime _applicationTime = new FakeApplicationTime();
    private readonly IProductFilter _filter = A.Fake<IProductFilter>();

    public HoldMapperTests()
    {
        A.CallTo(() => _filter.ShouldProcess(A<ProductHold>._))
            .Returns(false);
        A.CallTo(() => _filter.ShouldProcess(A<ProductHold>.That.Matches(h => h.Code == Dee)))
            .Returns(true);
    }

    private HoldMapper CreateSubject() => new(_applicationTime, _filter);

    private List<ProductHold> GetProductHolds()
    {
        return new List<ProductHold>
        {
            new()
            {
                Code = "not" + Dee,
                ExpiresAt = DateTime.UtcNow
            },
            new()
            {
                Code = Dee,
                ExpiresAt = _productHoldExpiresAt
            }
        };
    }

    private CDIEvaluationHeldEvent GetSource()
    {
        return new CDIEvaluationHeldEvent
        {
            Products = GetProductHolds()
        };
    }

    [Fact]
    public void Convert_FromHeldEvent_ReturnsSameInstanceAsUpdated()
    {
        var destination = new Hold();

        var actual = CreateSubject().Convert(GetSource(), destination, default);

        Assert.Equal(destination, actual);
    }

    [Fact]
    public void Convert_FromHeldEvent_WithNullDestination_ReturnsNotNull()
    {
        var actual = CreateSubject().Convert(GetSource(), null, default);

        Assert.NotNull(actual);
    }

    [Fact]
    public void Convert_FromHeldEvent_HappyPathTest()
    {
        // Arrange
        const int evaluationId = 1;
        var holdId = Guid.NewGuid();

        var heldOn = DateTime.UtcNow;
        var sentAt = DateTime.UtcNow;

        var source = GetSource();
        source.EvaluationId = evaluationId;
        source.HoldId = holdId;
        source.HeldOn = heldOn;
        source.SentAt = sentAt;

        // Act
        var destination = CreateSubject().Convert(source, default, default);

        // Assert
        Assert.Equal(evaluationId, destination.EvaluationId);
        Assert.Equal(holdId, destination.CdiHoldId);

        Assert.Equal(_productHoldExpiresAt, destination.ExpiresAt);
        Assert.Equal(heldOn, destination.HeldOnDateTime);
        Assert.Equal(sentAt, destination.SentAtDateTime);

        Assert.Equal(_applicationTime.UtcNow(), destination.CreatedDateTime);

        A.CallTo(() => _filter.ShouldProcess(A<ProductHold>._))
            .MustHaveHappenedTwiceExactly();
    }
}