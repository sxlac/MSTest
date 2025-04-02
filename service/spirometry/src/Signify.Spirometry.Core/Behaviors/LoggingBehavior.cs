using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Signify.Spirometry.Core.Behaviors
{
	/// <summary>
	/// Provides logging for all MediatR request handlers.
	/// Most of the time in backend services you want the recoverability of NServiceBus commands instead of
	/// Mediatr Requests, but this is left in cases where MediatR is sufficient.
	/// </summary>
	/// <typeparam name="TRequest"></typeparam>
	/// <typeparam name="TResponse"></typeparam>
	public class LoggingBehavior<TRequest, TResponse> :
		IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
	{
		private readonly ILogger _logger;

		public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
		{
			_logger = logger;
		}

		public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
		{
			_logger.LogInformation("Handling {RequestType}, Message: {Request}", typeof(TRequest).Name, request);
			var response = await next();
			_logger.LogInformation("Handled {ResponseType}, Response: {Response}", typeof(TResponse).Name, response);
			return response;
		}
	}
}
