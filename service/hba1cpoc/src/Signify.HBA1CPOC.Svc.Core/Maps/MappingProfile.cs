using AutoMapper;
using Refit;
using Signify.HBA1CPOC.Messages.Events;
using Signify.HBA1CPOC.Messages.Events.Akka;
using Signify.HBA1CPOC.Messages.Events.Status;
using Signify.HBA1CPOC.Sagas;
using Signify.HBA1CPOC.Svc.Core.ApiClient.Requests;
using Signify.HBA1CPOC.Svc.Core.ApiClient.Response;
using Signify.HBA1CPOC.Svc.Core.Commands;
using Signify.HBA1CPOC.Svc.Core.Constants;
using Signify.HBA1CPOC.Svc.Core.Data.Entities;
using Signify.HBA1CPOC.Svc.Core.Events;
using Signify.HBA1CPOC.Svc.Core.Models;
using Signify.HBA1CPOC.Svc.Core.Queries;
using Signify.HBA1CPOC.Svc.Core.Sagas.Commands;
using System;
using Result = Signify.HBA1CPOC.Sagas.Result;

namespace Signify.HBA1CPOC.Svc.Core.Maps;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<EvaluationFinalizedEvent, EvalReceived>();
        CreateMap<EvaluationAnswers, CreateOrUpdateHBA1CPOC>().ForMember(dest => dest.ExpirationDate, mapper => mapper.MapFrom(src => DateOnly.FromDateTime(src.ExpirationDate)));
        CreateMap<EvalReceived, CreateOrUpdateHBA1CPOC>();
        CreateMap<EvalReceived, GetMemberInfo>();
        CreateMap<MemberInfoRs, CreateOrUpdateHBA1CPOC>().ForMember(dest => dest.DateOfBirth, mapper => mapper.MapFrom(src => DateOnly.FromDateTime(src.DateOfBirth.Value)));
        CreateMap<CreateOrUpdateHBA1CPOC, Data.Entities.HBA1CPOC>().ReverseMap();
        CreateMap<Data.Entities.HBA1CPOC, A1CPOCPerformed>()
            .ForMember(dest => dest.CorrelationId, mapper => mapper.MapFrom(src => Guid.NewGuid()));

        CreateMap<A1CPOCPerformed, Data.Entities.HBA1CPOC>();

        CreateMap<A1CPOCPerformed, UpdateInventory>();
        CreateMap<UpdateInventory, Data.Entities.HBA1CPOC>();

        CreateMap<ApiResponse<MemberInfoRs>, MemberInfoRs>();
        CreateMap<UpdateInventory, UpdateInventoryRequest>()
            .ForMember(dest => dest.DateUpdated, mapper => mapper.MapFrom(src => src.ReceivedDateTime))
            .ForMember(dest => dest.ItemNumber, mapper => mapper.MapFrom(src => Constants.ApplicationConstants.ItemNumber))
            .ForMember(dest => dest.RequestId, mapper => mapper.MapFrom(src => src.CorrelationId))
            .ForMember(dest => dest.Quantity, mapper => mapper.MapFrom(src => 1));

        CreateMap<InventoryUpdated, InventoryUpdateReceived>().ForMember(dest => dest.ExpirationDate, mapper => mapper.MapFrom(src => DateOnly.FromDateTime(src.ExpirationDate.Value)));
        CreateMap<Signify.HBA1CPOC.Messages.Events.Result, Result>();

        CreateMap<PdfDeliveredToClient, CreateOrUpdatePDFToClient>()
            .ForMember(dest => dest.DeliveryCreatedDateTime, mapper => mapper.MapFrom(src => src.CreatedDateTime));
        CreateMap<CreateOrUpdatePDFToClient, PDFToClient>()
            .ForMember(dest => dest.DeliveryCreatedDateTime, mapper => mapper.MapFrom(src => src.DeliveryCreatedDateTime.ToUniversalTime()))
            .ForMember(dest => dest.DeliveryDateTime, mapper => mapper.MapFrom(src => src.DeliveryDateTime.ToUniversalTime()));
        CreateMap<RCMBillingRequest, CreateBillRequest>();
        CreateMap<Data.Entities.HBA1CPOC, RCMBillingRequest>()
            .ForMember(dest => dest.UsStateOfService, mapper => mapper.MapFrom(src => src.State))
            .ForMember(dest => dest.SharedClientId, mapper => mapper.MapFrom(src => src.ClientId));

        CreateMap<NotPerformedReason, Hba1CpocNotPerformed>()
            .ForMember(dest => dest.NotPerformedReasonId, mapper => mapper.MapFrom(src => src.NotPerformedReasonId));
        CreateMap<EvaluationFinalizedEvent, CreateHbA1CPoc>();
        CreateMap<CreateHbA1CPoc, EvalReceived>();

        CreateMap<Data.Entities.HBA1CPOC, ResultsReceived>()
            .ForMember(dest => dest.ProductCode, mapper => mapper.MapFrom(_ => ApplicationConstants.ProductCode))
            .ForMember(dest => dest.PerformedDate, mapper => mapper.MapFrom(source => source.CreatedDateTime))
            .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(source => source.ReceivedDateTime));

        CreateMap<ResultsModel, ResultsReceived>()
            .ConvertUsing<ResultsReceivedMapper>();

        CreateMap<Normality, string>()
            .ConvertUsing<NormalityMapper>();
        CreateMap<string, Normality>()
            .ConvertUsing<NormalityMapper>();

        CreateMap<Data.Entities.HBA1CPOC, Performed>()
            .ForMember(dest => dest.ProductCode, mapper => mapper.MapFrom(_ => ApplicationConstants.ProductCode))
            .ForMember(dest => dest.CreateDate, mapper => mapper.MapFrom(source => source.CreatedDateTime))
            .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(source => source.ReceivedDateTime));

        CreateMap<Data.Entities.HBA1CPOC, NotPerformed>()
            .ForMember(dest => dest.ProductCode, mapper => mapper.MapFrom(_ => ApplicationConstants.ProductCode))
            .ForMember(dest => dest.CreateDate, mapper => mapper.MapFrom(source => source.CreatedDateTime))
            .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(source => source.ReceivedDateTime));

        CreateMap<HBA1CPOCStatus, BillRequestSent>(MemberList.None)
            .ForMember(dest => dest.ProductCode, mapper => mapper.MapFrom(_ => ApplicationConstants.ProductCode))
            .ForMember(dest => dest.BillingProductCode, mapper => mapper.MapFrom(_ => ApplicationConstants.ProductCode))
            .ForMember(dest => dest.CreateDate, mapper => mapper.MapFrom(src => src.CreatedDateTime))
            .ForMember(dest => dest.ProviderId, mapper => mapper.MapFrom(src => src.HBA1CPOC.ProviderId))
            .ForMember(dest => dest.EvaluationId, mapper => mapper.MapFrom(src => src.HBA1CPOC.EvaluationId))
            .ForMember(dest => dest.MemberPlanId, mapper => mapper.MapFrom(src => src.HBA1CPOC.MemberPlanId));


        CreateMap<Data.Entities.HBA1CPOC, BillRequestSent>();

        CreateMap<Data.Entities.HBA1CPOC, BillRequestNotSent>()
            .ForMember(dest => dest.ProductCode, mapper => mapper.MapFrom(_ => ApplicationConstants.ProductCode))
            .ForMember(dest => dest.BillingProductCode, mapper => mapper.MapFrom(_ => ApplicationConstants.ProductCode))
            .ForMember(dest => dest.CreateDate, mapper => mapper.MapFrom(src => src.CreatedDateTime))
            .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => src.ReceivedDateTime));

        CreateMap<Data.Entities.HBA1CPOC, ProviderPayRequest>()
            .ForMember(dest => dest.ProviderProductCode, mapper => mapper.MapFrom(value => ApplicationConstants.ProductCode))
            .ForMember(dest => dest.PersonId, mapper => mapper.MapFrom(src => src.CenseoId))
            .ForMember(dest => dest.DateOfService, mapper => mapper.MapFrom(src => DateOnly.FromDateTime((DateTime)src.DateOfService).ToString("o")));

        CreateMap<ProviderPayRequest, ProviderPayApiRequest>();

        CreateMap<Data.Entities.HBA1CPOC, ProviderPayRequestSent>()
            .ForMember(dest => dest.ProviderPayProductCode, mapper => mapper.MapFrom(value => ApplicationConstants.ProductCode))
            .ForMember(dest => dest.CreateDate, mapper => mapper.MapFrom(src => src.CreatedDateTime.UtcDateTime))
            .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => new DateTimeOffset(src.ReceivedDateTime, TimeSpan.Zero)))
            .ForMember(dest => dest.EvaluationId, mapper => mapper.MapFrom(src => src.EvaluationId))
            .ForMember(dest => dest.ProviderId, mapper => mapper.MapFrom(src => src.ProviderId))
            .ForMember(dest => dest.MemberPlanId, mapper => mapper.MapFrom(src => src.MemberPlanId))
            .ForMember(dest => dest.ProductCode, mapper => mapper.MapFrom(value => ApplicationConstants.ProductCode));

        CreateMap<Data.Entities.HBA1CPOC, ProviderPayableEventReceived>()
            .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => new DateTimeOffset(src.ReceivedDateTime, TimeSpan.Zero)))
            .ForMember(dest => dest.CreateDate, mapper => mapper.MapFrom(src => src.CreatedDateTime.UtcDateTime))
            .ForMember(dest => dest.ProductCode, mapper => mapper.MapFrom(src => ApplicationConstants.ProductCode));

        CreateMap<Data.Entities.HBA1CPOC, ProviderNonPayableEventReceived>()
            .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => new DateTimeOffset(src.ReceivedDateTime, TimeSpan.Zero)))
            .ForMember(dest => dest.CreateDate, mapper => mapper.MapFrom(src => src.CreatedDateTime.UtcDateTime))
            .ForMember(dest => dest.ProductCode, mapper => mapper.MapFrom(src => ApplicationConstants.ProductCode));
    }
}