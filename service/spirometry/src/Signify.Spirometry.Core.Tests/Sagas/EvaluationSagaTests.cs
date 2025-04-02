using FakeItEasy;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using NServiceBus;
using Signify.Spirometry.Core.Configs.Loopback;
using Signify.Spirometry.Core.Infrastructure;
using Signify.Spirometry.Core.Sagas.Models;
using SpiroNsb.SagaCommands;
using SpiroNsb.SagaEvents;
using SpiroNsb.Sagas;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Xunit;

// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

namespace Signify.Spirometry.Core.Tests.Sagas
{
    public class EvaluationSagaTests
    {
        private const long EvaluationId = 999; // Just use a number unlikely to conflict with other test data

        private static readonly DateTime FinalizedProcessedDateTime = new(2023, 01, 01);
        private static readonly DateTime HoldCreatedDateTime = new(2023, 01, 02);
        private static readonly DateTime OverreadReceivedDateTime = new(2023, 01, 03);
        private static readonly DateTime OverreadProcessedDateTime = new(2023, 01, 04);
        private static readonly DateTime FlagCreatedDateTime = new(2023, 01, 05);
        private static readonly DateTime HoldReleasedDateTime = new(2023, 01, 06);
        private static readonly DateTime PdfDeliveredToClientDateTime = new(2023, 01, 07);
        private static readonly DateTime PdfDeliveryProcessedDateTime = new(2023, 01, 08);

        private readonly IApplicationTime _applicationTime = new FakeApplicationTime();
        private readonly IGetLoopbackConfig _config = A.Fake<IGetLoopbackConfig>();

        private EvaluationSaga CreateSubject()
        {
            return new EvaluationSaga(A.Dummy<ILogger<EvaluationSaga>>(), _config, _applicationTime)
            {
                Data = new EvaluationSagaData()
            };
        }

        private void EnableOverreadProcessing()
        {
            A.CallTo(() => _config.ShouldProcessOverreads)
                .Returns(true);
        }

        private void EnableEverything()
        {
            EnableOverreadProcessing();
            A.CallTo(() => _config.ShouldCreateFlags)
                .Returns(true);
            A.CallTo(() => _config.ShouldReleaseHolds)
                .Returns(true);
        }

        [Theory]
        [MemberData(nameof(Handle_EvaluationProcessedEvent_SagaDataTestData))]
        public async Task Handle_EvaluationProcessedEvent_SagaDataTests(EvaluationProcessedEvent message)
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
                Assert.Equal(message.NeedsFlag, subject.Data.NeedsFlag);
                Assert.Equal(message.IsBillable, subject.Data.IsBillable);
            }
            else
            {
                Assert.False(subject.Data.NeedsOverread);
                Assert.False(subject.Data.NeedsFlag);
                Assert.False(subject.Data.IsBillable);
            }

