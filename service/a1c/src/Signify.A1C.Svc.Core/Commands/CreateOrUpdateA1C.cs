using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.A1C.Svc.Core.Data;

namespace Signify.A1C.Svc.Core.Commands
{
    public class CreateOrUpdateA1C : IRequest<Data.Entities.A1C>
	{
		public int A1CId { get; set; }
		public int EvaluationId { get; set; }
		public int MemberId { get; set; }
		public int MemberPlanId { get; set; }
		public string CenseoId { get; set; }
		public int AppointmentId { get; set; }
		public int ProviderId { get; set; }
		public DateTime? DateOfService { get; set; }
		public DateTimeOffset CreatedDateTime { get; set; }
		public DateTime ReceivedDateTime { get; set; }
		public string Barcode { get; set; }
		public int ClientId { get; set; }
		public string UserName { get; set; }
		public string ApplicationId { get; set; }
		public string FirstName { get; set; }
		public string MiddleName { get; set; }
		public string LastName { get; set; }
		public DateTime? DateOfBirth { get; set; }
		public string AddressLineOne { get; set; }
		public string AddressLineTwo { get; set; }
		public string City { get; set; }
		public string State { get; set; }
		public string ZipCode { get; set; }
		public string NationalProviderIdentifier { get; set; }
		public Guid? OrderCorrelationId { get; set; }
	}

	public class CreateOrUpdateA1CHandler : IRequestHandler<CreateOrUpdateA1C, Data.Entities.A1C>
	{
		private readonly A1CDataContext _context;
		private readonly IMapper _mapper;
		private readonly ILogger<CreateOrUpdateA1CHandler> _logger;

		public CreateOrUpdateA1CHandler(A1CDataContext context, IMapper mapper, ILogger<CreateOrUpdateA1CHandler> logger)
		{
			_context = context;
			_mapper = mapper;
			_logger = logger;
		}

		[Trace]
		public async Task<Data.Entities.A1C> Handle(CreateOrUpdateA1C request, CancellationToken cancellationToken)
		{
			var a1C = _mapper.Map<Data.Entities.A1C>(request);

			if (request.A1CId == 0)
			{
				//Create A1C row
				var newA1C = await _context.A1C.AddAsync(a1C, cancellationToken);
				await _context.SaveChangesAsync(cancellationToken);
				return newA1C.Entity;


			}
			//update A1C row
			else
			{
				_logger.LogDebug($"Updating a1c A1CID:{a1C.A1CId}");
				var updateA1C = _context.A1C.Update(a1C);
				//update a1c status
				await _context.SaveChangesAsync(cancellationToken);
				return updateA1C.Entity;
			}

		}


	}
}
