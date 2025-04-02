using FakeItEasy;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Events;
using Signify.Spirometry.Core.Filters;
using Signify.Spirometry.Core.Infrastructure;
using Signify.Spirometry.Core.Maps;
using System.Collections.Generic;
using System;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Maps;

public class HoldMapperTests
{
    private const string Spiro = "spirometry";

    private readonly DateTime _productHoldExpiresAt = DateTime.UtcNow;

    private readonly IApplicationTime _applicationTime = new FakeApplicationTime();
    private readonly IProductFilter _filter = A.Fake<IProductFilter>();

    public HoldMapperTests()
    {
        A.CallTo(() => _filter.ShouldProcess(A<ProductHold>._))
            .Returns(false);
        A.CallTo(() => _filter.ShouldProcess(A<ProductHold>.That.Matches(h => h.Code == Spiro)))
            .Returns(true);
    }

    private HoldMapper CreateSubject() => new(_applicationTime, _filter);

    private List<ProductHold> GetProductHolds()
    {
        return new List<ProductHold>
        {
            new()
            {
                Code = "not" + Spiro,
                ExpiresAt = DateTime.UtcNow
            },
            new()
            {
                Code = Spiro,
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