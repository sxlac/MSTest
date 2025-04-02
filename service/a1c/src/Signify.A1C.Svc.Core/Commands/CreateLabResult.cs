using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.A1C.Svc.Core.Data;
using Microsoft.EntityFrameworkCore;
using Signify.A1C.Svc.Core.Data.Entities;

namespace Signify.A1C.Svc.Core.Commands
{
    public class CreateLabResult : IRequest<Data.Entities.LabResults>
	{
		  public int LabResultId { get; set; }
		  public Guid OrderCorrelationId { get; set; }
	      public int A1CId { get; set; }
		  public string Barcode { get; set; }
		  public string LabResults { get; set; }
		  public string LabTestType { get; set; }
		  public string AbnormalIndicator { get; set; }
		  public string Exception { get; set; }
		  public DateTime? CollectionDate { get; set; }
		  public DateTime? ServiceDate { get; set; }
		  public DateTime? ReleaseDate { get; set; }
		
	}

	public class CreateLabResultLabResultHandler : IRequestHandler<CreateLabResult, Data.Entities.LabResults>
	{
		private readonly A1CDataContext _context;
		private readonly IMapper _mapper;
		private readonly ILogger<CreateLabResultLabResultHandler> _logger;

		public CreateLabResultLabResultHandler(A1CDataContext context, IMapper mapper, ILogger<CreateLabResultLabResultHandler> logger)
		{
			_context = context;
			_mapper = mapper;
			_logger = logger;
		}

		[Trace]
		public async Task<Data.Entities.LabResults> Handle(CreateLabResult request, CancellationToken cancellationToken)
		{
			LabResults labResults = _mapper.Map<LabResults>(request);
			
			var newLabResult = await _context.LabResults.AddAsync(labResults, cancellationToken);
			await _context.SaveChangesAsync(cancellationToken);

			_logger.LogInformation($"Lab Result Received and Created in LabResult Table. ");
			return newLabResult.Entity;
		}


	}
}