            Assert.False(subject.Completed);
        }

        public static IEnumerable<object[]> Handle_EvaluationProcessedEvent_SagaDataTestData()
        {
            yield return
            [
                new EvaluationProcessedEvent
                {
                    SpirometryExamId = 1,
                    IsPerformed = true,
                    NeedsOverread = true,
                    NeedsFlag = false,
                    IsBillable = null,
                    CreatedDateTime = FinalizedProcessedDateTime
                }
            ];

            yield return
            [
                new EvaluationProcessedEvent
                {
                    SpirometryExamId = 1,
                    IsPerformed = true,
                    NeedsOverread = true,
                    NeedsFlag = null,
                    IsBillable = null,
                    CreatedDateTime = FinalizedProcessedDateTime
                }
            ];

            yield return
            [
                new EvaluationProcessedEvent
                {
                    SpirometryExamId = 1,
                    IsPerformed = true,
                    NeedsOverread = false,
                    IsBillable = true,
                    CreatedDateTime = FinalizedProcessedDateTime
                }
            ];

            yield return
            [
                new EvaluationProcessedEvent
                {
                    SpirometryExamId = 1,
                    IsPerformed = false,
                    NeedsOverread = true, // Although `true` here (which is not valid when Not Performed), the test validates this is ignored because `IsPerformed` is `false`
                    IsBillable = true, // Although `true` here (which is not valid when Not Performed), the test validates this is ignored because `IsPerformed` is `false`
                    NeedsFlag = true, // Although `true` here (which is not valid when Not Performed), the test validates this is ignored because `IsPerformed` is `false`
                    CreatedDateTime = FinalizedProcessedDateTime
                }
            ];
        }

        [Theory]
        [MemberData(nameof(Handle_HoldCreatedEvent_SagaDataTestData))]
        public async Task Handle_HoldCreatedEvent_SagaDataTests(HoldCreatedEvent message)
        {
            // Arrange
            var context = new TestableMessageHandlerContext();

            var subject = CreateSubject();

            // Act
            await subject.Handle(message, context);

            // Assert
            Assert.Equal(message.HoldId, subject.Data.HoldId);

            Assert.Equal(HoldCreatedDateTime, subject.Data.HoldCreatedDateTime);

            Assert.False(subject.Completed);
        }

        public static IEnumerable<object[]> Handle_HoldCreatedEvent_SagaDataTestData()
        {
            // No need to supply EvaluationId on the events because that's just used
            // to find the saga; it never gets set directly by handling the event
            yield return
            [
                new HoldCreatedEvent(/*EvaluationId*/ default, HoldCreatedDateTime, /*HoldId*/ 1)
            ];
        }

        [Theory]
        [MemberData(nameof(Handle_OverreadReceivedEvent_SagaDataTestData))]
        public async Task Handle_OverreadReceivedEvent_SagaDataTests(OverreadReceivedEvent message)
        {
            // Arrange
            var context = new TestableMessageHandlerContext();

            var subject = CreateSubject();

            // Act
            await subject.Handle(message, context);

            // Assert
            Assert.Equal(message.OverreadResultId, subject.Data.OverreadResultId);

            Assert.Equal(OverreadReceivedDateTime, subject.Data.OverreadReceivedDateTime);

            Assert.False(subject.Completed);
        }

        public static IEnumerable<object[]> Handle_OverreadReceivedEvent_SagaDataTestData()
        {
            // No need to supply EvaluationId on the events because that's just used
            // to find the saga; it never gets set directly by handling the event
            yield return
            [
                new OverreadReceivedEvent
                {
                    OverreadResultId = 1,
                    CreatedDateTime = OverreadReceivedDateTime
                }
            ];
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task Handle_OverreadProcessedEvent_SagaDataTests(bool isBillable, bool needsFlag)
        {
            // Arrange
            var message = new OverreadProcessedEvent
            {
                IsBillable = isBillable,
                NeedsFlag = needsFlag,
                CreatedDateTime = OverreadProcessedDateTime
            };

            var context = new TestableMessageHandlerContext();

            var subject = CreateSubject();

            // Act
            await subject.Handle(message, context);

            // Assert
            Assert.Equal(OverreadProcessedDateTime, subject.Data.OverreadProcessedDateTime);
            Assert.Equal(message.IsBillable, subject.Data.IsBillable);

            Assert.False(subject.Completed);
        }

        [Theory]
        [MemberData(nameof(Handle_FlagCreatedEvent_SagaDataTestData))]
        public async Task Handle_FlagCreatedEvent_SagaDataTests(FlagCreatedEvent message)
        {
            // Arrange
            var context = new TestableMessageHandlerContext();

            var subject = CreateSubject();

            // Act
            await subject.Handle(message, context);

            // Assert
            Assert.Equal(message.ClarificationFlagId, subject.Data.ClarificationFlagId);

            Assert.Equal(FlagCreatedDateTime, subject.Data.FlagCreatedDateTime);

            Assert.False(subject.Completed);
        }

        public static IEnumerable<object[]> Handle_FlagCreatedEvent_SagaDataTestData()
        {
            // No need to supply EvaluationId on the events because that's just used
            // to find the saga; it never gets set directly by handling the event
            yield return
            [
                new FlagCreatedEvent(/*EvaluationId*/ default, FlagCreatedDateTime, /*ClarificationFlagId*/ 1)
            ];
        }

        [Theory]
        [MemberData(nameof(Handle_HoldReleasedEvent_SagaDataTestData))]
        public async Task Handle_HoldReleasedEvent_SagaDataTests(HoldReleasedEvent message)
        {
            // Arrange
            var context = new TestableMessageHandlerContext();

            var subject = CreateSubject();

            // Act
            await subject.Handle(message, context);

            // Assert
            Assert.Equal(message.HoldId, subject.Data.HoldId);

            Assert.Equal(HoldReleasedDateTime, subject.Data.HoldReleasedDateTime);

            Assert.False(subject.Completed);
        }

        public static IEnumerable<object[]> Handle_HoldReleasedEvent_SagaDataTestData()
        {
            // No need to supply EvaluationId on the events because that's just used
            // to find the saga; it never gets set directly by handling the event
            yield return
            [
                new HoldReleasedEvent(/*EvaluationId*/ default, HoldReleasedDateTime, /*HoldId*/ 1)
            ];
        }

        /// <param name="message"></param>
        /// <param name="sagaDataSetup">Optional setup for the saga data, to be executed before the message is handled</param>
        /// <param name="sagaDataIsValid">Optional function to perform saga data validation</param>
        [Theory]
        [MemberData(nameof(Handle_PdfDeliveredToClientEvent_SagaDataTestData))]
        public async Task Handle_PdfDeliveredToClientEvent_SagaDataTests(PdfDeliveredToClientEvent message,
            Action<EvaluationSagaData> sagaDataSetup = null, Func<EvaluationSagaData, bool> sagaDataIsValid = null)
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
                Assert.Equal(message.PdfDeliveredToClientId, subject.Data.PdfDeliveredToClientId);
                Assert.Equal(message.CreatedDateTime, subject.Data.PdfDeliveredToClientDateTime);

                Assert.False(subject.Completed);
            }
        }

        public static IEnumerable<object[]> Handle_PdfDeliveredToClientEvent_SagaDataTestData()
        {
            // No need to supply EvaluationId on the events because that's just used
            // to find the saga; it never gets set directly by handling the event
            yield return
            [
                new PdfDeliveredToClientEvent
                {
                    PdfDeliveredToClientId = 1,
                    CreatedDateTime = PdfDeliveredToClientDateTime
                }
            ];

            yield return
            [
                // Raise new pdfdelivery event
                new PdfDeliveredToClientEvent
                {
                    PdfDeliveredToClientId = 2,
                    CreatedDateTime = DateTime.UtcNow
                },
                // Setup the saga data as if a pdfdelivery event was already handled
                delegate(EvaluationSagaData sagaData)
                {
                    sagaData.PdfDeliveredToClientId = 1;
                    sagaData.PdfDeliveredToClientDateTime = PdfDeliveredToClientDateTime;
                },
                // Verify the saga data properties are not changed to the new pdfdelivery event
                delegate(EvaluationSagaData sagaData)
                {
                    return sagaData.PdfDeliveredToClientId == 1 &&
                           sagaData.PdfDeliveredToClientDateTime == PdfDeliveredToClientDateTime;
                }
            ];
        }

        [Theory]
        [MemberData(nameof(Handle_PdfDeliveryProcessedEvent_SagaDataTestData))]
        public async Task Handle_PdfDeliveryProcessedEvent_SagaDataTests(PdfDeliveryProcessedEvent message)
        {
            // Arrange
            var context = new TestableMessageHandlerContext();

            var subject = CreateSubject();

            // Act
            await subject.Handle(message, context);

            // Assert
            Assert.Equal(message.CreatedDateTime, subject.Data.PdfDeliveryProcessedDateTime);

            Assert.True(subject.Completed);
        }

        public static IEnumerable<object[]> Handle_PdfDeliveryProcessedEvent_SagaDataTestData()
        {
            // No need to supply EvaluationId on the events because that's just used
            // to find the saga; it never gets set directly by handling the event
            yield return new object[]
            {
                new PdfDeliveryProcessedEvent
                {
                    CreatedDateTime = PdfDeliveryProcessedDateTime
                }
            };
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
            public Func<EvaluationSaga, IMessageHandlerContext, Task> SagaAction { get; init; }

            /// <summary>
            /// Action containing your assertions on the saga data state, which gets
            /// called after <see cref="SagaAction"/>
            /// </summary>
            /// <remarks>Optional</remarks>
            public Action<EvaluationSagaData> DataAssertAction { get; init; }

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
        /// 1) Runs optional assertions on the underlying <see cref="EvaluationSagaData"/>.
        /// 2) Runs assertions on the <see cref="IMessageHandlerContext"/>.
        /// 3) Asserts that the `IsComplete` flag is properly set, according to the input test data.
        ///
        /// This is to cover test cases of receiving events out-of-order, and verifying what
        /// NSB commands are raised after each message is handled.
        /// </summary>
        private async Task VerifySagaEventFlow(IEnumerable<SagaStateChange> changes)
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
        /// Finalized(NotPerformed) followed by PdfDelivery => ProcessPdfDelivery(!IsBillable)
        /// </summary>
        [Fact]
        public Task EventFlow_Finalized_NotPerformed_PdfDelivery_ProcessesPdfDelivery_NotBillable_Test()
        {
            return VerifySagaEventFlow(new[]
            {
                new SagaStateChange
                {
                    SagaAction = (saga, context) => saga.Handle(new EvaluationProcessedEvent
                    {
                        IsPerformed = false
                    }, context),
                    ContextAssertAction = AssertNoSentMessages,
                    SagaShouldBeComplete = false
                },
                new SagaStateChange
                {
                    SagaAction = (saga, context) => saga.Handle(new PdfDeliveredToClientEvent(), context),
                    ContextAssertAction = delegate(TestableMessageHandlerContext context)
                    {
                        Assert.Single(context.SentMessages);

                        var message = context.FindSentMessage<ProcessPdfDelivery>();

                        Assert.NotNull(message);
                        Assert.Equal(EvaluationId, message.EvaluationId);
                        Assert.False(message.IsBillable);
                    },
                    SagaShouldBeComplete = false
                },
                new SagaStateChange
                {
                    SagaAction = (saga, context) => saga.Handle(new PdfDeliveryProcessedEvent(), context),
                    ContextAssertAction = AssertNoSentMessages,
                    SagaShouldBeComplete = true
                }
            });
        }

        /// <summary>
        /// Finalized(Performed, POC results Billable, !NeedsOverread) followed by PdfDelivery => ProcessPdfDelivery(Billable)
        /// </summary>
        [Fact]
        public Task EventFlow_Finalized_Performed_PocBillable_PdfDelivery_ProcessesPdfDelivery_Billable_Test()
        {
            const int pdfEntityId = 1;

            EnableOverreadProcessing();

            return VerifySagaEventFlow(new[]
            {
                new SagaStateChange
                {
                    SagaAction = (saga, context) => saga.Handle(new EvaluationProcessedEvent
                    {
                        IsPerformed = true,
                        IsBillable = true,
                        NeedsOverread = false
                    }, context),
                    ContextAssertAction = AssertNoSentMessages,
                    SagaShouldBeComplete = false
                },
                new SagaStateChange
                {
                    SagaAction = (saga, context) => saga.Handle(new PdfDeliveredToClientEvent
                    {
                        PdfDeliveredToClientId = pdfEntityId
                    }, context),
                    ContextAssertAction = delegate(TestableMessageHandlerContext context)
                    {
                        Assert.Single(context.SentMessages);

                        var message = context.FindSentMessage<ProcessPdfDelivery>();

                        Assert.NotNull(message);
                        Assert.Equal(EvaluationId, message.EvaluationId);
                        Assert.Equal(pdfEntityId, message.PdfDeliveredToClientId);
                        Assert.True(message.IsBillable);
                    },
                    SagaShouldBeComplete = false
                },
                new SagaStateChange
                {
                    SagaAction = (saga, context) => saga.Handle(new PdfDeliveryProcessedEvent(), context),
                    DataAssertAction = delegate(EvaluationSagaData data)
                    {
                        Assert.NotNull(data.PdfDeliveryProcessedDateTime);
                    },
                    ContextAssertAction = AssertNoSentMessages,
                    SagaShouldBeComplete = true
                }
            });
        }

        /// <summary>
        /// PdfDelivery followed by Finalized(NotPerformed) => ProcessPdfDelivery(!IsBillable)
        /// </summary>
        [Fact]
        public Task EventFlow_PdfDelivery_Finalized_NotPerformed_ProcessesPdfDelivery_NotBillable_Test()
        {
            return VerifySagaEventFlow(new []
            {
                new SagaStateChange
                {
                    SagaAction = (saga, context) => saga.Handle(new PdfDeliveredToClientEvent(), context),
                    ContextAssertAction = AssertNoSentMessages,
                    SagaShouldBeComplete = false
                },
                new SagaStateChange
                {
                    SagaAction = (saga, context) => saga.Handle(new EvaluationProcessedEvent
                    {
                        IsPerformed = false
                    }, context),
                    ContextAssertAction = delegate(TestableMessageHandlerContext context)
                    {
                        Assert.Single(context.SentMessages);

                        var message = context.FindSentMessage<ProcessPdfDelivery>();

                        Assert.NotNull(message);
                        Assert.Equal(EvaluationId, message.EvaluationId);
                        Assert.False(message.IsBillable);
                    },
                    SagaShouldBeComplete = false
                },
                new SagaStateChange
                {
                    SagaAction = (saga, context) => saga.Handle(new PdfDeliveryProcessedEvent(), context),
                    ContextAssertAction = AssertNoSentMessages,
                    SagaShouldBeComplete = true
                }
            });
        }

        /// <summary>
        /// PdfDelivery followed by Finalized(Performed, IsBillable not known due to needing overread)
        /// </summary>
        [Fact]
        public Task EventFlow_PdfDelivery_Finalized_Performed_BillableNotKnown_Test()
        {
            return VerifySagaEventFlow(new[]
            {
                new SagaStateChange
                {
                    SagaAction = (saga, context) => saga.Handle(new PdfDeliveredToClientEvent(), context),
                    ContextAssertAction = AssertNoSentMessages,
                    SagaShouldBeComplete = false
                },
                new SagaStateChange
                {
                    SagaAction = (saga, context) => saga.Handle(new EvaluationProcessedEvent
                    {
                        IsPerformed = true,
                        NeedsOverread = true,
                        IsBillable = null
                    }, context),
                    DataAssertAction = delegate(EvaluationSagaData data)
                    {
                        Assert.True(data.IsPerformed);
                        Assert.True(data.NeedsOverread);
                        Assert.Null(data.IsBillable);
                    },
                    ContextAssertAction = AssertNoSentMessages, // Nothing to do because awaiting overread
                    SagaShouldBeComplete = false // Awaiting overread to determine if billable, so pdfdelivery can be processed
                }
            });
        }

        /// <summary>
        /// PdfDelivery followed by Finalized(Performed, Not Billable (no overread required because disabled)) => ProcessPdfDelivery(NotBillable)
        /// </summary>
        [Fact]
        public Task EventFlow_PdfDelivery_Finalized_Performed_NotBillable_ProcessesPdfDelivery_NotBillable_Test()
        {
            return VerifySagaEventFlow(new[]
            {
                new SagaStateChange
                {
                    SagaAction = (saga, context) => saga.Handle(new PdfDeliveredToClientEvent
                    {
                        PdfDeliveredToClientId = 1
                    }, context),
                    ContextAssertAction = AssertNoSentMessages,
                    SagaShouldBeComplete = false
                },
                new SagaStateChange
                {
                    SagaAction = (saga, context) => saga.Handle(new EvaluationProcessedEvent
                    {
                        IsPerformed = true,
                        IsBillable = false,
                        NeedsOverread = false // ProcessOverreads config was `false` when Finalized
                    }, context),
                    DataAssertAction = delegate(EvaluationSagaData data)
                    {
                        Assert.True(data.IsPerformed);
                        Assert.False(data.IsBillable);
                        Assert.False(data.NeedsOverread);
                    },
                    ContextAssertAction = delegate(TestableMessageHandlerContext context)
                    {
                        Assert.Single(context.SentMessages);

                        var message = context.FindSentMessage<ProcessPdfDelivery>();

                        Assert.NotNull(message);
                        Assert.Equal(EvaluationId, message.EvaluationId);
                        Assert.Equal(1, message.PdfDeliveredToClientId);
                        Assert.False(message.IsBillable);
                    },
                    SagaShouldBeComplete = false
                },
                new SagaStateChange
                {
                    SagaAction = (saga, context) => saga.Handle(new PdfDeliveryProcessedEvent(), context),
                    ContextAssertAction = AssertNoSentMessages,
                    SagaShouldBeComplete = true
                }
            });
        }

        /// <summary>
        /// PdfDelivery followed by Finalized(Performed, Billable, !NeedsOverread) => ProcessPdfDelivery(Billable)
        /// </summary>
        [Fact]
        public Task EventFlow_PdfDelivery_Finalized_Performed_Billable_OverreadNotNeeded_ProcessesPdfDelivery_Billable_Test()
        {
            const int pdfEntityId = 1;

            return VerifySagaEventFlow(new[]
            {
                new SagaStateChange
                {
                    SagaAction = (saga, context) => saga.Handle(new PdfDeliveredToClientEvent
                    {
                        PdfDeliveredToClientId = pdfEntityId
                    }, context),
                    ContextAssertAction = AssertNoSentMessages,
                    DataAssertAction = delegate(EvaluationSagaData data)
                    {
                        Assert.Equal(pdfEntityId, data.PdfDeliveredToClientId);
                    },
                    SagaShouldBeComplete = false
                },
                new SagaStateChange
                {
                    SagaAction = (saga, context) => saga.Handle(new EvaluationProcessedEvent
                    {
                        IsPerformed = true,
                        IsBillable = true,
                        NeedsOverread = false
                    }, context),
                    ContextAssertAction = delegate(TestableMessageHandlerContext context)
                    {
                        Assert.Single(context.SentMessages);

                        var message = context.FindSentMessage<ProcessPdfDelivery>();

                        Assert.NotNull(message);
                        Assert.Equal(EvaluationId, message.EvaluationId);
                        Assert.Equal(pdfEntityId, message.PdfDeliveredToClientId);
                        Assert.True(message.IsBillable);
                    },
                    SagaShouldBeComplete = false
                },
                new SagaStateChange
                {
                    SagaAction = (saga, context) => saga.Handle(new PdfDeliveryProcessedEvent(), context),
                    ContextAssertAction = AssertNoSentMessages,
                    SagaShouldBeComplete = true
                }
            });
        }

        /// <summary>
        /// Finalized(Performed, !Billable, NeedsOverread) followed by OverreadReceived => ProcessOverread
        /// => OverreadProcessed
        /// then
        /// PdfDelivery => ProcessPdfDelivery
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public Task EventFlow_Finalized_Performed_NotBillable_NeedsOverread_OverreadReceived_ProcessesOverread_OverreadProcessed_Billable_PdfDelivery_ProcessesPdfDelivery_Billable_Test(bool overreadIsBillable)
        {
            const int pdfEntityId = 1;
            const int overreadResultId = 2;

            var overreadReceivedDateTime = DateTime.UtcNow;

            EnableOverreadProcessing();

            return VerifySagaEventFlow(new[]
            {
                new SagaStateChange
                {
                    SagaAction = (saga, context) => saga.Handle(new EvaluationProcessedEvent
                    {
                        IsPerformed = true,
                        IsBillable = false,
                        NeedsOverread = true
                    }, context),
                    ContextAssertAction = AssertNoSentMessages,
                    DataAssertAction = delegate(EvaluationSagaData data)
                    {
                        Assert.False(data.IsBillable);
                        Assert.True(data.NeedsOverread);
                    },
                    SagaShouldBeComplete = false
                },
                new SagaStateChange
                {
                    SagaAction = (saga, context) => saga.Handle(new OverreadReceivedEvent
                    {
                        CreatedDateTime = overreadReceivedDateTime,
                        OverreadResultId = overreadResultId
                    }, context),
                    ContextAssertAction = delegate(TestableMessageHandlerContext context)
                    {
                        Assert.Single(context.SentMessages);

                        var message = context.FindSentMessage<ProcessOverread>();

                        Assert.NotNull(message);
                        Assert.Equal(EvaluationId, message.EvaluationId);
                    },
                    DataAssertAction = delegate(EvaluationSagaData data)
                    {
                        Assert.Equal(overreadReceivedDateTime, data.OverreadReceivedDateTime);
                        Assert.NotNull(data.ProcessOverreadCommandSentDateTime);
                    },
                    SagaShouldBeComplete = false
                },
                new SagaStateChange
                {
                    SagaAction = (saga, context) => saga.Handle(new OverreadProcessedEvent
                    {
                        IsBillable = overreadIsBillable
                    }, context),
                    ContextAssertAction = AssertNoSentMessages, // waiting for pdfdelivery
                    DataAssertAction = delegate(EvaluationSagaData data)
                    {
                        Assert.Equal(overreadIsBillable, data.IsBillable);
                        Assert.NotNull(data.OverreadProcessedDateTime);
                    },
                    SagaShouldBeComplete = false
                },
                new SagaStateChange
                {
                    SagaAction = (saga, context) => saga.Handle(new PdfDeliveredToClientEvent
                    {
                        PdfDeliveredToClientId = pdfEntityId
                    }, context),
                    ContextAssertAction = delegate(TestableMessageHandlerContext context)
                    {
                        Assert.Single(context.SentMessages);

                        var message = context.FindSentMessage<ProcessPdfDelivery>();

                        Assert.NotNull(message);
                        Assert.Equal(overreadIsBillable, message.IsBillable);
                    },
                    DataAssertAction = delegate(EvaluationSagaData data)
                    {
                        Assert.Equal(pdfEntityId, data.PdfDeliveredToClientId);
                        Assert.NotNull(data.ProcessPdfDeliveryCommandSentDateTime);
                    },
                    SagaShouldBeComplete = false
                },
                new SagaStateChange
                {
                    SagaAction = (saga, context) => saga.Handle(new PdfDeliveryProcessedEvent(), context),
                    ContextAssertAction = AssertNoSentMessages,
                    SagaShouldBeComplete = true
                }
            });
        }

        /// <summary>
        /// OverreadReceived followed by Finalized(Performed, !Billable, NeedsOverread) => ProcessOverread
        /// => OverreadProcessed
        /// then
        /// PdfDelivery => ProcessPdfDelivery
        /// </summary>
        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public Task EventFlow_OverreadReceived_Finalized_Performed_NotBillable_NeedsOverread_ProcessesOverread_OverreadProcessed_Billable_PdfDelivery_ProcessesPdfDelivery_Billable_Test(bool overreadIsBillable, bool needsFlag)
        {
            const int pdfEntityId = 1;
            const int overreadResultId = 2;

            var overreadReceivedDateTime = DateTime.UtcNow;

            EnableOverreadProcessing();

            return VerifySagaEventFlow(new[]
            {
                new SagaStateChange
                {
                    SagaAction = (saga, context) => saga.Handle(new OverreadReceivedEvent
                    {
                        CreatedDateTime = overreadReceivedDateTime,
                        OverreadResultId = overreadResultId
                    }, context),
                    ContextAssertAction = AssertNoSentMessages
                },
                new SagaStateChange
                {
                    SagaAction = (saga, context) => saga.Handle(new EvaluationProcessedEvent
                    {
                        IsPerformed = true,
                        IsBillable = false,
                        NeedsOverread = true
                    }, context),
                    ContextAssertAction = delegate(TestableMessageHandlerContext context)
                    {
                        Assert.Single(context.SentMessages);

                        var message = context.FindSentMessage<ProcessOverread>();

                        Assert.NotNull(message);
                        Assert.Equal(EvaluationId, message.EvaluationId);
                    },
                    DataAssertAction = delegate(EvaluationSagaData data)
                    {
                        Assert.False(data.IsBillable);
                        Assert.True(data.NeedsOverread);

                        Assert.Equal(overreadReceivedDateTime, data.OverreadReceivedDateTime);
                        Assert.NotNull(data.ProcessOverreadCommandSentDateTime);
                    },
                    SagaShouldBeComplete = false
                },
                new SagaStateChange
                {
                    SagaAction = (saga, context) => saga.Handle(new OverreadProcessedEvent
                    {
                        IsBillable = overreadIsBillable,
                        NeedsFlag = needsFlag
                    }, context),
                    ContextAssertAction = AssertNoSentMessages, // waiting for pdfdelivery
                    DataAssertAction = delegate(EvaluationSagaData data)
                    {
                        Assert.Equal(overreadIsBillable, data.IsBillable);
                        Assert.Equal(needsFlag, data.NeedsFlag);
                        Assert.NotNull(data.OverreadProcessedDateTime);
                    },
                    SagaShouldBeComplete = false
                },
                new SagaStateChange
                {
                    SagaAction = (saga, context) => saga.Handle(new PdfDeliveredToClientEvent
                    {
                        PdfDeliveredToClientId = pdfEntityId
                    }, context),
                    ContextAssertAction = delegate(TestableMessageHandlerContext context)
                    {
                        Assert.Single(context.SentMessages);

                        var message = context.FindSentMessage<ProcessPdfDelivery>();

                        Assert.NotNull(message);
                        Assert.Equal(overreadIsBillable, message.IsBillable);
                    },
                    DataAssertAction = delegate(EvaluationSagaData data)
                    {
                        Assert.Equal(pdfEntityId, data.PdfDeliveredToClientId);
                        Assert.NotNull(data.ProcessPdfDeliveryCommandSentDateTime);
                    },
                    SagaShouldBeComplete = false
                },
                new SagaStateChange
                {
                    SagaAction = (saga, context) => saga.Handle(new PdfDeliveryProcessedEvent(), context),
                    ContextAssertAction = AssertNoSentMessages,
                    SagaShouldBeComplete = true
                }
            });
        }

        #region Test Scenarios with everything enabled
        /// <summary>
        /// Finalized(Performed, NeedsOverread)
        /// then
        /// HoldReceived
        /// then
        /// OverreadReceived => ProcessOverread
        /// then
        /// OverreadProcessed => either immediately ReleaseHold, -or- CreateFlag then FlagCreatedEvent => ReleaseHold
        /// then PdfDelivered => ProcessPdfDelivery
        /// </summary>
        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public Task EventFlow_Finalized_Performed_NeedsOverread_HoldReceived_OverreadReceived_ReleasesHold_Test(bool overreadIsBillable, bool needsFlag)
        {
            const int holdId = 1;
            const int overreadResultId = 2;
            const int pdfDeliveredToClientId = 3;

            EnableEverything();

            var stateChanges = new List<SagaStateChange>
            {
                new SagaStateChange
                {
                    SagaAction = (saga, context) => saga.Handle(new EvaluationProcessedEvent
                    {
                        IsPerformed = true,
                        NeedsOverread = true,
                        IsBillable = null
                    }, context),
                    ContextAssertAction = AssertNoSentMessages,
                    SagaShouldBeComplete = false
                },
                new SagaStateChange
                {
                    SagaAction = (saga, context) => saga.Handle(new HoldCreatedEvent(default, default, holdId), context),
                    ContextAssertAction = AssertNoSentMessages,
                    SagaShouldBeComplete = false
                },
                new SagaStateChange
                {
                    SagaAction = (saga, context) => saga.Handle(new OverreadReceivedEvent
                    {
                        OverreadResultId = overreadResultId
                    }, context),
                    ContextAssertAction = delegate(TestableMessageHandlerContext context)
                    {
                        Assert.Single(context.SentMessages);

                        var message = context.FindSentMessage<ProcessOverread>();
                        Assert.Equal(EvaluationId, message.EvaluationId);
                        Assert.Equal(overreadResultId, message.OverreadResultId);
                    },
                    DataAssertAction = data => Assert.NotNull(data.ProcessOverreadCommandSentDateTime),
                    SagaShouldBeComplete = false
                },
                new SagaStateChange
                {
                    SagaAction = (saga, context) => saga.Handle(new OverreadProcessedEvent
                    {
                        IsBillable = overreadIsBillable,
                        NeedsFlag = needsFlag
                    }, context),
                    ContextAssertAction = delegate(TestableMessageHandlerContext context)
                    {
                        Assert.Single(context.SentMessages);

                        if (needsFlag)
                        {
                            var createFlag = context.FindSentMessage<CreateFlag>();
                            Assert.Equal(EvaluationId, createFlag.EvaluationId);
                        }
                        else
                        {
                            var releaseHold = context.FindSentMessage<ReleaseHold>();
                            Assert.Equal(EvaluationId, releaseHold.EvaluationId);
                            Assert.Equal(holdId, releaseHold.HoldId);
                        }
                    },
                    DataAssertAction = delegate(EvaluationSagaData data)
                    {
                        if (needsFlag)
                        {
                            Assert.NotNull(data.CreateFlagCommandSentDateTime);
                            Assert.Null(data.ReleaseHoldCommandSentDateTime);
                        }
                        else
                        {
                            Assert.Null(data.CreateFlagCommandSentDateTime);
                            Assert.NotNull(data.ReleaseHoldCommandSentDateTime);
                        }
                    },
                    SagaShouldBeComplete = false
                }
            };

            // If a flag is needed, add a FlagCreatedEvent next
            if (needsFlag)
            {
                stateChanges.Add(new SagaStateChange
                {
                    SagaAction = (saga, context) => saga.Handle(new FlagCreatedEvent(default, default, default), context),
                    ContextAssertAction = delegate(TestableMessageHandlerContext context)
                    {
                        Assert.Single(context.SentMessages);

                        var message = context.FindSentMessage<ReleaseHold>();
                        Assert.Equal(EvaluationId, message.EvaluationId);
                        Assert.Equal(holdId, message.HoldId);
                    },
                    DataAssertAction = data => Assert.NotNull(data.ReleaseHoldCommandSentDateTime),
                    SagaShouldBeComplete = false
                });
            }

            stateChanges.AddRange(new []
            {
                new SagaStateChange
                {
                    SagaAction = (saga, context) => saga.Handle(new HoldReleasedEvent(default, default, default), context),
                    ContextAssertAction = AssertNoSentMessages,
                    SagaShouldBeComplete = false
                },
                new SagaStateChange
                {
                    SagaAction = (saga, context) => saga.Handle(new PdfDeliveredToClientEvent
                    {
                        PdfDeliveredToClientId = pdfDeliveredToClientId
                    }, context),
                    ContextAssertAction = delegate(TestableMessageHandlerContext context)
                    {
                        Assert.Single(context.SentMessages);

                        var message = context.FindSentMessage<ProcessPdfDelivery>();
                        Assert.Equal(EvaluationId, message.EvaluationId);
                        Assert.Equal(overreadIsBillable, message.IsBillable);
                        Assert.Equal(pdfDeliveredToClientId, message.PdfDeliveredToClientId);
                    },
                    DataAssertAction = data => Assert.NotNull(data.ProcessPdfDeliveryCommandSentDateTime),
                    SagaShouldBeComplete = false
                },
                new SagaStateChange
                {
                    SagaAction = (saga, context) => saga.Handle(new PdfDeliveryProcessedEvent(), context),
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
}
