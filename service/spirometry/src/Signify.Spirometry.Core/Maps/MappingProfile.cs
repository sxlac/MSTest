using AutoMapper;
using System;
using Signify.Spirometry.Core.ApiClients.CdiApi.Flags.Requests;
using Signify.Spirometry.Core.ApiClients.MemberApi.Responses;
using Signify.Spirometry.Core.ApiClients.RcmApi.Requests;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Events;
using Signify.Spirometry.Core.Events.Akka;
using Signify.Spirometry.Core.Events.Status;
using Signify.Spirometry.Core.Models;
using SpiroEvents;
using SpiroNsbEvents;
using Signify.Spirometry.Core.ApiClients.ProviderPayApi.Requests;
using SpiroNsb.SagaEvents;

using ExamStatusEvent = Signify.Spirometry.Core.Events.ExamStatusEvent;
using NormalityIndicator = Signify.Spirometry.Core.Models.NormalityIndicator;
using NotPerformedReason = Signify.Spirometry.Core.Models.NotPerformedReason;
using NotPerformedReasonEntity = Signify.Spirometry.Core.Data.Entities.NotPerformedReason;
using OccurrenceFrequency = Signify.Spirometry.Core.Models.OccurrenceFrequency;
using PdfDeliveredToClient = SpiroEvents.PdfDeliveredToClient;
using ResultsReceived = Signify.Spirometry.Core.Events.Akka.ResultsReceived;
using SessionGrade = Signify.Spirometry.Core.Models.SessionGrade;
using TrileanType = Signify.Spirometry.Core.Models.TrileanType;

