using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.FOBT.Svc.Core.Data;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.FOBT.Svc.Core.Commands;

[ExcludeFromCodeCoverage]
public class CreateOrUpdateFOBT : IRequest<Data.Entities.FOBT>
{
    public int FOBTId { get; set; }
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

public class CreateOrUpdateFOBTHandler : IRequestHandler<CreateOrUpdateFOBT, Data.Entities.FOBT>
{
    private readonly FOBTDataContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateOrUpdateFOBTHandler> _logger;

    public CreateOrUpdateFOBTHandler(FOBTDataContext context, IMapper mapper, ILogger<CreateOrUpdateFOBTHandler> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    [Trace]
    public async Task<Data.Entities.FOBT> Handle(CreateOrUpdateFOBT request, CancellationToken cancellationToken)
    {
        var fobt = _mapper.Map<Data.Entities.FOBT>(request);

        if (request.FOBTId == 0)
        {
            //Create FOBT row
            var newFobt = await _context.FOBT.AddAsync(fobt, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return newFobt.Entity;
        }
        //update FOBT row

        _logger.LogDebug("Updating FOBT FobtId:{FobtId}", fobt.FOBTId);
        var updateFobt = _context.FOBT.Update(fobt);
        //update FOBT status
        await _context.SaveChangesAsync(cancellationToken);
        return updateFobt.Entity;
    }
}