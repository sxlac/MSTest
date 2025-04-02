using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Signify.PAD.Messages.Events;
using Signify.PAD.Svc.Core.BusinessRules;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Events;
using Signify.PAD.Svc.Core.Events.Status;
using Signify.PAD.Svc.Core.Maps;
using Signify.PAD.Svc.Core.Models;
using System;
using System.Collections.Generic;
using Xunit;
using static Signify.PAD.Svc.Core.Constants.Application;
using Pad = Signify.PAD.Svc.Core.Data.Entities.PAD;

namespace Signify.PAD.Svc.Core.Tests.Maps;

public class MappingProfileTests
{
    private static IMapper CreateSubject()
    {
        var services = new ServiceCollection().AddSingleton<IBillableRules>(new BillAndPayRules());
        return new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
            cfg.ConstructServicesUsing(
                type => ActivatorUtilities.CreateInstance(services.BuildServiceProvider(), type));
        }).CreateMapper();
    }

    [Fact]
    public void Should_Map_PADPerformed_From_PAD()
    {
        var pad = new Core.Data.Entities.PAD();

        var mapper = CreateSubject();

        //Action
        var padPerformed = mapper.Map<Core.Data.Entities.PAD, PADPerformed>(pad);

        //Assert
        padPerformed.CorrelationId.Should().NotBeEmpty();
    }

    [Theory]
    [MemberData(nameof(Map_FromEvaluationAnswers_ToResultsReceived_Tests_Data))]
    public void Map_FromEvaluationAnswers_ToResultsReceived_Tests(EvaluationAnswers answers, ResultsReceived expected)
    {
        var subject = CreateSubject();

        var actual = subject.Map<ResultsReceived>(answers);

        Assert.Equal(expected, actual);
    }

    public static IEnumerable<object[]> Map_FromEvaluationAnswers_ToResultsReceived_Tests_Data()
    {
        // Yes, this results in these test cases being covered twice, but ensures our MappingProfile uses our custom converter
        return ResultsReceivedConverterTests.Convert_Tests_Data();
    }

    [Fact]
    public void Map_FromExamStatusEvent_ToPadStatus_Test()
    {
        var source = new ExamStatusEventNew
        {
            Exam = new Core.Data.Entities.PAD
            {
                PADId = 1
            },
            StatusCode = StatusCodes.BillRequestNotSent,
            StatusDateTime = DateTime.UtcNow
        };

        var subject = CreateSubject();

        var actual = subject.Map<PADStatus>(source);

        Assert.Equal(1, actual.PADId);
        Assert.Equal(PADStatusCode.BillRequestNotSent.PADStatusCodeId, actual.PADStatusCodeId);
        Assert.Equal(source.StatusDateTime, actual.CreatedDateTime);

        Assert.Null(actual.PAD);
        Assert.Null(actual.PADStatusCode);
    }

    [Fact]
    public void Map_ToBillRequestNotSent_Test()
    {
        // Arrange
        var padSource = new Pad
        {
            EvaluationId = 1,
            MemberPlanId = 2,
            ProviderId = 3,
            CreatedDateTime = DateTimeOffset.UtcNow,
            ReceivedDateTime = DateTime.UtcNow
        };

        var pdfSource = new PDFToClient
        {
            EvaluationId = 1,
            DeliveryDateTime = DateTime.UtcNow
        };

        // Act
        var subject = CreateSubject();

        var actual = subject.Map<BillRequestNotSent>(padSource);
        subject.Map(pdfSource, actual);

        // Assert
        Assert.Equal(1, actual.EvaluationId);
        Assert.Equal(2, actual.MemberPlanId);
        Assert.Equal(3, actual.ProviderId);

        Assert.Equal(padSource.CreatedDateTime, actual.CreateDate);
        Assert.Equal(padSource.ReceivedDateTime, actual.ReceivedDate);
        Assert.Equal(pdfSource.DeliveryDateTime, actual.PdfDeliveryDate);
    }

    [Fact]
    public void Map_FromAoeSymptomAnswers_ToAoeSymptomSupportResultTest()
    {
        // Arrange
        var source = new AoeSymptomAnswers
        {
            LateralityCodeId = 2,
            PedalPulseCodeId = 3,
            FootPainDisappearsWalkingOrDangling = false,
            FootPainDisappearsOtc = true,
            AoeWithRestingLegPainConfirmed = true,
            HasClinicalSupportForAoeWithRestingLegPain = true,
            HasSymptomsForAoeWithRestingLegPain = true
        };

        // Act
        var subject = CreateSubject();

        var actual = subject.Map<AoeSymptomSupportResult>(source);

        // Assert
        Assert.Equal(2, actual.FootPainRestingElevatedLateralityCodeId);
        Assert.Equal(3, actual.PedalPulseCodeId);
        Assert.False(actual.FootPainDisappearsWalkingOrDangling);
        Assert.True(actual.FootPainDisappearsOtc);
        Assert.True(actual.AoeWithRestingLegPainConfirmed);
        Assert.True(actual.HasClinicalSupportForAoeWithRestingLegPain);
        Assert.True(actual.HasSymptomsForAoeWithRestingLegPain);
    }

    [Fact]
    public void Map_FromEvalReceived_ToAoeResultTest()
    {
        // Arrange
        var source = new EvalReceived
        {
            EvaluationId = 1234,
            ReceivedDateTime = DateTime.UtcNow
        };

        // Act
        var subject = CreateSubject();

        var actual = subject.Map<AoeResult>(source);

        // Assert
        Assert.Equal(1234, actual.EvaluationId);
        Assert.Equal(source.ReceivedDateTime, actual.ReceivedDate);
    }

    [Fact]
    public void Map_FromAoeSymptomAnswers_ToAoeResultTest()
    {
        // Arrange
        var source = new AoeSymptomAnswers
        {
            LateralityCodeId = 2,
            PedalPulseCodeId = 2,
            FootPainDisappearsWalkingOrDangling = true,
            FootPainDisappearsOtc = true,
            HasSymptomsForAoeWithRestingLegPain = true,
            HasClinicalSupportForAoeWithRestingLegPain = true,
            AoeWithRestingLegPainConfirmed = true,
            ReasonAoeWithRestingLegPainNotConfirmed = "Reason Example"
        };

        // Act
        var subject = CreateSubject();

        var actual = subject.Map<AoeResult>(source);

        // Assert
        Assert.Equal("Left", actual.ClinicalSupport.Find(x => x.SupportType == ClinicalSupportType.PainInLegs).SupportValue);
        Assert.Equal("Abnormal-Left", actual.ClinicalSupport.Find(x => x.SupportType == ClinicalSupportType.PedalPulseCode).SupportValue);
        Assert.Equal("true", actual.ClinicalSupport.Find(x => x.SupportType == ClinicalSupportType.FootPainDisappearsWalkingOrDangling).SupportValue);
        Assert.Equal("true", actual.ClinicalSupport.Find(x => x.SupportType == ClinicalSupportType.FootPainDisappearsWithMeds).SupportValue);
        Assert.Equal("true", actual.ClinicalSupport.Find(x => x.SupportType == ClinicalSupportType.HasSymptomsForAoeWithRestingLegPain).SupportValue);
        Assert.Equal("true", actual.ClinicalSupport.Find(x => x.SupportType == ClinicalSupportType.HasClinicalSupportForAoeWithRestingLegPain).SupportValue);
        Assert.Equal("true", actual.ClinicalSupport.Find(x => x.SupportType == ClinicalSupportType.AoeWithRestingLegPainConfirmed).SupportValue);
        Assert.Equal("Reason Example", actual.ClinicalSupport.Find(x => x.SupportType == ClinicalSupportType.ReasonAoeWithRestingLegPainNotConfirmed).SupportValue);
    }

    [Theory]
    [InlineData(1, "Normal")]
    [InlineData(2, "Abnormal-Left")]
    [InlineData(3, "Abnormal-Right")]
    [InlineData(4, "Abnormal-Bilateral")]
    [InlineData(5, "Not Performed")]
    public void Map_FromAoeSymptomAnswersToAoeResult_MapAllPedalPulseCodesSuccessfully(int pedalPulseCodeId, string pedalPulseCodeValue)
    {
        // Arrange
        var source = new AoeSymptomAnswers
        {
            LateralityCodeId = 2,
            PedalPulseCodeId = pedalPulseCodeId,
            FootPainDisappearsWalkingOrDangling = true,
            FootPainDisappearsOtc = true,
            HasSymptomsForAoeWithRestingLegPain = true,
            HasClinicalSupportForAoeWithRestingLegPain = true,
            AoeWithRestingLegPainConfirmed = true,
            ReasonAoeWithRestingLegPainNotConfirmed = "Reason Example"
        };

        // Act
        var subject = CreateSubject();

        var actual = subject.Map<AoeResult>(source);

        // Assert
        Assert.Equal(pedalPulseCodeValue, actual.ClinicalSupport.Find(x => x.SupportType == ClinicalSupportType.PedalPulseCode).SupportValue);
    }
}