using AutoMapper;
using Signify.uACR.Core.ApiClients.RcmApi.Requests;
using Signify.uACR.Core.Data.Entities;
using System.Collections.Generic;
using UacrNsbEvents;

namespace Signify.uACR.Core.Maps;

public class CreateBillRequestMapper : ITypeConverter<Exam, CreateBillRequest>,
    ITypeConverter<CreateBillEvent, CreateBillRequest>
{
    public CreateBillRequest Convert(Exam source, CreateBillRequest destination, ResolutionContext context)
    {
        destination ??= new CreateBillRequest();

        destination.UsStateOfService = source.State;
        destination.SharedClientId = source.ClientId;
        destination.ProviderId = source.ProviderId;
        destination.DateOfService = source.DateOfService;
        destination.ApplicationId = Constants.Application.ApplicationId;
        destination.MemberPlanId = source.MemberPlanId;

        destination.AdditionalDetails ??= new Dictionary<string, string>();
        destination.AdditionalDetails["appointmentId"] = source.AppointmentId.ToString(); // Yes, this needs to be "appointmentId", not "AppointmentId"

        return destination;
    }

    public CreateBillRequest Convert(CreateBillEvent source, CreateBillRequest destination, ResolutionContext context)
    {
        destination ??= new CreateBillRequest();

        destination.BillableDate = source.BillableDate;

        destination.AdditionalDetails ??= new Dictionary<string, string>();
        destination.AdditionalDetails["BatchName"] = source.BatchName;
        destination.AdditionalDetails["EvaluationId"] = source.EvaluationId.ToString();

        return destination;
    }
}