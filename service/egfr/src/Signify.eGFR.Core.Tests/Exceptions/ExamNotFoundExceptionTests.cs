using System;
using Signify.eGFR.Core.Exceptions;
using Xunit;

namespace Signify.eGFR.Core.Tests.Exceptions;

public class ExamNotFoundExceptionTests
{
    [Fact]
    public void Constructor_SetsProperties_Test()
    {
        const string censeoId = "test";
        DateTimeOffset? collectionDate = new DateTimeOffset(DateTime.UtcNow);

        var ex = new ExamNotFoundException(censeoId, collectionDate);

        Assert.Equal(censeoId, ex.CenseoId);
        Assert.Equal(collectionDate, ex.CollectionDate);
    }
}