namespace Signify.Spirometry.Core.Maps
{
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
                    mapper => mapper.MapFrom(source => SetToUtc(source.DateOfService)));

            CreateMap<EvalReceived, SpirometryExam>()
                .ForMember(
                    destination => destination.EvaluationReceivedDateTime,
                    mapper => mapper.MapFrom(source => source.ReceivedDateTime))
                .ForMember(
                    destination => destination.EvaluationCreatedDateTime,
                    mapper => mapper.MapFrom(source => source.CreatedDateTime.UtcDateTime))
                .ForMember(
                    destination => destination.CreatedDateTime,
                    mapper => mapper.MapFrom(source => source.ReceivedBySpirometryProcessManagerDateTime));

            CreateMap<RawExamResult, ExamResult>()
                .ConvertUsing<RawExamResultMapper>();

            CreateSpirometryExamResultMaps();

            CreateMap<SpirometryExam, ExamNotPerformed>();

            CreateMap<NotPerformedInfo, ExamNotPerformed>()
                .ForMember(
                    destination => destination.NotPerformedReasonId,
                    mapper => mapper.MapFrom(source => Convert(source.Reason)));

            CreateMap<MemberInfo, SpirometryExam>();

            CreateMap<PdfDeliveredToClient, Data.Entities.PdfDeliveredToClient>()
                .ForMember(
                    destination => destination.DeliveryDateTime,
                    mapper => mapper.MapFrom(source => SetToUtc(source.DeliveryDateTime)))
                .ForMember(
                    destination => destination.CreatedDateTime,
                    mapper => mapper.MapFrom(source => SetToUtc(source.CreatedDateTime)));

            CreateMap<Data.Entities.PdfDeliveredToClient, BillableEvent>()
                .ForMember(destination => destination.BillableDate,
                    mapper => mapper.MapFrom(source => source.DeliveryDateTime));

            CreateMap<BillableEvent, CreateBill>();

            CreateMap<SpirometryExam, CreateBillRequest>()
                .ConvertUsing<CreateBillRequestMapper>();
            CreateMap<CreateBill, CreateBillRequest>()
                .ConvertUsing<CreateBillRequestMapper>();

            CreateMap<ExamStatusEvent, ExamStatus>()
                .ForMember(destination => destination.StatusCodeId,
                    mapper => mapper.MapFrom(source => (int)source.StatusCode))
                .ForMember(destination => destination.StatusDateTime,
                    mapper => mapper.MapFrom(source => SetToUtc(source.StatusDateTime)))
                .ForMember(destination => destination.SpirometryExamId,
                    mapper => mapper.MapFrom(source => source.Exam.SpirometryExamId))
                .ForMember(destination => destination.StatusCode,
                    mapper => mapper.Ignore());

            CreateMap<SaveProviderPay, ExamStatusEvent>()
                .ForMember(dest => dest.StatusDateTime, mapper => mapper.MapFrom(src => src.ParentEventDateTime));

            CreateMap<SaveProviderPay, ProviderPay>()
                .ForMember(dest => dest.SpirometryExamId, mapper => mapper.MapFrom(src => src.ExamId));

            CreateMap<EvaluationProcessedEvent, EvaluationProcessedEventForPayment>();

            CreateMap<OverreadProcessedEvent, OverreadProcessedEventForPayment>();

            CreateMap<SpirometryExam, ProviderPayApiRequest>()
                .ForMember(dest => dest.PersonId, mapper =>
                    mapper.MapFrom(src => src.CenseoId))
                .ForMember(dest => dest.DateOfService, mapper =>
                    mapper.MapFrom(src => DateOnly.FromDateTime(src.DateOfService!.Value).ToString("o")));

            CreateMap<CdiEventForPayment, SaveProviderPay>()
                .ForMember(dest => dest.EventId, mapper => mapper.MapFrom(src => src.RequestId))
                .ForMember(dest => dest.ParentEventDateTime, mapper => mapper.MapFrom(src => src.DateTime))
                .ForMember(dest => dest.ParentEventReceivedDateTime, mapper => mapper.MapFrom(src => src.CreatedDateTime));

            CreateMap<CDIPassedEvent, CdiEventForPayment>()
                .ForMember(dest => dest.EventType, mapper => mapper.MapFrom(src => src.GetType().Name))
                .ForMember(dest => dest.DateTime, mapper => mapper.MapFrom(src => src.DateTime.ToUniversalTime()));

            CreateMap<CDIFailedEvent, CdiEventForPayment>()
                .ForMember(dest => dest.EventType, mapper => mapper.MapFrom(src => src.GetType().Name))
                .ForMember(dest => dest.DateTime, mapper => mapper.MapFrom(src => src.DateTime.ToUniversalTime()));

            CreateMap<CdiEventForPayment, CdiEventForPaymentReceived>()
                .ForMember(dest=>dest.CreatedDateTime, mapper=>mapper.MapFrom(src=>src.CreatedDateTime.UtcDateTime));

            CreateMap<CdiEventForPayment, ExamStatusEvent>()
                .ForMember(dest => dest.EventId, mapper => mapper.MapFrom(src => src.RequestId))
                .ForMember(dest => dest.StatusDateTime, mapper => mapper.MapFrom(src => src.DateTime.UtcDateTime))
                .ForMember(dest => dest.ParentCdiEvent, mapper => mapper.MapFrom(src => src.EventType))
                .ForMember(dest => dest.ParentEventReceivedDateTime, mapper => mapper.MapFrom(src => src.CreatedDateTime));

            CreateMap<SaveProviderPay, ExamStatusEvent>()
                .ForMember(dest => dest.StatusDateTime, mapper => mapper.MapFrom(src => src.ParentEventDateTime.UtcDateTime));
            
            CreateHoldMaps();
            CreateOverreadMaps();
            CreateFlagMaps();
            CreateStatusEventMaps();
            CreateKafkaResultEventMaps();
        }

        private void CreateOverreadMaps()
        {
            CreateMap<OverreadProcessed, OverreadResult>()
                .ForMember(destination => destination.ExternalId,
                    mapper => mapper.MapFrom(source => source.OverreadId ?? Guid.Empty))
                .ForMember(destination => destination.OverreadComment,
                    mapper => mapper.MapFrom(source => source.Comment))
                .ForMember(destination => destination.PerformedDateTime,
                    mapper => mapper.MapFrom(source => source.PerformedDateTime.UtcDateTime))
                .ForMember(destination => destination.OverreadDateTime,
                    mapper => mapper.MapFrom(source => source.OverreadDateTime.UtcDateTime))
                .ForMember(destination => destination.ReceivedDateTime,
                    mapper => mapper.MapFrom(source => source.ReceivedDateTime.UtcDateTime));
        }

        private void CreateFlagMaps()
        {
            CreateMap<SpirometryExam, SaveSystemFlagRequest>()
                .ConvertUsing<SaveSystemFlagRequestMapper>();
            CreateMap<SpirometryExamResult, SaveSystemFlagRequest>()
                .ConvertUsing<SaveSystemFlagRequestMapper>();
        }

        private void CreateStatusEventMaps()
        {
            CreateMap<SpirometryExam, Performed>()
              .ForMember(dest => dest.CreateDate, mapper => mapper.MapFrom(src => src.EvaluationCreatedDateTime))
              .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => src.EvaluationReceivedDateTime));

            CreateMap<SpirometryExam, NotPerformed>()
               .ForMember(dest => dest.CreateDate, mapper => mapper.MapFrom(src => src.EvaluationCreatedDateTime))
               .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => src.EvaluationReceivedDateTime));

            CreateMap<SpirometryExam, FlaggedForLoopback>()
                .ForMember(dest => dest.CreateDate, mapper => mapper.MapFrom(src => src.EvaluationCreatedDateTime))
                .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => src.EvaluationReceivedDateTime));

            CreateMap<ExamNotPerformed, NotPerformed>()
               .ForMember(dest => dest.Reason, mapper => mapper.MapFrom(src => src.NotPerformedReason.Reason))
               .ForMember(dest => dest.ReasonNotes, mapper => mapper.MapFrom(src => src.Notes));

            CreateMap<SpirometryExam, Events.Status.BillRequestSent>()
                .ForMember(dest => dest.CreateDate, mapper => mapper.MapFrom(src => src.EvaluationCreatedDateTime));

            CreateMap<Data.Entities.BillRequestSent, Events.Status.BillRequestSent>()
                .ForMember(dest => dest.BillId, mapper => mapper.MapFrom(src => src.BillId))
                .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => src.CreatedDateTime));

            CreateMap<Data.Entities.PdfDeliveredToClient, Events.Status.BillRequestSent>()
                .ForMember(dest => dest.PdfDeliveryDate, mapper => mapper.MapFrom(src => src.DeliveryDateTime));

            CreateMap<SpirometryExam, BillRequestNotSent>()
                .ForMember(dest => dest.CreateDate, mapper => mapper.MapFrom(src => src.EvaluationCreatedDateTime))
                .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => src.EvaluationReceivedDateTime));

            CreateMap<Data.Entities.PdfDeliveredToClient, BillRequestNotSent>()
                .ForMember(dest => dest.PdfDeliveryDate, mapper => mapper.MapFrom(src => src.DeliveryDateTime));

            CreateMap<SpirometryExam, Signify.Spirometry.Core.Events.Status.ResultsReceived>()
                .ForMember(dest => dest.CreateDate, mapper => mapper.MapFrom(src => src.EvaluationCreatedDateTime))
                .ForMember(dest => dest.ReceivedDate,
                    mapper => mapper.MapFrom(src =>
                        src.EvaluationReceivedDateTime)); // May later get overwritten with the overread received date if an overread was processed

            CreateMap<SpirometryExam, ProviderPayRequestSent>()
                .ForMember(dest => dest.CreateDate, mapper => mapper.Ignore());

            CreateMap<ExamStatusEvent, ProviderPayRequestSent>()
                .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => src.ParentEventReceivedDateTime))
                .ForMember(dest => dest.CreateDate, mapper => mapper.MapFrom(src => src.ParentEventReceivedDateTime))
                .ForMember(dest => dest.PaymentId, mapper => mapper.MapFrom(src => src.PaymentId))
                .ForMember(dest => dest.ParentEventDateTime, mapper => mapper.MapFrom(src => src.StatusDateTime));

            CreateMap<SpirometryExam, ProviderNonPayableEventReceived>()
                .ForMember(dest => dest.CreateDate, mapper => mapper.Ignore());
            
            CreateMap<ExamStatusEvent, ProviderNonPayableEventReceived>()
                .ForMember(dest => dest.CreateDate, mapper => mapper.MapFrom(src => src.ParentEventReceivedDateTime))
                .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => src.ParentEventReceivedDateTime));

            CreateMap<SpirometryExam, ProviderPayableEventReceived>()
                .ForMember(dest => dest.CreateDate, mapper => mapper.Ignore());
            
            CreateMap<ExamStatusEvent, ProviderPayableEventReceived>()
                .ForMember(dest => dest.CreateDate, mapper => mapper.MapFrom(src => src.ParentEventReceivedDateTime))
                .ForMember(dest => dest.ReceivedDate, mapper => mapper.MapFrom(src => src.ParentEventReceivedDateTime));
        }

        private void CreateKafkaResultEventMaps()
        {
            CreateMap<SpirometryExam, ResultsReceived>()
                .ConvertUsing<ResultsReceivedMapper>();
            CreateMap<SpirometryExamResult, ResultsReceived>()
                .ConvertUsing<ResultsReceivedMapper>();
        }

        private void CreateHoldMaps()
        {
            CreateMap<CDIEvaluationHeldEvent, Hold>()
                .ConvertUsing<HoldMapper>();
        }

        private void CreateSpirometryExamResultMaps()
        {
            CreateMap<ExamResult, SpirometryExamResult>()
                .ForMember(
                    destination => destination.SessionGradeId,
                    mapper => mapper.MapFrom(source => Convert(source.SessionGrade)))
                .ForMember(
                    destination => destination.SessionGrade,
                    mapper => mapper.Ignore())
                .ForMember(
                    destination => destination.HasHighSymptomTrileanTypeId,
                    mapper => mapper.MapFrom(source => Convert(source.HasHighSymptom)))
                .ForMember(
                    destination => destination.HasHighSymptomTrileanType,
                    mapper => mapper.Ignore())
                .ForMember(
                    destination => destination.HasEnvOrExpRiskTrileanTypeId,
                    mapper => mapper.MapFrom(source => Convert(source.HasEnvOrExpRisk)))
                .ForMember(
                    destination => destination.HasEnvOrExpRiskTrileanType,
                    mapper => mapper.Ignore())
                .ForMember(
                    destination => destination.HasHighComorbidityTrileanTypeId,
                    mapper => mapper.MapFrom(source => Convert(source.HasHighComorbidity)))
                .ForMember(
                    destination => destination.HasHighComorbidityTrileanType,
                    mapper => mapper.Ignore())
                .ForMember(
                    destination => destination.NormalityIndicatorId,
                    mapper => mapper.MapFrom(source => Convert(source.NormalityIndicator)))
                .ForMember(
                    destination => destination.NormalityIndicator,
                    mapper => mapper.Ignore())
                .ForMember(
                    destination => destination.FvcNormalityIndicatorId,
                    mapper => mapper.MapFrom(source => Convert(source.FvcNormalityIndicator)))
                .ForMember(
                    destination => destination.FvcNormalityIndicator,
                    mapper => mapper.Ignore())
                .ForMember(
                    destination => destination.Fev1NormalityIndicatorId,
                    mapper => mapper.MapFrom(source => Convert(source.Fev1NormalityIndicator)))
                .ForMember(
                    destination => destination.Fev1NormalityIndicator,
                    mapper => mapper.Ignore())
                .ForMember(
                    destination => destination.CoughMucusOccurrenceFrequencyId,
                    mapper => mapper.MapFrom(source => Convert(source.CoughMucusFrequency)))
                .ForMember(
                    destination => destination.CoughMucusOccurrenceFrequency,
                    mapper => mapper.Ignore())
                .ForMember(
                    destination => destination.HadWheezingPast12moTrileanTypeId,
                    mapper => mapper.MapFrom(source => Convert(source.HadWheezingPast12mo)))
                .ForMember(
                    destination => destination.HadWheezingPast12moTrileanType,
                    mapper => mapper.Ignore())
                .ForMember(
                    destination => destination.GetsShortnessOfBreathAtRestTrileanTypeId,
                    mapper => mapper.MapFrom(source => Convert(source.GetsShortnessOfBreathAtRest)))
                .ForMember(
                    destination => destination.GetsShortnessOfBreathAtRestTrileanType,
                    mapper => mapper.Ignore())
                .ForMember(
                    destination => destination.GetsShortnessOfBreathWithMildExertionTrileanTypeId,
                    mapper => mapper.MapFrom(source => Convert(source.GetsShortnessOfBreathWithMildExertion)))
                .ForMember(
                    destination => destination.GetsShortnessOfBreathWithMildExertionTrileanType,
                    mapper => mapper.Ignore())
                .ForMember(
                    destination => destination.NoisyChestOccurrenceFrequencyId,
                    mapper => mapper.MapFrom(source => Convert(source.NoisyChestFrequency)))
                .ForMember(
                    destination => destination.NoisyChestOccurrenceFrequency,
                    mapper => mapper.Ignore())
                .ForMember(
                    destination => destination.ShortnessOfBreathPhysicalActivityOccurrenceFrequencyId,
                    mapper => mapper.MapFrom(source => Convert(source.ShortnessOfBreathPhysicalActivityFrequency)))
                .ForMember(
                    destination => destination.ShortnessOfBreathPhysicalActivityOccurrenceFrequency,
                    mapper => mapper.Ignore())
                .ForMember(
                    destination => destination.LungFunctionScore,
                    mapper => mapper.MapFrom(source => source.LungFunctionQuestionnaireScore));
        }

        private static DateTime? SetToUtc(DateTime? source)
        {
            if (!source.HasValue)
                return null;

            switch (source.Value.Kind)
            {
                case DateTimeKind.Utc:
                    return source;
                case DateTimeKind.Unspecified:
                    return new DateTime(source.Value.Ticks, DateTimeKind.Utc);
                case DateTimeKind.Local:
                default:
                    return source.Value.ToUniversalTime(); // This method treats Unspecified as Local, which we don't want
            }
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
                NotPerformedReason.MemberPhysicallyUnable => NotPerformedReasonEntity.MemberPhysicallyUnable.NotPerformedReasonId,
                NotPerformedReason.MemberOutsideDemographicRanges => NotPerformedReasonEntity.MemberOutsideDemographicRanges.NotPerformedReasonId,
                _ => throw new NotImplementedException("Unhandled NotPerformedReason: " + reason)
            };
        }

        private static short Convert(NormalityIndicator indicator)
        {
            return indicator switch
            {
                NormalityIndicator.Undetermined => Data.Entities.NormalityIndicator.Undetermined.NormalityIndicatorId,
                NormalityIndicator.Normal => Data.Entities.NormalityIndicator.Normal.NormalityIndicatorId,
                NormalityIndicator.Abnormal => Data.Entities.NormalityIndicator.Abnormal.NormalityIndicatorId,
                _ => throw new NotImplementedException("Unhandled NormalityIndicator: " + indicator)
            };
        }

        private static short? Convert(SessionGrade? sessionGrade)
        {
            return sessionGrade switch
            {
                null => null,
                SessionGrade.A => Data.Entities.SessionGrade.A.SessionGradeId,
                SessionGrade.B => Data.Entities.SessionGrade.B.SessionGradeId,
                SessionGrade.C => Data.Entities.SessionGrade.C.SessionGradeId,
                SessionGrade.D => Data.Entities.SessionGrade.D.SessionGradeId,
                SessionGrade.E => Data.Entities.SessionGrade.E.SessionGradeId,
                SessionGrade.F => Data.Entities.SessionGrade.F.SessionGradeId,
                _ => throw new NotImplementedException("Unhandled SessionGrade: " + sessionGrade)
            };
        }

        private static short? Convert(TrileanType? trileanType)
        {
            return trileanType switch
            {
                null => null,
                TrileanType.Unknown => Data.Entities.TrileanType.Unknown.TrileanTypeId,
                TrileanType.Yes => Data.Entities.TrileanType.Yes.TrileanTypeId,
                TrileanType.No => Data.Entities.TrileanType.No.TrileanTypeId,
                _ => throw new NotImplementedException("Unhandled TrileanType: " + trileanType)
            };
        }

        private static short? Convert(OccurrenceFrequency? frequency)
        {
            return frequency switch
            {
                null => null,
                OccurrenceFrequency.Never => Data.Entities.OccurrenceFrequency.Never.OccurrenceFrequencyId,
                OccurrenceFrequency.Rarely => Data.Entities.OccurrenceFrequency.Rarely.OccurrenceFrequencyId,
                OccurrenceFrequency.Sometimes => Data.Entities.OccurrenceFrequency.Sometimes.OccurrenceFrequencyId,
                OccurrenceFrequency.Often => Data.Entities.OccurrenceFrequency.Often.OccurrenceFrequencyId,
                OccurrenceFrequency.VeryOften => Data.Entities.OccurrenceFrequency.VeryOften.OccurrenceFrequencyId,
                _ => throw new NotImplementedException("Unhandled OccurrenceFrequency: " + frequency)
            };
        }
    }
}
