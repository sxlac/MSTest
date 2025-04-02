using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.A1C.Messages.Events;

namespace Signify.A1C.Svc.Core.EventHandlers
{
	/*********************** Phase 2 implementation ***********************/

	/// <summary>
	///This handles A1CLabResultsReceived event.
	/// </summary>
	public class A1CLabResultsReceivedHandler : IHandleMessages<A1CLabResultsReceived>
	{
        private readonly ILogger<A1CLabResultsReceivedHandler> _logger;


        public A1CLabResultsReceivedHandler(ILogger<A1CLabResultsReceivedHandler> logger)
		{
			_logger = logger;
        }

        [Transaction]
		//look for details in https://app.lucidchart.com/documents/edit/e3a5aacc-d0aa-49f4-9fda-dac79a6ae5f8/PGqqEGkfbdPo#?folder_id=home&browser=icon
		public Task Handle(A1CLabResultsReceived message, IMessageHandlerContext context)
		{
			_logger.LogDebug($"Start Handle BarCodeUpdatedEvent, AppointmentId: {message.AppointmentId}");



			_logger.LogDebug($"End Handle BarcodeUpdatedEvent, AppointmentId: {message.ApplicationId}");

			return Task.CompletedTask;
		}
	}
}