using AutoMapper;
using EgfrNsbEvents;
using Signify.eGFR.Core.ApiClients.MemberApi.Responses;
using Signify.eGFR.Core.ApiClients.ProviderPayApi.Requests;
using Signify.eGFR.Core.ApiClients.RcmApi.Requests;
using Signify.eGFR.Core.Commands;
using Signify.eGFR.Core.Constants;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Events.Akka;
using Signify.eGFR.Core.Events.Status;
using Signify.eGFR.Core.Events;
using System;
using System.Text.Json;
using BillRequestSent = Signify.eGFR.Core.Events.Status.BillRequestSent;
using NotPerformedReason = Signify.eGFR.Core.Models.NotPerformedReason;
using NotPerformedReasonEntity = Signify.eGFR.Core.Data.Entities.NotPerformedReason;
using OrderCreationEvent = EgfrNsbEvents.OrderCreationEvent;
using PdfDeliveredToClientEntity = Signify.eGFR.Core.Data.Entities.PdfDeliveredToClient;
using PdfDeliveredToClient = EgfrEvents.PdfDeliveredToClient;

namespace Signify.eGFR.Core.Maps;

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
                mapper => mapper.MapFrom(source => source.ReceivedByeGFRProcessManagerDateTime));

        CreateMap<MemberInfo, Exam>();

        CreateMap<Exam, ExamNotPerformed>();

        CreateMap<NotPerformedReason, ExamNotPerformed>()
            .ForMember(
                destination => destination.NotPerformedReasonId,
                mapper => mapper.MapFrom(source => Convert(source)));

        CreateMap<ExamStatusEvent, ExamStatus>()
            .ForMember(destination => destination.ExamStatusCodeId,
                mapper => mapper.MapFrom(source => source.StatusCode.StatusCodeId))
            .ForMember(destination => destination.StatusDateTime,
                mapper => mapper.MapFrom(source => source.StatusDateTime))
            .ForMember(destination => destination.ExamStatusCode,
                mapper => mapper.Ignore());

        CreateMap<EgfrLabResult, QuestLabResult>()
            .ForMember(dest => dest.CreatedDateTime,
                mapper => mapper.MapFrom(src => src.ReceivedByEgfrDateTime));
        
        CreateMap<KedEgfrLabResult, LabResult>()
            .ForMember(dest => dest.CreatedDateTime,
                mapper => mapper.MapFrom(src => src.ReceivedByEgfrDateTime))
            .ForMember(dest => dest.ReceivedDate,
                mapper => mapper.MapFrom(src => src.DateLabReceived))
            .ForMember(dest => dest.ResultDescription,
                mapper => mapper.MapFrom(src => src.EstimatedGlomerularFiltrationRateResultDescription));

        CreateMap<PdfDeliveredToClient, PdfDeliveredToClientEntity>()
            .ForMember(
                destination => destination.DeliveryDateTime,
                mapper => mapper.MapFrom(source => source.DeliveryDateTime))
            .ForMember(
                destination => destination.CreatedDateTime,
                mapper => mapper.MapFrom(source => source.CreatedDateTime));

        CreateMap<PdfDeliveredToClientEntity, BillableEvent>()
            .ForMember(destination => destination.BillableDate,
                mapper => mapper.MapFrom(source => source.DeliveryDateTime));
        CreateMap<BillableEvent, CreateBillEvent>();

        CreateMap<Exam, CreateBillRequest>()
            .ConvertUsing<CreateBillRequestMapper>();
        CreateMap<CreateBillEvent, CreateBillRequest>()
            .ConvertUsing<CreateBillRequestMapper>();

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

        CreateMap<Exam, ResultsReceived>()
            .ConvertUsing<ResultsReceivedMapper>();
        CreateMap<QuestLabResult, ResultsReceived>()
            .ConvertUsing<ResultsReceivedMapper>();
        
        CreateMap<LabResult, ResultsReceived>()
            .ConvertUsing<ResultsReceivedMapper>();

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
                map => map.MapFrom(src => src.StatusCode.StatusCodeId))
            .ForMember(dest => dest.Exam,
                map => map.Ignore());

        CreateMap<Exam, ProviderPayRequest>()
            .ForMember(dest => dest.ProviderProductCode, mapper => mapper.MapFrom(value => ProductCodes.eGFR))
            .ForMember(dest => dest.PersonId, mapper => mapper.MapFrom(src => src.CenseoId))
            .ForMember(dest => dest.DateOfService, mapper => 
                mapper.MapFrom(src => DateOnly.FromDateTime(src.DateOfService.Value.Date).ToString("o")));
        
        CreateMap<ProviderPayStatusEvent, ProviderPayRequestSent>()
            .ForMember(dest => dest.ProviderPayProductCode,
                mapper => mapper.MapFrom(value => ProductCodes.eGFR))
            .ForMember(dest => dest.ProductCode,
                mapper => mapper.MapFrom(value => ProductCodes.eGFR))
            .ForMember(dest => dest.CreatedDate, mapper => mapper.Ignore())
            .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => src.ParentEventReceivedDateTime))
            .ForMember(dest => dest.PaymentId, mapper => mapper.MapFrom(src => src.PaymentId))
            .ForMember(dest => dest.ParentEventDateTime, mapper => mapper.MapFrom(src => src.StatusDateTime));
        
        CreateMap<ProviderPayStatusEvent, ProviderPayableEventReceived>()
            .ForMember(dest => dest.ProductCode,
                mapper => mapper.MapFrom(value => ProductCodes.eGFR))
            .ForMember(dest => dest.CreatedDate, mapper => mapper.Ignore())
            .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => src.ParentEventReceivedDateTime));

        CreateMap<ProviderPayStatusEvent, ProviderNonPayableEventReceived>()
            .ForMember(dest => dest.ProductCode,
                mapper => mapper.MapFrom(value => ProductCodes.eGFR))
            .ForMember(dest => dest.CreatedDate, mapper => mapper.Ignore())
            .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => src.ParentEventReceivedDateTime));

        CreateMap<ProviderPayRequest, ProviderPayStatusEvent>()
            .ForMember(dest => dest.ParentCdiEvent, mapper => mapper.MapFrom(src => src.ParentEvent))
            .ForMember(dest => dest.StatusDateTime, mapper => mapper.MapFrom(src => src.ParentEventDateTime));

        CreateMap<CdiEventBase, ProviderPayStatusEvent>()
            .ForMember(dest => dest.EventId, mapper => mapper.MapFrom(src => src.RequestId))
            .ForMember(dest => dest.StatusDateTime, mapper => mapper.MapFrom(src => src.DateTime))
            .ForMember(dest => dest.ParentEventReceivedDateTime, mapper => mapper.MapFrom(src => src.ReceivedByEgfrDateTime))
            .ForMember(dest => dest.ParentCdiEvent, mapper => mapper.MapFrom(src => src.GetType().Name));

        CreateMap<CdiEventBase, ProviderPayRequest>()
            .ForMember(dest => dest.EventId, mapper => mapper.MapFrom(src => src.RequestId))
            .ForMember(dest => dest.ParentEventDateTime, mapper => mapper.MapFrom(src => src.DateTime))
            .ForMember(dest => dest.ParentEventReceivedDateTime, mapper => mapper.MapFrom(src => src.ReceivedByEgfrDateTime))
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

        CreateMap<OrderCreationEvent, Events.Akka.OrderCreationEvent>();
        
        CreateMap<JsonElement, KedEgfrLabResult>()
            .ConvertUsing<KedEgfrLabResultMapper>();
    }

    private static short Convert(NotPerformedReason reason)
    {
        return reason switch
        {
            NotPerformedReason.MemberRecentlyCompleted => NotPerformedReasonEntity.MemberRecentlyCompleted.NotPerformedReasonId,
            NotPerformedReason.ScheduledToComplete => NotPerformedReasonEntity.ScheduledToComplete.NotPerformedReasonId,
            NotPerformedReason.MemberApprehension => NotPerformedReasonEntity.MemberApprehension.NotPerformedReasonId,
            NotPerformedReason.NotInterested => NotPerformedReasonEntity.NotInterested.NotPerformedReasonId,
            NotPerformedReason.TechnicalIssue => NotPerformedReasonEntity.TechnicalIssue.NotPerformedReasonId,
            NotPerformedReason.EnvironmentalIssue => NotPerformedReasonEntity.EnvironmentalIssue.NotPerformedReasonId,
            NotPerformedReason.NoSuppliesOrEquipment => NotPerformedReasonEntity.NoSuppliesOrEquipment.NotPerformedReasonId,
            NotPerformedReason.InsufficientTraining => NotPerformedReasonEntity.InsufficientTraining.NotPerformedReasonId,
            NotPerformedReason.ClinicallyNotRelevant => NotPerformedReasonEntity.ClinicallyNotRelevant.NotPerformedReasonId,
            NotPerformedReason.MemberPhysicallyUnable => NotPerformedReasonEntity.MemberPhysicallyUnable.NotPerformedReasonId,
            _ => throw new NotImplementedException("Unhandled NotPerformedReason: " + reason)
        };
    }
}