using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.HBA1CPOC.Svc.Core.Data;
using System;
using System.Threading;
using System.Threading.Tasks;
using NewRelic.Api.Agent;

namespace Signify.HBA1CPOC.Svc.Core.Commands;

public class CreateOrUpdateHBA1CPOC : IRequest<Data.Entities.HBA1CPOC>
{
	public int HBA1CPOCId { get; set; }
	public int EvaluationId { get; set; }
	public int MemberId { get; set; }
	public int MemberPlanId { get; set; }
	public string CenseoId { get; set; }
	public int AppointmentId { get; set; }
	public int ProviderId { get; set; }
	public DateTime? DateOfService { get; set; }
	public DateTimeOffset CreatedDateTime { get; set; }
	public DateTime ReceivedDateTime { get; set; }
	public int ClientId { get; set; }
	public string UserName { get; set; }
	public string ApplicationId { get; set; }
	public string FirstName { get; set; }
	public string MiddleName { get; set; }
	public string LastName { get; set; }
	public DateOnly? DateOfBirth { get; set; }
	public string AddressLineOne { get; set; }
	public string AddressLineTwo { get; set; }
	public string City { get; set; }
	public string State { get; set; }
	public string ZipCode { get; set; }
	public string NationalProviderIdentifier { get; set; }
	public string A1CPercent { get; set; }
	public string NormalityIndicator { get; set; }
	public DateOnly? ExpirationDate { get; set; }
}

public class CreateOrUpdateHBA1CPOCHandler : IRequestHandler<CreateOrUpdateHBA1CPOC, Data.Entities.HBA1CPOC>
{
	private readonly Hba1CpocDataContext _context;
	private readonly IMapper _mapper;
	private readonly ILogger<CreateOrUpdateHBA1CPOCHandler> _logger;

	public CreateOrUpdateHBA1CPOCHandler(Hba1CpocDataContext context, IMapper mapper, ILogger<CreateOrUpdateHBA1CPOCHandler> logger)
	{
		_context = context;
		_mapper = mapper;
		_logger = logger;
	}

	[Trace]
	public async Task<Data.Entities.HBA1CPOC> Handle(CreateOrUpdateHBA1CPOC request, CancellationToken cancellationToken)
	{
		var exam = _mapper.Map<Data.Entities.HBA1CPOC>(request);

		if (request.HBA1CPOCId == 0)
		{
			//Create HBA1CPOC row
			var newExam = await _context.HBA1CPOC.AddAsync(exam, cancellationToken);
			await _context.SaveChangesAsync(cancellationToken);
			return newExam.Entity;
		}

		_logger.LogDebug("Updating HBA1CPOC record with HBA1CPOCID:{HBA1CPOCId}", exam.HBA1CPOCId);
		var updateExam = _context.HBA1CPOC.Update(exam);
		await _context.SaveChangesAsync(cancellationToken);
		return updateExam.Entity;
	}
}