using FakeItEasy;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using NServiceBus;
using Signify.Spirometry.Core.Sagas.Models;
using SpiroNsb.SagaCommands;
using SpiroNsb.SagaEvents;
using SpiroNsb.Sagas;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Sagas;

public class ProviderPaySagaTests
{
    private const long EvaluationId = 129837; // Just use a number unlikely to conflict with other test data

    private static readonly DateTime FinalizedProcessedDateTime = new(2023, 01, 01, 0, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime OverreadProcessedDateTime = new(2023, 01, 04, 0, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime CdiEventReceivedDateTime = new(2023, 01, 07, 0, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime ProviderPaidEventDateTime = new(2023, 01, 08, 0, 0, 0, DateTimeKind.Utc);
    
    private static ProviderPaySaga CreateSubject()
    {
        return new ProviderPaySaga(A.Dummy<ILogger<ProviderPaySaga>>())
        {
            Data = new ProviderPaySagaData()
        };
    }

    [Theory]
    [MemberData(nameof(Handle_EvaluationProcessedEventForPayment_SagaDataTestData))]
    public async Task Handle_EvaluationProcessedEventForPayment_SagaDataTests(EvaluationProcessedEventForPayment message)
    {
        // Arrange
        var context = new TestableMessageHandlerContext();

        var subject = CreateSubject();

        // Act
        await subject.Handle(message, context);

        // Assert
        Assert.Equal(message.IsPerformed, subject.Data.IsPerformed);
        Assert.Equal(message.SpirometryExamId, subject.Data.SpirometryExamId);

        Assert.Equal(FinalizedProcessedDateTime, subject.Data.FinalizedProcessedDateTime);

        if (message.IsPerformed)
        {
            Assert.Equal(message.NeedsOverread, subject.Data.NeedsOverread);
            Assert.Equal(message.IsPayable, subject.Data.IsPayable);
        }
        else
        {
            Assert.False(subject.Data.NeedsOverread);
            Assert.False(subject.Data.IsPayable);
        }

        Assert.False(subject.Completed);
    }

    public static IEnumerable<object[]> Handle_EvaluationProcessedEventForPayment_SagaDataTestData()
    {
        yield return
        [
            new EvaluationProcessedEventForPayment
            {
                SpirometryExamId = 1,
                IsPerformed = true,
                NeedsOverread = true,
                IsPayable = null,
                CreatedDateTime = FinalizedProcessedDateTime
            }
        ];

        yield return
        [
            new EvaluationProcessedEventForPayment
            {
                SpirometryExamId = 1,
                IsPerformed = true,
                NeedsOverread = false,
                IsPayable = true,
                CreatedDateTime = FinalizedProcessedDateTime
            }
        ];

        yield return
        [
            new EvaluationProcessedEventForPayment
            {
                SpirometryExamId = 1,
                IsPerformed = true,
                NeedsOverread = false,
                IsPayable = false,
                CreatedDateTime = FinalizedProcessedDateTime
            }
        ];

        yield return
        [
            new EvaluationProcessedEventForPayment
            {
                SpirometryExamId = 1,
                IsPerformed = false,
                NeedsOverread = true, // Although `true` here (which is not valid when Not Performed), the test validates this is ignored because `IsPerformed` is `false`
                IsPayable = true, // Although `true` here (which is not valid when Not Performed), the test validates this is ignored because `IsPerformed` is `false`
                CreatedDateTime = FinalizedProcessedDateTime
            }
        ];
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handle_OverreadProcessedEventForPayment_SagaDataTests(bool isPayable)
    {
        // Arrange
        var message = new OverreadProcessedEventForPayment
        {
            IsPayable = isPayable,
            CreatedDateTime = OverreadProcessedDateTime
        };

        var context = new TestableMessageHandlerContext();

        var subject = CreateSubject();

        // Act
        await subject.Handle(message, context);

        // Assert
        Assert.Equal(OverreadProcessedDateTime, subject.Data.OverreadProcessedDateTime);
        Assert.Equal(message.IsPayable, subject.Data.IsPayable);

        Assert.False(subject.Completed);
    }

    /// <param name="message"></param>
    /// <param name="sagaDataSetup">Optional setup for the saga data, to be executed before the message is handled</param>
    /// <param name="sagaDataIsValid">Optional function to perform saga data validation</param>
    [Theory]
    [MemberData(nameof(Handle_CdiEventForPaymentReceived_SagaDataTestData))]
    public async Task Handle_CdiEventForPaymentReceived_SagaDataTests(CdiEventForPaymentReceived message,
        Action<ProviderPaySagaData> sagaDataSetup = null, Func<ProviderPaySagaData, bool> sagaDataIsValid = null)
    {
        // Arrange
        var context = new TestableMessageHandlerContext();

        var subject = CreateSubject();

        sagaDataSetup?.Invoke(subject.Data);

        // Act
        await subject.Handle(message, context);

        // Assert
        if (sagaDataIsValid != null)
            Assert.True(sagaDataIsValid.Invoke(subject.Data));
        else
        {
            Assert.Equal(message.CreatedDateTime, subject.Data.CdiEventReceivedDateTime);

            Assert.False(subject.Completed);
        }
    }

    public static IEnumerable<object[]> Handle_CdiEventForPaymentReceived_SagaDataTestData()
    {
        // No need to supply EvaluationId on the events because that's just used
        // to find the saga; it never gets set directly by handling the event
        yield return
        [
            new CdiEventForPaymentReceived
            {
                CreatedDateTime = CdiEventReceivedDateTime
            }
        ];

        yield return
        [
            // Raise new CdiEventForPaymentReceived event
            new CdiEventForPaymentReceived
            {
                CreatedDateTime = DateTime.UtcNow
            },
            // Setup the saga data as if a CdiEventForPaymentReceived event was already handled
            delegate(ProviderPaySagaData sagaData) { sagaData.CdiEventReceivedDateTime = CdiEventReceivedDateTime; },
            // Verify the saga data properties are updated for each new CdiEventForPaymentReceived event
            delegate(ProviderPaySagaData sagaData) { return sagaData.CdiEventReceivedDateTime != CdiEventReceivedDateTime; }
        ];
    }

    [Theory]
    [MemberData(nameof(Handle_ProviderPaidEvent_SagaDataTestData))]
    public async Task Handle_ProviderPaidEvent_SagaDataTests(ProviderPaidEvent message)
    {
        // Arrange
        var context = new TestableMessageHandlerContext();

        var subject = CreateSubject();

        // Act
        await subject.Handle(message, context);

        // Assert
        Assert.True(subject.Data.IsPaymentComplete);

        Assert.True(subject.Completed);
    }

    public static IEnumerable<object[]> Handle_ProviderPaidEvent_SagaDataTestData()
    {
        // No need to supply EvaluationId on the events because that's just used
        // to find the saga; it never gets set directly by handling the event
        yield return
        [
            new ProviderPaidEvent
            {
                CreatedDateTime = ProviderPaidEventDateTime
            }
        ];
    }

    /*
     * This section contains all the test scenarios to cover receiving events out-of-order,
     * and verifying what NSB commands are raised after each message is handled.
     */

    #region Verify Saga Event Flow

    private class SagaStateChange
    {
        /// <summary>
        /// The action to perform on the saga that results in your state change.
        /// This generally should be a call to a `Handle` method to handle an `ISagaEvent`.
        /// </summary>
        public Func<ProviderPaySaga, IMessageHandlerContext, Task> SagaAction { get; init; }

        /// <summary>
        /// Action containing your assertions on the saga data state, which gets
        /// called after <see cref="SagaAction"/>
        /// </summary>
        /// <remarks>Optional</remarks>
        public Action<ProviderPaySagaData> DataAssertAction { get; init; }

        /// <summary>
        /// Action containing assertions on the message handler context, which gets
        /// called after <see cref="SagaAction"/>
        /// </summary>
        /// <remarks>Required</remarks>
        public Action<TestableMessageHandlerContext> ContextAssertAction { get; init; }

        /// <summary>
        /// Whether or not the saga should be marked as complete after processing the event
        /// </summary>
        public bool SagaShouldBeComplete { get; init; }
    }

    /// <summary>
    /// Helper method for brevity in the test data
    /// </summary>
    private static void AssertNoSentMessages(TestablePipelineContext context)
        => Assert.Empty(context.SentMessages);

    /// <summary>
    /// This sends one or more <see cref="ISagaEvent"/> messages to the subject's `Handle`
    /// method, then, after each message is handled by the test subject:
    ///
    /// 1) Runs optional assertions on the underlying <see cref="ProviderPaySaga"/>.
    /// 2) Runs assertions on the <see cref="IMessageHandlerContext"/>.
    /// 3) Asserts that the `IsComplete` flag is properly set, according to the input test data.
    ///
    /// This is to cover test cases of receiving events out-of-order, and verifying what
    /// NSB commands are raised after each message is handled.
    /// </summary>
    private static async Task VerifySagaEventFlow(IEnumerable<SagaStateChange> changes)
    {
        // Arrange
        var subject = CreateSubject();

        subject.Data.EvaluationId = EvaluationId;

        // Act
        foreach (var change in changes)
        {
            // Create a new context each time an `ISagaEvent` is sent to the test subject,
            // so that we have a different context to assert on after each message. This way
            // we don't have to deal with ignoring commands sent to the context from previous
            // changes sent to the subject.
            var context = new TestableMessageHandlerContext();

            await change.SagaAction(subject, context); // Send the change event to the subject

            // Assert
            change.DataAssertAction?.Invoke(subject.Data); // Run optional assertions on the underlying saga data

            change.ContextAssertAction.Invoke(context); // Run assertions on the context

            Assert.Equal(change.SagaShouldBeComplete, subject.Completed);
        }
    }

    #region Test Scenarios

    /// <summary>
    /// Finalized(NotPerformed) followed by CdiReceived => ProcessProviderPay(!IsPayable)
    /// </summary>
    [Fact]
    public Task EventFlow_Finalized_NotPerformed_CdiReceived_ProcessesPayProvider_NotPayable_Test()
    {
        return VerifySagaEventFlow(new[]
        {
            new SagaStateChange
            {
                SagaAction = (saga, context) => saga.Handle(new EvaluationProcessedEventForPayment
                {
                    IsPerformed = false
                }, context),
                ContextAssertAction = AssertNoSentMessages,
                SagaShouldBeComplete = false
            },
            new SagaStateChange
            {
                SagaAction = (saga, context) => saga.Handle(new CdiEventForPaymentReceived(), context),
                ContextAssertAction = delegate(TestableMessageHandlerContext context)
                {
                    Assert.Empty(context.SentMessages);

                    var message = context.FindSentMessage<ProcessProviderPay>();

                    Assert.Null(message);
                },
                SagaShouldBeComplete = false
            },
            new SagaStateChange
            {
                SagaAction = (saga, context) => saga.Handle(new ProviderPaidEvent(), context),
                ContextAssertAction = AssertNoSentMessages,
                SagaShouldBeComplete = true
            }
        });
    }

    /// <summary>
    /// Finalized(Performed, POC results Payable, !NeedsOverread) followed by CdiEventForPaymentReceived => ProcessPayProvider(Payable)
    /// </summary>
    [Fact]
    public Task EventFlow_Finalized_Performed_PocPayable_CdiReceived_ProcessesPayProvider_Payable_Test()
    {
        return VerifySagaEventFlow(new[]
        {
            new SagaStateChange
            {
                SagaAction = (saga, context) => saga.Handle(new EvaluationProcessedEventForPayment
                {
                    IsPerformed = true,
                    IsPayable = true,
                    NeedsOverread = false
                }, context),
                ContextAssertAction = AssertNoSentMessages,
                SagaShouldBeComplete = false
            },
            new SagaStateChange
            {
                SagaAction = (saga, context) => saga.Handle(new CdiEventForPaymentReceived
                {
                    CreatedDateTime = CdiEventReceivedDateTime
                }, context),
                ContextAssertAction = delegate(TestableMessageHandlerContext context)
                {
                    Assert.Single(context.SentMessages);

                    var message = context.FindSentMessage<ProcessProviderPay>();

                    Assert.NotNull(message);
                    Assert.Equal(EvaluationId, message.EvaluationId);
                    Assert.True(message.IsPayable);
                },
                SagaShouldBeComplete = false
            },
            new SagaStateChange
            {
                SagaAction = (saga, context) => saga.Handle(new ProviderPaidEvent(), context),
                DataAssertAction = delegate(ProviderPaySagaData data) { Assert.True(data.IsPaymentComplete); },
                ContextAssertAction = AssertNoSentMessages,
                SagaShouldBeComplete = true
            }
        });
    }

    /// <summary>
    /// CdiReceived followed by Finalized(NotPerformed) => ProcessesPayProvider(!IsPayable)
    /// </summary>
    [Fact]
    public Task EventFlow_CdiReceived_Finalized_NotPerformed_ProcessesPayProvider_NotPayable_Test()
    {
        return VerifySagaEventFlow(new[]
        {
            new SagaStateChange
            {
                SagaAction = (saga, context) => saga.Handle(new CdiEventForPaymentReceived(), context),
                ContextAssertAction = AssertNoSentMessages,
                SagaShouldBeComplete = false
            },
            new SagaStateChange
            {
                SagaAction = (saga, context) => saga.Handle(new EvaluationProcessedEventForPayment
                {
                    IsPerformed = false
                }, context),
                ContextAssertAction = delegate(TestableMessageHandlerContext context)
                {
                    Assert.Empty(context.SentMessages);

                    var message = context.FindSentMessage<ProcessProviderPay>();

                    Assert.Null(message);
                },
                SagaShouldBeComplete = false
            },
            new SagaStateChange
            {
                SagaAction = (saga, context) => saga.Handle(new ProviderPaidEvent(), context),
                ContextAssertAction = AssertNoSentMessages,
                SagaShouldBeComplete = true
            }
        });
    }

    /// <summary>
    /// CdiReceived followed by Finalized(Performed, IsPayable not known due to needing overread)
    /// </summary>
    [Fact]
    public Task EventFlow_CdiReceived_Finalized_Performed_PayableNotKnown_Test()
    {
        return VerifySagaEventFlow(new[]
        {
            new SagaStateChange
            {
                SagaAction = (saga, context) => saga.Handle(new CdiEventForPaymentReceived(), context),
                ContextAssertAction = AssertNoSentMessages,
                SagaShouldBeComplete = false
            },
            new SagaStateChange
            {
                SagaAction = (saga, context) => saga.Handle(new EvaluationProcessedEventForPayment
                {
                    IsPerformed = true,
                    NeedsOverread = true,
                    IsPayable = null
                }, context),
                DataAssertAction = delegate(ProviderPaySagaData data)
                {
                    Assert.True(data.IsPerformed);
                    Assert.True(data.NeedsOverread);
                    Assert.Null(data.IsPayable);
                },
                ContextAssertAction = AssertNoSentMessages, // Nothing to do because awaiting overread
                SagaShouldBeComplete = false // Awaiting overread to determine if payable, so payment can be processed
            }
        });
    }

    /// <summary>
    /// CdiReceived followed by Finalized(Performed, Not Payable (no overread required because disabled)) => ProcessProviderPay(NotPayable)
    /// </summary>
    [Fact]
    public Task EventFlow_CdiReceived_Finalized_Performed_NotPayable_ProcessesProviderPay_NotPayable_Test()
    {
        return VerifySagaEventFlow(new[]
        {
            new SagaStateChange
            {
                SagaAction = (saga, context) => saga.Handle(new CdiEventForPaymentReceived
                {
                    CreatedDateTime = CdiEventReceivedDateTime
                }, context),
                ContextAssertAction = AssertNoSentMessages,
                SagaShouldBeComplete = false
            },
            new SagaStateChange
            {
                SagaAction = (saga, context) => saga.Handle(new EvaluationProcessedEventForPayment
                {
                    IsPerformed = true,
                    IsPayable = false,
                    NeedsOverread = false // ProcessOverreads config was `false` when Finalized
                }, context),
                DataAssertAction = delegate(ProviderPaySagaData data)
                {
                    Assert.True(data.IsPerformed);
                    Assert.False(data.IsPayable);
                    Assert.False(data.NeedsOverread);
                },
                ContextAssertAction = delegate(TestableMessageHandlerContext context)
                {
                    Assert.Single(context.SentMessages);

                    var message = context.FindSentMessage<ProcessProviderPay>();

                    Assert.NotNull(message);
                    Assert.Equal(EvaluationId, message.EvaluationId);
                    Assert.False(message.IsPayable);
                },
                SagaShouldBeComplete = false
            },
            new SagaStateChange
            {
                SagaAction = (saga, context) => saga.Handle(new ProviderPaidEvent(), context),
                ContextAssertAction = AssertNoSentMessages,
                SagaShouldBeComplete = true
            }
        });
    }

    /// <summary>
    /// CdiReceived followed by Finalized(Performed, Payable, !NeedsOverread) => ProcessProviderPay(Payable)
    /// </summary>
    [Fact]
    public Task EventFlow_CdiReceived_Finalized_Performed_Payable_OverreadNotNeeded_ProcessesProviderPay_Payable_Test()
    {
        return VerifySagaEventFlow(new[]
        {
            new SagaStateChange
            {
                SagaAction = (saga, context) => saga.Handle(new CdiEventForPaymentReceived
                {
                    CreatedDateTime = CdiEventReceivedDateTime
                }, context),
                ContextAssertAction = AssertNoSentMessages,
                DataAssertAction = delegate(ProviderPaySagaData data) { Assert.Equal(EvaluationId, data.EvaluationId); },
                SagaShouldBeComplete = false
            },
            new SagaStateChange
            {
                SagaAction = (saga, context) => saga.Handle(new EvaluationProcessedEventForPayment
                {
                    IsPerformed = true,
                    IsPayable = true,
                    NeedsOverread = false
                }, context),
                ContextAssertAction = delegate(TestableMessageHandlerContext context)
                {
                    Assert.Single(context.SentMessages);

                    var message = context.FindSentMessage<ProcessProviderPay>();

                    Assert.NotNull(message);
                    Assert.Equal(EvaluationId, message.EvaluationId);
                    Assert.True(message.IsPayable);
                },
                SagaShouldBeComplete = false
            },
            new SagaStateChange
            {
                SagaAction = (saga, context) => saga.Handle(new ProviderPaidEvent(), context),
                ContextAssertAction = AssertNoSentMessages,
                SagaShouldBeComplete = true
            }
        });
    }

    /// <summary>
    /// Finalized(Performed, !Payable, NeedsOverread) followed by OverreadProcessed
    /// then CdiReceived => ProcessProviderPay
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public Task
        EventFlow_Finalized_Performed_NotPayable_NeedsOverread_OverreadReceived_ProcessesOverread_OverreadProcessed_Payable_CdiReceived_ProcessesProviderPay_Payable_Test(
            bool overreadIsPayable)
    {
        return VerifySagaEventFlow(new[]
        {
            new SagaStateChange
            {
                SagaAction = (saga, context) => saga.Handle(new EvaluationProcessedEventForPayment
                {
                    IsPerformed = true,
                    IsPayable = false,
                    NeedsOverread = true
                }, context),
                ContextAssertAction = AssertNoSentMessages,
                DataAssertAction = delegate(ProviderPaySagaData data)
                {
                    Assert.False(data.IsPayable);
                    Assert.True(data.NeedsOverread);
                },
                SagaShouldBeComplete = false
            },
            new SagaStateChange
            {
                SagaAction = (saga, context) => saga.Handle(new OverreadProcessedEventForPayment
                {
                    IsPayable = overreadIsPayable
                }, context),
                ContextAssertAction = AssertNoSentMessages, // waiting for CdiEventForPaymentReceived
                DataAssertAction = delegate(ProviderPaySagaData data)
                {
                    Assert.Equal(overreadIsPayable, data.IsPayable);
                    Assert.NotNull(data.OverreadProcessedDateTime);
                },
                SagaShouldBeComplete = false
            },
            new SagaStateChange
            {
                SagaAction = (saga, context) => saga.Handle(new CdiEventForPaymentReceived
                {
                    CreatedDateTime = CdiEventReceivedDateTime
                }, context),
                ContextAssertAction = delegate(TestableMessageHandlerContext context)
                {
                    Assert.Single(context.SentMessages);

                    var message = context.FindSentMessage<ProcessProviderPay>();

                    Assert.NotNull(message);
                    Assert.Equal(overreadIsPayable, message.IsPayable);
                },
                DataAssertAction = delegate(ProviderPaySagaData data) { Assert.Equal(EvaluationId, data.EvaluationId); },
                SagaShouldBeComplete = false
            },
            new SagaStateChange
            {
                SagaAction = (saga, context) => saga.Handle(new ProviderPaidEvent(), context),
                ContextAssertAction = AssertNoSentMessages,
                SagaShouldBeComplete = true
            }
        });
    }

    #region Test Scenarios with everything enabled

    /// <summary>
    /// Finalized(Performed, NeedsOverread)
    /// then
    /// OverreadProcessed => either 
    /// then CdiReceived => ProcessProviderPay
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task EventFlow_Finalized_Performed_NeedsOverread_Test(bool overreadIsPayable)
    {
        var stateChanges = new List<SagaStateChange>
        {
            new SagaStateChange
            {
                SagaAction = (saga, context) => saga.Handle(new EvaluationProcessedEventForPayment
                {
                    IsPerformed = true,
                    NeedsOverread = true,
                    IsPayable = null
                }, context),
                ContextAssertAction = AssertNoSentMessages,
                SagaShouldBeComplete = false
            },
            new SagaStateChange
            {
                SagaAction = (saga, context) => saga.Handle(new OverreadProcessedEventForPayment
                {
                    IsPayable = overreadIsPayable
                }, context),
                ContextAssertAction = AssertNoSentMessages,
                SagaShouldBeComplete = false
            }
        };

        stateChanges.AddRange(new[]
        {
            new SagaStateChange
            {
                SagaAction = (saga, context) => saga.Handle(new CdiEventForPaymentReceived
                {
                    CreatedDateTime = CdiEventReceivedDateTime
                }, context),
                ContextAssertAction = delegate(TestableMessageHandlerContext context)
                {
                    Assert.Single(context.SentMessages);

                    var message = context.FindSentMessage<ProcessProviderPay>();
                    Assert.Equal(EvaluationId, message.EvaluationId);
                    Assert.Equal(overreadIsPayable, message.IsPayable);
                },
                DataAssertAction = data => Assert.NotNull(data.CdiEventReceivedDateTime),
                SagaShouldBeComplete = false
            },
            new SagaStateChange
            {
                SagaAction = (saga, context) => saga.Handle(new ProviderPaidEvent(), context),
                ContextAssertAction = AssertNoSentMessages,
                SagaShouldBeComplete = true
            }
        });

        return VerifySagaEventFlow(stateChanges);
    }

    #endregion Test Scenarios with everything enabled

    #endregion Test Scenarios

    #endregion Verify Saga Event Flow
}