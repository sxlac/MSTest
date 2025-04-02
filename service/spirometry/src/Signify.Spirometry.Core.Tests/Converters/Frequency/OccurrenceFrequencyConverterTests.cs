using Signify.Spirometry.Core.Converters.Frequency;
using Signify.Spirometry.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Converters.Frequency;

public class OccurrenceFrequencyConverterTests
{
    /// <summary>
    /// Dummy concrete type converter for tests that don't need a real implementation defined in the app
    /// </summary>
    private class ConcreteConverter : OccurrenceFrequencyConverter
    {
        public ConcreteConverter(
            int neverAnswerId,
            int rarelyAnswerId,
            int sometimesAnswerId,
            int oftenAnswerId,
            int veryOftenAnswerId)
            : base(
                neverAnswerId,
                rarelyAnswerId,
                sometimesAnswerId,
                oftenAnswerId,
                veryOftenAnswerId)
        {
        }
    }

    private const int NeverAnswerId = 1;
    private const int RarelyAnswerId = 2;
    private const int SometimesAnswerId = 3;
    private const int OftenAnswerId = 4;
    private const int VeryOftenAnswerId = 5;

    [Theory]
    [InlineData(1, 1, 1, 1, 1)]
    [InlineData(1, 2, 3, 3, 4)]
    [InlineData(1, 2, 3, 4, 1)]
    public void Construct_WithSameAnswerIds_ThrowsArgEx(
        int neverAnswerId,
        int rarelyAnswerId,
        int sometimesAnswerId,
        int oftenAnswerId,
        int veryOftenAnswerId)
    {
        Assert.Throws<ArgumentException>(() => new ConcreteConverter(
            neverAnswerId,
            rarelyAnswerId,
            sometimesAnswerId,
            oftenAnswerId,
            veryOftenAnswerId));
    }

    [Theory]
    [InlineData(NeverAnswerId, OccurrenceFrequency.Never)]
    [InlineData(RarelyAnswerId, OccurrenceFrequency.Rarely)]
    [InlineData(SometimesAnswerId, OccurrenceFrequency.Sometimes)]
    [InlineData(OftenAnswerId, OccurrenceFrequency.Often)]
    [InlineData(VeryOftenAnswerId, OccurrenceFrequency.VeryOften)]
    public void TryConvert_WithValidAnswerId_ReturnsCorrectFrequency(int answerId, OccurrenceFrequency expected)
    {
        var subject = new ConcreteConverter(
            NeverAnswerId,
            RarelyAnswerId,
            SometimesAnswerId,
            OftenAnswerId,
            VeryOftenAnswerId);

        Assert.True(subject.TryConvert(answerId, out var actual));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TryConvert_WithInvalidAnswerId_ReturnsFalse()
    {
        var subject = new ConcreteConverter(
            NeverAnswerId,
            RarelyAnswerId,
            SometimesAnswerId,
            OftenAnswerId,
            VeryOftenAnswerId);

        Assert.False(subject.TryConvert(100, out _));
    }

    [Fact]
    public void NoConcreteConverters_ShareSameAnswerId()
    {
        var iFrequencyConverterType = typeof(IOccurrenceFrequencyConverter);

        bool Matches(Type type)
        {
            return type.Namespace == iFrequencyConverterType.Namespace
                   && type.Name == iFrequencyConverterType.Name;
        }

        IEnumerable<Type> GetConcreteTypes()
        {
            return typeof(OccurrenceFrequencyConverter).Assembly
                .GetTypes()
                .Where(type => type.GetInterfaces().Any(Matches) && !type.IsAbstract);
        }

        var foundAnswerIds = new HashSet<int>();
        foreach (var type in GetConcreteTypes())
        {
            var concreteInstance = (IOccurrenceFrequencyConverter) Activator.CreateInstance(type);

            Assert.True(concreteInstance != null, "This test needs updating; likely the concrete instance no longer has a default constructor so this activator was unable to instantiate an instance of the type");

            Assert.True(foundAnswerIds.Add(concreteInstance.NeverAnswerId));
            Assert.True(foundAnswerIds.Add(concreteInstance.RarelyAnswerId));
            Assert.True(foundAnswerIds.Add(concreteInstance.SometimesAnswerId));
            Assert.True(foundAnswerIds.Add(concreteInstance.OftenAnswerId));
            Assert.True(foundAnswerIds.Add(concreteInstance.VeryOftenAnswerId));
        }
    }
}