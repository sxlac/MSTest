using AutoMapper;
using Iris.Public.Types.Models.Public._2._3._1;
using Microsoft.Extensions.DependencyInjection;
using Signify.DEE.Svc.Core.Commands;
using Signify.DEE.Svc.Core.Configs;
using Signify.DEE.Svc.Core.Maps;
using Signify.DEE.Svc.Core.Messages.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Maps;

public sealed class IrisImageUploadMapperTests : IDisposable, IAsyncDisposable
{
    private readonly IMapper _mapper;
    private readonly ServiceProvider _serviceProvider;

    public IrisImageUploadMapperTests()
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
        return new IrisImageUploadMapper(_serviceProvider.GetService<IrisConfig>());
    }

    [Fact]
    public void WhenNormalSource_MapsNecessaryIrisValues()
    {
        var now = DateTime.UtcNow;
        var guid = Guid.NewGuid().ToString();
        var destination = new ImageRequest()
        {
            ClientGuid = "clientGuid",
            OrderLocalId = guid,
            Image = new Iris.Public.Types.Models.RequestImage()
            {
                Taken = now
            },
            ItemNumberInCollection = 2,
            ImageEncoding = Iris.Public.Types.Enums.ImageEncoding.PNG,
            Site = new Iris.Public.Types.Models.RequestSite()
            {
                LocalId = "55"
            }
        };

        var source = BuildSource(now, guid);

        var actual = _mapper.Map<ImageRequest>(source);

        Assert.Equal(destination.ClientGuid, actual.ClientGuid);
        Assert.Equal(destination.OrderLocalId, actual.OrderLocalId);
        Assert.Equal(destination.Image.Taken, actual.Image.Taken);
        Assert.Equal(2, actual.ItemNumberInCollection);
        Assert.Equal(destination.ImageEncoding, actual.ImageEncoding);
        Assert.Equal(destination.Site.LocalId, actual.Site.LocalId);
        Assert.Equal(Iris.Public.Types.Enums.ImageContext.Primary, actual.Image.ImageContext);
        Assert.Equal(Iris.Public.Types.Enums.ImageClass.Fundus, actual.ImageClass);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _serviceProvider.DisposeAsync();
    }

    internal static UploadIrisImages BuildSource(DateTime dateTime, string guid)
        => new UploadIrisImages
        {
            Exam = new ExamModel()
            {
                ExamLocalId = guid,
                State = "MD",
                DateOfService = dateTime
            },
            ExamAnswers = new ExamAnswersModel()
            {
                ProviderEmail = "provider@email.com",
                MemberPlanId = 5093823,
                MemberFirstName = "FirstName",
                MemberLastName = "LastName",
                MemberGender = "M",
                MemberBirthDate = dateTime,
                Images = new List<string>() { "Image1", "Image2" }
            },
            ImageIdToRawImageMap = new Dictionary<string, string>()
        };
}