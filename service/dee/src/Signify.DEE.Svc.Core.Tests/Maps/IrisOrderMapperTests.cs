using AutoMapper;
using Iris.Public.Types.Enums;
using Iris.Public.Types.Models;
using Iris.Public.Types.Models.V2_3_1;
using Microsoft.Extensions.DependencyInjection;
using Signify.DEE.Svc.Core.Commands;
using Signify.DEE.Svc.Core.Configs;
using Signify.DEE.Svc.Core.Constants;
using Signify.DEE.Svc.Core.Maps;
using Signify.DEE.Svc.Core.Messages.Models;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Maps;

public sealed class IrisOrderMapperTests : IDisposable, IAsyncDisposable
{
    private readonly IMapper _mapper;
    private readonly ServiceProvider _serviceProvider;

    public IrisOrderMapperTests()
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
        return new IrisOrderMapper(_serviceProvider.GetService<IrisConfig>());
    }

    [Fact]
    public void WhenNormalSource_MapsNecessaryIrisValues()
    {
        var now = DateTime.UtcNow;
        var guid = Guid.NewGuid().ToString();
        var irisNamePoco = new PersonName() { First = "Super", Last = "Man" };
        var destination = new OrderRequest()
        {
            OrderControlCode = OrderControlCode.NW,
            ClientGuid = "clientGuid",
            CameraOperator = new RequestProvider()
            {
                Email = "test@test.com",
                NPI = "123456",
                Name = irisNamePoco
            },
            Order = new RequestOrder()
            {
                EvaluationTypes = new EvaluationType[] { EvaluationType.DR_AMD },
                ScheduledTime = now,
                CreatedTime = now,
                LocalId = guid,
                State = "MD"
            },
            Patient = new RequestPatient()
            {
                LocalId = "5093823",
                Name = new PersonName()
                {
                    First = "FirstName",
                    Last = "LastName",
                },
                Dob = now.ToString(),
                Gender = Gender.M
            },
            Site = new RequestSite()
            {
                LocalId = "55"
            }
        };

        var source = BuildSource(now, guid);
        var actual = _mapper.Map<OrderRequest>(source);

        Assert.Equal(destination.OrderControlCode, actual.OrderControlCode);
        Assert.Equal(destination.ClientGuid, actual.ClientGuid);
        Assert.Equal(destination.CameraOperator.Name.First, actual.CameraOperator.Name.First);
        Assert.Equal(destination.CameraOperator.Name.Last, actual.CameraOperator.Name.Last);
        Assert.Equal(destination.CameraOperator.NPI, actual.CameraOperator.NPI);
        Assert.Equal(destination.CameraOperator.Email, actual.CameraOperator.Email);

        Assert.Equal(destination.Order.EvaluationTypes, actual.Order.EvaluationTypes);
        Assert.Equal(destination.Order.ScheduledTime, actual.Order.ScheduledTime);
        Assert.Equal(destination.Order.LocalId, actual.Order.LocalId);
        Assert.Equal(destination.Order.State, actual.Order.State);

        Assert.Equal(destination.Patient.LocalId, actual.Patient.LocalId);
        Assert.Equal(destination.Patient.Name.First, actual.Patient.Name.First);
        Assert.Equal(destination.Patient.Name.Last, actual.Patient.Name.Last);
        Assert.Equal(destination.Patient.Dob, actual.Patient.Dob);

        Assert.Equal(destination.Site.LocalId, actual.Site.LocalId);
    }

    [Fact]
    public void WhenHasEnucleationIsNull_ShouldSetSingleEyeOnlyFalse()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var guid = Guid.NewGuid().ToString();
        var source = BuildSource(now, guid);
        source.Exam.HasEnucleation = null;

        // Act
        var actual = _mapper.Map<OrderRequest>(source);

        // Assert
        Assert.False(actual.Order.SingleEyeOnly);
    }

    [Fact]
    public void WhenHasEnucleationIsTrue_ShouldSetSingleEyeOnlyTrue()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var guid = Guid.NewGuid().ToString();
        var source = BuildSource(now, guid);
        source.Exam.HasEnucleation = true;

        // Act
        var actual = _mapper.Map<OrderRequest>(source);

        // Assert
        Assert.True(actual.Order.SingleEyeOnly);
        Assert.Equal(ApplicationConstants.Enucleation, actual.Order.MissingEyeReason);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _serviceProvider.DisposeAsync();
    }

    internal static CreateIrisOrder BuildSource(DateTime dateTime, string guid)
        => new CreateIrisOrder
        {
            Exam = new ExamModel()
            {
                ExamLocalId = guid,
                State = "MD",
                DateOfService = dateTime
            },
            ExamAnswers = new ExamAnswersModel()
            {
                ProviderFirstName = "Super",
                ProviderLastName = "Man",
                ProviderEmail = "test@test.com",
                ProviderNpi = "123456",
                MemberPlanId = 5093823,
                MemberFirstName = "FirstName",
                MemberLastName = "LastName",
                MemberGender = "M",
                MemberBirthDate = dateTime
            }
        };

}