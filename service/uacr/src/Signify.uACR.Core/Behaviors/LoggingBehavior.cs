using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Signify.uACR.Core.Behaviors;

/// <summary>
/// Provides logging for all MediatR request handlers.
/// Most of the time in backend services you want the recoverability of NServiceBus commands instead of
/// Mediatr Requests, but this is left in cases where MediatR is sufficient.
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResponse"></typeparam>
public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger) :
	IPipelineBehavior<TRequest, TResponse>
	where TRequest : IRequest<TResponse>
{
	public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
	{
		logger.LogDebug("Handling {RequestType}", typeof(TRequest).Name);
		var response = await next();
		logger.LogDebug("Handled {ResponseType}", typeof(TResponse).Name);
		return response;
	}
}