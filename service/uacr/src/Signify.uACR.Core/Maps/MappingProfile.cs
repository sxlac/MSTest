using AutoMapper;
using NotPerformedReason = Signify.uACR.Core.Models.NotPerformedReason;
using NotPerformedReasonEntity = Signify.uACR.Core.Data.Entities.NotPerformedReason;
using OrderCreationEvent = UacrNsbEvents.OrderCreationEvent;
using Signify.uACR.Core.ApiClients.MemberApi.Responses;
using Signify.uACR.Core.ApiClients.ProviderPayAPi.Requests;
using Signify.uACR.Core.ApiClients.RcmApi.Requests;
using Signify.uACR.Core.Constants;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Events.Akka;
using Signify.uACR.Core.Events.Status;
using Signify.uACR.Core.Events;
using System;
using System.Text.Json;
using UacrNsbEvents;

namespace Signify.uACR.Core.Maps;

/// <summary>
/// Profile for mapping types using <see cref="AutoMapper"/>
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<EvaluationFinalizedEvent, EvalReceived>()
            .ForMember(
                // This is the only DateTime property on EvaluationFinalizedEvent as of today that is set to a non-UTC kind
                destination => destination.DateOfService,
                mapper => mapper.MapFrom(source => source.DateOfService));

        CreateMap<EvalReceived, Exam>()
            .ForMember(
                destination => destination.EvaluationReceivedDateTime,
                mapper => mapper.MapFrom(source => source.ReceivedDateTime))
            .ForMember(
                destination => destination.EvaluationCreatedDateTime,
                mapper => mapper.MapFrom(source => source.CreatedDateTime.UtcDateTime))
            .ForMember(
                destination => destination.CreatedDateTime,
                mapper => mapper.MapFrom(source => source.ReceivedByUacrProcessManagerDateTime));

        CreateMap<MemberInfo, Exam>();

        CreateMap<Exam, ExamNotPerformed>();

        CreateMap<NotPerformedReason, ExamNotPerformed>()
            .ForMember(
                destination => destination.NotPerformedReasonId,
                mapper => mapper.MapFrom(source => Convert(source)));

        CreateMap<ExamStatusEvent, ExamStatus>()
            .ForMember(destination => destination.ExamStatusCodeId,
                map => map.MapFrom(src => src.StatusCode.ExamStatusCodeId))
            .ForMember(destination => destination.StatusDateTime,
                mapper => mapper.MapFrom(source => source.StatusDateTime))
            .ForMember(destination => destination.ExamStatusCode,
                mapper => mapper.Ignore());

        CreateMap<Exam, Performed>()
            .ForMember(destination => destination.ReceivedDate,
                mapper => mapper.MapFrom(source => source.EvaluationReceivedDateTime))
            .ForMember(destination => destination.CreatedDate,
                mapper => mapper.MapFrom(source => source.EvaluationCreatedDateTime));

        CreateMap<Exam, NotPerformed>()
            .ForMember(destination => destination.ReceivedDate,
                mapper => mapper.MapFrom(source => source.EvaluationReceivedDateTime))
            .ForMember(destination => destination.CreatedDate,
                mapper => mapper.MapFrom(source => source.EvaluationCreatedDateTime));

        CreateMap<ExamNotPerformed, NotPerformed>()
            .ForMember(destination => destination.Reason,
                mapper => mapper.MapFrom(source => source.NotPerformedReason.Reason))
            .ForMember(destination => destination.ReasonNotes,
                mapper => mapper.MapFrom(source => source.Notes));

        CreateMap<Exam, BillRequestSent>()
            .ForMember(destination => destination.ReceivedDate,
                mapper => mapper.MapFrom(source => source.EvaluationReceivedDateTime))
            .ForMember(destination => destination.CreatedDate,
                mapper => mapper.MapFrom(source => source.EvaluationCreatedDateTime));

        CreateMap<Exam, BillRequestNotSent>()
            .ForMember(destination => destination.ReceivedDate,
                mapper => mapper.MapFrom(source => source.EvaluationReceivedDateTime))
            .ForMember(destination => destination.CreatedDate,
                mapper => mapper.MapFrom(source => source.EvaluationCreatedDateTime));

        CreateMap<ExamStatusEvent, ExamStatus>()
            .ForMember(dest => dest.ExamStatusCodeId,
                map => map.MapFrom(src => src.StatusCode.ExamStatusCodeId))
            .ForMember(dest => dest.Exam,
                map => map.Ignore());

        CreateMap<OrderCreationEvent, Events.Akka.OrderCreationEvent>();
        
        CreateMap<UacrEvents.PdfDeliveredToClient, PdfDeliveredToClient>()
            .ForMember(
                destination => destination.DeliveryDateTime,
                mapper => mapper.MapFrom(source => source.DeliveryDateTime))
            .ForMember(
                destination => destination.CreatedDateTime,
                mapper => mapper.MapFrom(source => source.CreatedDateTime));
        
        CreateMap<Exam, ProviderPayRequest>()
            .ForMember(dest => dest.ProviderProductCode, mapper => mapper.MapFrom(value => ProductCodes.uACR))
            .ForMember(dest => dest.PersonId, mapper => mapper.MapFrom(src => src.CenseoId))
            .ForMember(dest => dest.DateOfService, mapper => 
                mapper.MapFrom(src => DateOnly.FromDateTime(src.DateOfService.Value.Date).ToString("o")));
        
        CreateMap<ProviderPayStatusEvent, ProviderPayRequestSent>()
            .ForMember(dest => dest.ProviderPayProductCode,
                mapper => mapper.MapFrom(value => ProductCodes.uACR))
            .ForMember(dest => dest.ProductCode,
                mapper => mapper.MapFrom(value => ProductCodes.uACR))
            .ForMember(dest => dest.CreatedDate, mapper => mapper.Ignore())
            .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => src.ParentEventReceivedDateTime))
            .ForMember(dest => dest.PaymentId, mapper => mapper.MapFrom(src => src.PaymentId))
            .ForMember(dest => dest.ParentEventDateTime, mapper => mapper.MapFrom(src => src.StatusDateTime));
        
        CreateMap<ProviderPayStatusEvent, ProviderPayableEventReceived>()
            .ForMember(dest => dest.ProductCode,
                mapper => mapper.MapFrom(value => ProductCodes.uACR))
            .ForMember(dest => dest.CreatedDate, mapper => mapper.Ignore())
            .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => src.ParentEventReceivedDateTime));

        CreateMap<ProviderPayStatusEvent, ProviderNonPayableEventReceived>()
            .ForMember(dest => dest.ProductCode,
                mapper => mapper.MapFrom(value => ProductCodes.uACR))
            .ForMember(dest => dest.CreatedDate, mapper => mapper.Ignore())
            .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => src.ParentEventReceivedDateTime));

        CreateMap<ProviderPayRequest, ProviderPayStatusEvent>()
            .ForMember(dest => dest.ParentCdiEvent, mapper => mapper.MapFrom(src => src.ParentEvent))
            .ForMember(dest => dest.StatusDateTime, mapper => mapper.MapFrom(src => src.ParentEventDateTime));

        CreateMap<CdiEventBase, ProviderPayStatusEvent>()
            .ForMember(dest => dest.EventId, mapper => mapper.MapFrom(src => src.RequestId))
            .ForMember(dest => dest.StatusDateTime, mapper => mapper.MapFrom(src => src.DateTime))
            .ForMember(dest => dest.ParentEventReceivedDateTime, mapper => mapper.MapFrom(src => src.ReceivedByUacrDateTime))
            .ForMember(dest => dest.ParentCdiEvent, mapper => mapper.MapFrom(src => src.GetType().Name));

        CreateMap<CdiEventBase, ProviderPayRequest>()
            .ForMember(dest => dest.EventId, mapper => mapper.MapFrom(src => src.RequestId))
            .ForMember(dest => dest.ParentEventDateTime, mapper => mapper.MapFrom(src => src.DateTime))
            .ForMember(dest => dest.ParentEventReceivedDateTime, mapper => mapper.MapFrom(src => src.ReceivedByUacrDateTime))
            .ForMember(dest => dest.ParentEvent, mapper => mapper.MapFrom(src => src.GetType().Name));
        
        CreateMap<Exam, ProviderPayStatusEvent>()
            .ForMember(dest => dest.EvaluationId, 
                map => map.Ignore())
            .ForMember(dest => dest.ExamId, 
                map => map.MapFrom(src => src.ExamId))
            .ForMember(dest => dest.MemberPlanId, 
                map => map.MapFrom(src => src.MemberPlanId))
            .ForMember(dest => dest.ProviderId, 
                map => map.MapFrom(src => src.ProviderId));
        
        CreateMap<ProviderPayRequest, ProviderPayApiRequest>();
        
        CreateMap<ProviderPayRequest, SaveProviderPay>();
        CreateMap<SaveProviderPay, ProviderPayStatusEvent>()
            .ForMember(dest => dest.StatusDateTime, mapper => mapper.MapFrom(src => src.ParentEventDateTime))
            .ForMember(dest => dest.ParentCdiEvent, mapper => mapper.MapFrom(src => src.ParentEvent));

        CreateMap<KedUacrLabResult, LabResult>()
            .ForMember(dest => dest.CreatedDateTime,
                mapper => mapper.MapFrom(src => src.ReceivedByUacrDateTime))
            .ForMember(dest => dest.ReceivedDate,
                mapper => mapper.MapFrom(src => src.DateLabReceived))
            .ForMember(dest => dest.ResultColor,
                        mapper => mapper.MapFrom(src => src.UrineAlbuminToCreatinineRatioResultColor))
            .ForMember(dest => dest.ResultDescription,
                mapper => mapper.MapFrom(src => src.UrineAlbuminToCreatinineRatioResultDescription));
        
        CreateMap<Exam, ResultsReceived>()
            .ConvertUsing<ResultsReceivedMapper>();
        CreateMap<LabResult, ResultsReceived>()
            .ConvertUsing<ResultsReceivedMapper>();

        CreateMap<Exam, CreateBillRequest>()
            .ConvertUsing<CreateBillRequestMapper>();
        CreateMap<CreateBillEvent, CreateBillRequest>()
            .ConvertUsing<CreateBillRequestMapper>();
        
        CreateMap<JsonElement, KedUacrLabResult>()
            .ConvertUsing<KedUacrLabResultMapper>();
    }

    private static short Convert(NotPerformedReason reason)
    {
        return reason switch
        {
            NotPerformedReason.ScheduledToComplete => NotPerformedReasonEntity.ScheduledToComplete.NotPerformedReasonId,
            NotPerformedReason.MemberApprehension => NotPerformedReasonEntity.MemberApprehension.NotPerformedReasonId,
            NotPerformedReason.NotInterested => NotPerformedReasonEntity.NotInterested.NotPerformedReasonId,
            NotPerformedReason.TechnicalIssue => NotPerformedReasonEntity.TechnicalIssue.NotPerformedReasonId,
            NotPerformedReason.EnvironmentalIssue => NotPerformedReasonEntity.EnvironmentalIssue.NotPerformedReasonId,
            NotPerformedReason.NoSuppliesOrEquipment => NotPerformedReasonEntity.NoSuppliesOrEquipment.NotPerformedReasonId,
            NotPerformedReason.InsufficientTraining => NotPerformedReasonEntity.InsufficientTraining.NotPerformedReasonId,
            NotPerformedReason.MemberPhysicallyUnable => NotPerformedReasonEntity.MemberPhysicallyUnable.NotPerformedReasonId,
            NotPerformedReason.MemberRecentlyCompleted => NotPerformedReasonEntity.MemberRecentlyCompleted.NotPerformedReasonId,
            _ => throw new NotImplementedException("Unhandled NotPerformedReason: " + reason)
        };
    }
}