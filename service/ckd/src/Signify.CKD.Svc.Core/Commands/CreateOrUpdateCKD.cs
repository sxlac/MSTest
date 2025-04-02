using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using NewRelic.Api.Agent;
using Signify.CKD.Svc.Core.Data;

namespace Signify.CKD.Svc.Core.Commands
{
	public class CreateOrUpdateCKD : IRequest<Data.Entities.CKD>
	{
		public int CKDId { get; set; }
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
		public DateTime DateOfBirth { get; set; }
		public string AddressLineOne { get; set; }
		public string AddressLineTwo { get; set; }
		public string City { get; set; }
		public string State { get; set; }
		public string ZipCode { get; set; }
		public string NationalProviderIdentifier { get; set; }
		public string CKDAnswer { get; set; }
		public DateTime? ExpirationDate { get; set; }
	}

	public class CreateOrUpdateCKDHandler : IRequestHandler<CreateOrUpdateCKD, Data.Entities.CKD>
	{
		private readonly CKDDataContext _context;
		private readonly IMapper _mapper;

        public CreateOrUpdateCKDHandler(CKDDataContext context, IMapper mapper)
		{
			_context = context;
			_mapper = mapper;
        }

		[Trace]
		public async Task<Data.Entities.CKD> Handle(CreateOrUpdateCKD request, CancellationToken cancellationToken)
		{
			var ckd = _mapper.Map<Data.Entities.CKD>(request);
			var record = request.CKDId < 1
				? await _context.CKD.AddAsync(ckd, cancellationToken)
				: _context.CKD.Update(ckd);
			await _context.SaveChangesAsync(cancellationToken);

			return record.Entity;
		}
	}
}
