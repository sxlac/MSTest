using System;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Signify.AkkaStreams.Kafka;
using Signify.Spirometry.Core.Data;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Data;

public class TransactionSupplierTests
{
    private readonly IMessageProducer _producer = A.Fake<IMessageProducer>();
    private readonly MockDbFixture _mockDbFixture = new ();

    private TransactionSupplier CreateSubject()
        => new(_mockDbFixture.SharedDbContext, _producer);

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
        // Act
        var subject = CreateSubject();

        // Assert
        Assert.NotNull(subject.BeginTransaction());
    }
}