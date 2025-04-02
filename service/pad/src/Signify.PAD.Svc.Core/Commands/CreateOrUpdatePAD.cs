using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.PAD.Svc.Core.Data;

namespace Signify.PAD.Svc.Core.Commands;

[ExcludeFromCodeCoverage]
public class CreateOrUpdatePAD : IRequest<Data.Entities.PAD>
{
	public int PADId { get; set; }
	public int EvaluationId { get; set; }
	public int MemberId { get; set; }
	public int MemberPlanId { get; set; }
	public string CenseoId { get; set; }
	public int AppointmentId { get; set; }
	public int ProviderId { get; set; }
	public DateTime? DateOfService { get; set; }
	public DateTimeOffset CreatedDateTime { get; set; }
	public DateTimeOffset ReceivedDateTime { get; set; }
	public int ClientId { get; set; }
	public string UserName { get; set; }
	public string ApplicationId { get; set; }
	public string FirstName { get; set; }
	public string MiddleName { get; set; }
	public string LastName { get; set; }
	public DateTime DateOfBirth { get; set; }
	public string AddressLineOne { get; set; }
	public string AddressLineTwo { get; set; }
	public string City { get; set; }
	public string State { get; set; }
	public string ZipCode { get; set; }
	public string NationalProviderIdentifier { get; set; }
	public string LeftScoreAnswerValue { get; set; }
	public string LeftSeverityAnswerValue { get; set; }
	public string RightScoreAnswerValue { get; set; }
	public string RightSeverityAnswerValue { get; set; }
	public string LeftNormalityIndicator { get; set; }
	public string RightNormalityIndicator { get; set; }
}

public class CreateOrUpdatePADHandler : IRequestHandler<CreateOrUpdatePAD, Data.Entities.PAD>
{
	private readonly PADDataContext _context;
	private readonly IMapper _mapper;
	private readonly ILogger<CreateOrUpdatePADHandler> _logger;

	public CreateOrUpdatePADHandler(PADDataContext context, IMapper mapper, ILogger<CreateOrUpdatePADHandler> logger)
	{
		_context = context;
		_mapper = mapper;
		_logger = logger;
	}

	[Trace]
	public async Task<Data.Entities.PAD> Handle(CreateOrUpdatePAD request, CancellationToken cancellationToken)
	{
		var PAD = _mapper.Map<Data.Entities.PAD>(request);

		if (request.PADId == 0)
		{
			//Create PAD row
			var newPAD = await _context.PAD.AddAsync(PAD, cancellationToken);
			await _context.SaveChangesAsync(cancellationToken);
			return newPAD.Entity;
		}
		//update PAD row
		else
		{
			_logger.LogDebug($"Updating PAD PADID:{PAD.PADId}");
			var updatePAD = _context.PAD.Update(PAD);
			//update PAD status
			await _context.SaveChangesAsync(cancellationToken);
			return updatePAD.Entity;
		}
	}
}