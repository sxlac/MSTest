using Signify.Spirometry.Core.Converters.Frequency;
using Signify.Spirometry.Core.Factories;
using System.Collections.Generic;
using System.Linq;
using System;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Factories;

public class OccurrenceFrequencyConverterFactoryTests
{
    [Fact]
    public void Create_WithAllFrequencyConverterTypes_AreHandled()
    {
        var subject = new OccurrenceFrequencyConverterFactory();

        var values = Enum.GetValues<IOccurrenceFrequencyConverterFactory.FrequencyConverterType>();

        foreach (var type in values)
        {
            Assert.NotNull(subject.Create(type));
        }
    }

    [Fact]
    public void Create_WithAllFrequencyConverterTypes_NeverReturnsMultipleOfSameType()
    {
        var subject = new OccurrenceFrequencyConverterFactory();

        var converterTypesFound = new HashSet<Type>();

        var values = Enum.GetValues<IOccurrenceFrequencyConverterFactory.FrequencyConverterType>();

        foreach (var type in values)
        {
            var converterType = subject.Create(type).GetType();

            Assert.True(converterTypesFound.Add(converterType));
        }
    }

    [Fact]
    public void Factory_CanCreate_AllFrequencyConverters()
    {
        var typesSupportedByFactory = Enum.GetValues<IOccurrenceFrequencyConverterFactory.FrequencyConverterType>();

        var iOccurrenceFrequencyConverterType = typeof(IOccurrenceFrequencyConverter);

        bool Matches(Type type)
        {
            return type.Namespace == iOccurrenceFrequencyConverterType.Namespace
                   && type.Name == iOccurrenceFrequencyConverterType.Name;
        }

        // Find all types in the assembly that implement IOccurrenceFrequencyConverter
        var concreteConverterTypes = typeof(OccurrenceFrequencyConverterFactory).Assembly
            .GetTypes()
            .Where(type => type.GetInterfaces().Any(Matches) && !type.IsAbstract)
            .ToList();

        Assert.Equal(typesSupportedByFactory.Length, concreteConverterTypes.Count);
    }
}