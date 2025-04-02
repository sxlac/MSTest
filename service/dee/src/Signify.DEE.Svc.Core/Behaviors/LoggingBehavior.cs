using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Signify.DEE.Svc.Core.Behaviors;

/// <summary>
/// Provides logging for all MediatR request handlers.
/// Most of the time in backend services you want the recoverability of NServiceBus commands instead of Mediatr Requests, but this is left in cases
/// Where MediatR is sufficient.
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResponse"></typeparam>
public class LoggingBehavior<TRequest, TResponse>(ILogger<TRequest> logger) :
	IPipelineBehavior<TRequest, TResponse>
{
	public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
	{
		logger.LogInformation("Handling {Name}", typeof(TRequest).Name);
		var response = await next();
		logger.LogInformation("Handled {Name}", typeof(TResponse).Name);
		return response;
	}
}