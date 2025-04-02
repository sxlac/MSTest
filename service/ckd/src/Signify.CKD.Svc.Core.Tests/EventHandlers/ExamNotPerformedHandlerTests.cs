using FakeItEasy;
using MediatR;
using Signify.CKD.Svc.Core.Commands;
using Signify.CKD.Svc.Core.EventHandlers;
using Signify.CKD.Svc.Core.Events;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Signify.CKD.Svc.Core.Infrastructure.Observability;
using Xunit;

namespace Signify.CKD.Svc.Core.Tests.EventHandlers
{
    public class ExamNotPerformedHandlerTests
    {
        private readonly IMediator _mediator = A.Fake<IMediator>();
        private readonly IObservabilityService _observabilityService = A.Fake<IObservabilityService>();
        private readonly ILogger<ExamNotPerformedHandler> _logger = A.Fake<ILogger<ExamNotPerformedHandler>>();
        private ExamNotPerformedHandler CreateSubject()
            => new ExamNotPerformedHandler(_logger, _mediator, _observabilityService);

        [Fact]
        public async Task Handle_AddsExamNotPerformed()
        {
            var request = new ExamNotPerformedEvent
            {
                Exam = new Core.Data.Entities.CKD(),
                NotPerformedReasonId = 1,
                NotPerformedReasonNotes = "notes"
            };

            var subject = CreateSubject();

            await subject.Handle(request, default);

            A.CallTo(() => _mediator.Send(A<AddExamNotPerformed>.That.Matches(a =>
                        a.Exam == request.Exam && a.NotPerformedReasonId == 1 && a.Notes == "notes"),
                    A<CancellationToken>._))
                .MustHaveHappened();
        }
    }
}
