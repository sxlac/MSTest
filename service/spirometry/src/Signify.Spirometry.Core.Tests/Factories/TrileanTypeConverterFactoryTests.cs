using Signify.Spirometry.Core.Converters.Trilean;
using Signify.Spirometry.Core.Factories;
using System.Collections.Generic;
using System.Linq;
using System;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Factories;

public class TrileanTypeConverterFactoryTests
{
    [Fact]
    public void Create_WithAllTrileanConverterTypes_AreHandled()
    {
        var subject = new TrileanTypeConverterFactory();

        var values = Enum.GetValues<ITrileanTypeConverterFactory.TrileanConverterType>();

        foreach (var type in values)
        {
            Assert.NotNull(subject.Create(type));
        }
    }

    [Fact]
    public void Create_WithAllTrileanConverterTypes_NeverReturnsMultipleOfSameType()
    {
        var subject = new TrileanTypeConverterFactory();

        var converterTypesFound = new HashSet<Type>();

        var values = Enum.GetValues<ITrileanTypeConverterFactory.TrileanConverterType>();

        foreach (var type in values)
        {
            var converterType = subject.Create(type).GetType();

            Assert.True(converterTypesFound.Add(converterType));
        }
    }

    [Fact]
    public void Factory_CanCreate_AllFrequencyConverters()
    {
        var typesSupportedByFactory = Enum.GetValues<ITrileanTypeConverterFactory.TrileanConverterType>();

        var iTrileanTypeConverterType = typeof(ITrileanTypeConverter);

        bool Matches(Type type)
        {
            return type.Namespace == iTrileanTypeConverterType.Namespace
                   && type.Name == iTrileanTypeConverterType.Name;
        }

        // Find all types in the assembly that implement ITrileanTypeConverter
        var concreteConverterTypes = typeof(TrileanTypeConverterFactory).Assembly
            .GetTypes()
            .Where(type => type.GetInterfaces().Any(Matches) && !type.IsAbstract)
            .ToList();

        Assert.Equal(typesSupportedByFactory.Length, concreteConverterTypes.Count);
    }
}