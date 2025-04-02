using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Signify.eGFR.Core.Behaviors;

/// <summary>
/// Provides logging for all MediatR request handlers.
/// Most of the time in backend services you want the recoverability of NServiceBus commands instead of Mediatr Requests, but this is left in cases
/// Where MediatR is sufficient.
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResponse"></typeparam>
public class LoggingBehavior<TRequest, TResponse>(ILogger<TRequest> logger) :
	IPipelineBehavior<TRequest, TResponse>
	where TRequest : IRequest<TResponse>
{
	private readonly ILogger _logger = logger;

	public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
	{
		_logger.LogDebug("Handling {RequestType}, Message: {Request}", typeof(TRequest).Name, request);
		var response = await next();
		_logger.LogDebug("Handled {ResponseType}, Response: {Response}", typeof(TResponse).Name, response);
		return response;
	}
}