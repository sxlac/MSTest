using AutoMapper;
using FluentAssertions;
using Signify.CKD.Messages.Events;
using Signify.CKD.Svc.Core.ApiClient.Requests;
using Signify.CKD.Svc.Core.Commands;
using Signify.CKD.Svc.Core.Data.Entities;
using Signify.CKD.Svc.Core.Maps;
using Signify.CKD.Svc.Core.Models;
using Signify.CKD.Svc.Core.Sagas.Commands;
using System;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Signify.CKD.Svc.Core.BusinessRules;
using Xunit;

namespace Signify.CKD.Svc.Core.Tests.Maps;

public class MappingProfileTests
{
    //Note: This test will throw error until all the concerned entities have their mappings resolved  
    //[Fact]
    //public void Mapper_Configs_Should_Be_Valid_Strict()
    //{
    //    var config = new MapperConfiguration(cfg =>
    //    {
    //        cfg.AddProfile<MappingProfile>();
    //        //cfg.ValidateInlineMaps = false;
    //    });

    //    config.AssertConfigurationIsValid();
    //}
    
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
    public void Should_Map_CKDPerformed_From_CKD()
    {
        var ckd = new Core.Data.Entities.CKD();

        var mapper = CreateSubject();

        //Action
        var ckdPerformed = mapper.Map<Core.Data.Entities.CKD, CKDPerformed>(ckd);

        //Assert
        ckdPerformed.CorrelationId.Should().NotBeEmpty();
    }

    [Fact]
    public void Should_Map_UpdateInventory()
    {
        var updateInv = new UpdateInventory();

        var mapper = CreateSubject();

        //Action
        var req = mapper.Map<UpdateInventory, UpdateInventoryRequest>(updateInv);

        //Assert
        req.RequestId.Should().Be(updateInv.CorrelationId);
        req.DateUpdated.Should().Be(updateInv.ReceivedDateTime);
    }

    [Fact]
    public void Map_From_Entity_To_ExamNotPerformed_Tests()
    {
        var entity = new Core.Data.Entities.CKD
        {
            CKDId = 1
        };

        var subject = CreateSubject();

        var actual = subject.Map<ExamNotPerformed>(entity);

        Assert.Equal(entity.CKDId, actual.CKDId);
        Assert.Equal(DateTime.Now.Date, actual.CreatedDateTime.Date);
    }

    [Fact]
    public void Map_From_Evaluations_To_CreateOrUpdateCkd_Tests()
    {
        //Arrange
        var evaluationAnswer = new EvaluationAnswers
        {
            LookupCKDAnswerEntity = new Core.Data.Entities.LookupCKDAnswer
            {
                CKDAnswerValue = "Albumin: 10 - Creatinine: 2.0 ; Normal"
            }
        };
        var subject = CreateSubject();

        //Act
        var actual = subject.Map<CreateOrUpdateCKD>(evaluationAnswer);

        //Assert
        Assert.Equal(evaluationAnswer.LookupCKDAnswerEntity.CKDAnswerValue, actual.CKDAnswer);
    }

    [Fact]
    public void Map_From_Evaluations_To_CreateOrUpdateCkd_Tests_With_Null_Ckd_Answer()
    {
        //Arrange
        var evaluationAnswer = new EvaluationAnswers();
        var subject = CreateSubject();

        //Act
        var actual = subject.Map<CreateOrUpdateCKD>(evaluationAnswer);

        //Assert
        Assert.Null(actual.CKDAnswer);
    }

    [Fact]
    public void MapFromEvaluationAnswerToResultHasNormalNormality()
    {
        //Arrange
        var result = new Messages.Result();

        var evaluationAnswer = new EvaluationAnswers()
        {
            LookupCKDAnswerEntity = new LookupCKDAnswer()
            {
                Acr = "3",
                Albumin = 10,
                CKDAnswerId = 20978,
                CKDAnswerValue = "Albumin: 10 - Creatinine: 3.0 ; Normal",
                Creatinine = 3,
                NormalityIndicator = "N",
                Severity = ""
            }
        };
        var subject = CreateSubject();

        //Act
        var actual = subject.Map(evaluationAnswer.LookupCKDAnswerEntity, result);

        //Assert
        Assert.Equal("N", actual.Determination);
    }
    
    [Fact]
    public void Should_Map_ProviderPayRequest_From_Ckd()
    {
        //Arrange
        var dateOfServiceDateTime = DateTime.Today;
        var providerPayRequest = new ProviderPayRequest();

        var ckd = A.Fake<Core.Data.Entities.CKD>();
        ckd.CenseoId = "12345";
        ckd.DateOfService=dateOfServiceDateTime;
        
        var subject = CreateSubject();

        //Act
        var actual = subject.Map(ckd, providerPayRequest);

        //Assert
        Assert.Equal(actual.PersonId,ckd.CenseoId);
        Assert.Equal(actual.DateOfService, dateOfServiceDateTime.ToString("yyyy-MM-dd"));
    }
}