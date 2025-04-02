using System;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Signify.AkkaStreams.Kafka;
using Signify.uACR.Core.Data;
using Xunit;

namespace Signify.uACR.Core.Tests.Data;

public class TransactionSupplierTests
{
    private readonly IMessageProducer _producer = A.Fake<IMessageProducer>();
    private readonly MockDbFixture _dbFixture = new();

    private TransactionSupplier CreateSubject()
        => new(_dbFixture.SharedDbContext, _producer);

    public TransactionSupplierTests()
    {
        // These setups are needed because of the way Signify.AkkaStreams.Postgres.BufferedTransactionExtensions.BeginTransaction uses ActivatorUtilities.CreateInstance
        var sp = A.Fake<IServiceProvider>();

        A.CallTo(() => sp.GetService(typeof(IServiceProviderIsService)))
            .Returns(A.Dummy<IServiceProviderIsService>());

        A.CallTo(() => _producer.ServiceProvider)
            .Returns(sp);
    }
    
    [Fact]
    public void CurrentTransaction_BeforeCallingBeginTransaction_IsNull()
    {
        // Arrange
        var subject = CreateSubject();

        // Act
        // Assert
        Assert.NotNull(subject.BeginTransaction());
    }
}