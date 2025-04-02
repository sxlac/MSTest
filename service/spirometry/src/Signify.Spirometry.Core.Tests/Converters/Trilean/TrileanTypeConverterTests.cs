using Signify.Spirometry.Core.Converters.Trilean;
using Signify.Spirometry.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Converters.Trilean;

public class TrileanTypeConverterTests
{
    /// <summary>
    /// Dummy concrete type converter for tests that don't need a real implementation defined in the app
    /// </summary>
    private class ConcreteConverter : TrileanTypeConverter
    {
        public ConcreteConverter(int unknownAnswerId, int yesAnswerId, int noAnswerId)
            : base(unknownAnswerId, yesAnswerId, noAnswerId)
        {
        }
    }

    private const int Yes = 1;
    private const int No = 2;
    private const int Unknown = 3;

    [Theory]
    [InlineData(1, 1, 2)]
    [InlineData(1, 2, 1)]
    [InlineData(2, 1, 1)]
    [InlineData(1, 1, 1)]
    public void Construct_WithSameAnswerIds_ThrowsArgEx(int unknownAnswerId, int yesAnswerId, int noAnswerId)
    {
        Assert.Throws<ArgumentException>(() =>
            new ConcreteConverter(unknownAnswerId, yesAnswerId, noAnswerId));
    }

    [Theory]
    [InlineData(Yes, TrileanType.Yes)]
    [InlineData(No, TrileanType.No)]
    [InlineData(Unknown, TrileanType.Unknown)]
    public void TryConvert_WithValidAnswerId_ReturnsCorrectTrileanType(int answerId, TrileanType expectedType)
    {
        var subject = new ConcreteConverter(Unknown, Yes, No);

        Assert.True(subject.TryConvert(answerId, out var actualType));

        Assert.Equal(expectedType, actualType);
    }

    [Fact]
    public void TryConvert_WithInvalidAnswerId_ReturnsFalse()
    {
        var subject = new ConcreteConverter(Unknown, Yes, No);

        Assert.False(subject.TryConvert(100, out _));
    }

    [Fact]
    public void NoConcreteConverters_ShareSameAnswerId()
    {
        var iTrileanTypeConverterType = typeof(ITrileanTypeConverter);

        bool Matches(Type type)
        {
            return type.Namespace == iTrileanTypeConverterType.Namespace
                   && type.Name == iTrileanTypeConverterType.Name;
        }

        IEnumerable<Type> GetConcreteTypes()
        {
            return typeof(TrileanTypeConverter).Assembly
                .GetTypes()
                .Where(type => type.GetInterfaces().Any(Matches) && !type.IsAbstract);
        }

        var foundAnswerIds = new HashSet<int>();
        foreach (var type in GetConcreteTypes())
        {
            var concreteInstance = (ITrileanTypeConverter)Activator.CreateInstance(type);

            Assert.True(concreteInstance != null, "This test needs updating; likely the concrete instance no longer has a default constructor so this activator was unable to instantiate an instance of the type");

            Assert.True(foundAnswerIds.Add(concreteInstance.UnknownAnswerId));
            Assert.True(foundAnswerIds.Add(concreteInstance.YesAnswerId));
            Assert.True(foundAnswerIds.Add(concreteInstance.NoAnswerId));
        }
    }
}