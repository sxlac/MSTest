using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Signify.DEE.Svc.Core.Configs;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Maps;
using Signify.DEE.Svc.Core.Messages.Models;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Maps;

public sealed class ExamResultMapperTests: IDisposable, IAsyncDisposable
{
    private readonly IMapper _mapper;
    private readonly ServiceProvider _serviceProvider;
    private readonly FakeApplicationTime _applicationTime = new();

    public ExamResultMapperTests()
    {
        _serviceProvider = new ServiceCollection()
            .AddSingleton(new IrisConfig() { ClientGuid = "clientGuid", SiteLocalId = "55" })
            .BuildServiceProvider();

        _mapper = CreateSubject();
    }

    private IMapper CreateSubject()
    {
        // Cannot unit test the ResultsReceivedMapper directly in this case, since it needs the ResolutionContext,
        // which is not an interface so it cannot be mocked. Need to test using mapper.Map() instead.
        // https://github.com/AutoMapper/AutoMapper/discussions/3726

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(
                new MappingProfile());
            cfg.ConstructServicesUsing(ResolveService);
        });
        return config.CreateMapper();
    }

    private object ResolveService(Type type)
    {
        return new ExamResultMapper();
    }
        
    [Fact]
    public void Should_Map_ExamResultModel_To_ExamResult()
    {
        //Arrange
        var leftEyeFindings = new List<string> { "Macular Edema - None", "Diabetic Retinopathy - Mild" };
        var rightEyeFindings = new List<string> { "Macular Edema - None", "Diabetic Retinopathy - None", "Other - Cataract" };
        var diagnosis = new List<string> { "E118234", "E118235" };
        var source = new ExamResultModel
        {
            CarePlan = "Humana",
            DateSigned = _applicationTime.UtcNow(),
            ExamId = 1,
            ExamResultId = 1,
            GradableImage = true,
            PatientId = 1,
            LeftEyeFindings = leftEyeFindings,
            RightEyeFindings = rightEyeFindings,
            Diagnoses = diagnosis,
            LeftEyeHasPathology = true,
            RightEyeHasPathology = false,
            Grader = new ExamGraderModel()
        };

        //Act
        var target = _mapper.Map<ExamResultModel, ExamResult>(source);

        //Assert
        target.ExamId.Should().Be(1);
        target.ExamFindings.Should().NotBeNull();
        target.ExamFindings.First(find => find.Finding == "Diabetic Retinopathy - None").NormalityIndicator.Should().Be("N");
        target.ExamFindings.First(find => find.Finding == "Macular Edema - None").NormalityIndicator.Should().Be("N");
        target.ExamFindings.First(find => find.Finding == "Diabetic Retinopathy - Mild").NormalityIndicator.Should().Be("A");
        target.ExamFindings.First(find => find.Finding == "Other - Cataract").NormalityIndicator.Should().Be("A");
        target.ExamDiagnoses.ElementAt(0).Diagnosis.Should().Be("E118234");
        target.ExamDiagnoses.ElementAt(1).Diagnosis.Should().Be("E118235");
        target.LeftEyeHasPathology.Should().BeTrue();
        target.RightEyeHasPathology.Should().BeFalse();
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _serviceProvider.DisposeAsync();
    }

    [Theory]
    [InlineData("Diabetic Retinopathy - None", "N")]
    [InlineData("Diabetic Retinopathy - Indeterminable", "U")]
    [InlineData("Macular Edema - None", "N")]
    [InlineData("Other", "A")]
    [InlineData("Some unexpected finding", "U")]
    [InlineData("Some unexpected finding - None", "U")]
    [InlineData("", "U")]
    [InlineData("Other - Suspected Cataract", "A")]
    [InlineData("Wet AMD - No Observable", "N")]
    [InlineData("Wet AMD - Indeterminable", "U")]
    [InlineData("Wet AMD - Positive", "A")]
    [InlineData("Dry AMD - No Observable", "N")]
    [InlineData("Dry AMD - Indeterminable", "U")]
    [InlineData("Dry AMD - Early Stage", "A")]
    [InlineData("Dry AMD - Intermediate Stage", "A")]
    [InlineData("Dry AMD - Adv. Atrophic w/ Subfoveal Involvement", "A")]
    [InlineData("Dry AMD - Adv. Atrophic w/o Subfoveal Involvement", "A")]
    [InlineData("Other: Suspected Dry AMD", "A")]
    [InlineData("Other - Suspected Dry AMD", "A")]
    [InlineData("Other: Suspected Wet AMD", "A")]
    [InlineData("Other - Suspected Wet AMD", "A")]
    public void Finding_Mapper_Check_Scenarios(string finding, string expectedNormalityIndicator)
    {
        //Act
        var actual = ExamResultMapper.GetNormalityIndicator(finding);
        //Assert
        actual.Should().Be(expectedNormalityIndicator);
    }
}