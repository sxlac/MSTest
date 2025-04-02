using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.A1C.Messages.Events;
using Signify.A1C.Svc.Core.ApiClient;
using Signify.A1C.Svc.Core.Commands;
using Signify.A1C.Svc.Core.Data;
using System.Threading.Tasks;

namespace Signify.A1C.Svc.Core.EventHandlers
{
    /// <summary>
    /// This handles DOS update of existing A1C and saves to database
    /// </summary>
    public class DateOfServiceUpdateHandler : IHandleMessages<DateOfServiceUpdated>
    {
        private readonly ILogger<DateOfServiceUpdateHandler> _logger;
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        private readonly A1CDataContext _dataContext;

        public DateOfServiceUpdateHandler(ILogger<DateOfServiceUpdateHandler> logger, IMediator mediator, A1CDataContext dataContext, IMapper mapper, IProviderApi providerApi)
        {
            _logger = logger;
            _mediator = mediator;
            _mapper = mapper;
            _dataContext = dataContext;
        }

        [Transaction]
        public async Task Handle(DateOfServiceUpdated message, IMessageHandlerContext context)
        {
            //Guaranteed that this evaluationId exists in DB, otherwise NSB would not have published
            var a1c = await _dataContext.A1C.AsNoTracking().FirstOrDefaultAsync(s => s.EvaluationId == message.EvaluationId);
            var oldDos = a1c.DateOfService;
            a1c.DateOfService = message.DateOfService;
            var updatea1C = _mapper.Map<CreateOrUpdateA1C>(a1c);
            await _mediator.Send(updatea1C);
            _logger.LogInformation(
                $"DOS updated for existing Evaluation, EvaluationID : {message.EvaluationId}, previous DOS: {oldDos}");
        }
    }
}
