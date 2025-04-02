using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Signify.A1C.Svc.Core.Behaviors
{
	/// <summary>
	/// Provides logging for all MediatR request handlers.
	/// Most of the time in backend services you want the recoverability of NServiceBus commands instead of Mediatr Requests, but this is left in cases
	/// Where MediatR is sufficient.
	/// </summary>
	/// <typeparam name="TRequest"></typeparam>
	/// <typeparam name="TResponse"></typeparam>
	public class LoggingBehavior<TRequest, TResponse> :
		IPipelineBehavior<TRequest, TResponse>
	{
		private readonly ILogger _logger;

		public LoggingBehavior(ILogger<TRequest> logger)
		{
			_logger = logger;
		}

		public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
		{
            _logger.LogInformation($"Handling {typeof(TRequest).Name},  Message: {request?.ToString()}");
			var response = await next();
			_logger.LogInformation($"Handled {typeof(TResponse).Name}, Response: {response?.ToString()}");
			return response;
		}
	}
}