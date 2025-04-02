using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Messages.Commands;
using Signify.DEE.Svc.Core.Messages.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Messages.Commands;

public class UpdateExamImagesHandlerTest
{
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly DataContext _context = A.Fake<DataContext>();
    private UpdateExamImagesHandler _handler;
    private readonly ILogger<UpdateExamImagesHandler> _logger = A.Dummy<ILogger<UpdateExamImagesHandler>>();
#pragma warning disable CS0618 // Type or member is obsolete
    [Fact]
    public async Task Update_NonGraded_To_Graded()
    {
        //Arrange
        var imageId = Guid.NewGuid().ToString();
        //Graded image from IRIS
        var images = new List<ExamImageModel>();
        images.Add(new ExamImageModel() { ExamId = 1, Gradable = true, NotGradableReasons = new List<string>(), Laterality = "OD", ImageLocalId = imageId });

        //Generate input request
        var request = new UpdateExamImages() { Images = images };

        //Create db old record
        var examImage = new ExamImage
        {
            ExamId = 1,
            Gradable = false,
            NotGradableReasons = "blah",
            LateralityCodeId = 1,
            ImageLocalId = imageId,
        };
        var fakeDbSet = FakeDbSet(examImage);

        //Arrange call to handler
        _handler = new UpdateExamImagesHandler(_mapper, _context, _logger);

        //set DB old record entity
        A.CallTo(() => _context.ExamImages).Returns(fakeDbSet);

        //new record
        A.CallTo(() => _mapper.Map<ExamImage>(A<ExamImageModel>._)).Returns(new ExamImage
        {
            ExamId = 1,
            Gradable = true,
            NotGradableReasons = "",
            LateralityCode = new LateralityCode(1, "OD", "Right, Oculu"),
            ImageLocalId = imageId,
        });

        //Act
        var actual = await _handler.Handle(request, CancellationToken.None);

        //Assert
        actual.Should().Be(true);
        fakeDbSet.First().Gradable.Should().Be(true);
        fakeDbSet.First().NotGradableReasons.Should().Be(string.Empty);
    }

    [Fact]
    public async Task Should_Update_Laterality_Code()
    {
        //Arrange
        var imageId = Guid.NewGuid().ToString();

        //Graded image from IRIS
        var images = new List<ExamImageModel>();
        images.Add(new ExamImageModel() { ExamId = 22, Gradable = true, NotGradableReasons = new List<string>(), Laterality = "OD", ImageLocalId = imageId });

        //Generate input request
        var request = new UpdateExamImages() { Images = images };

        //Create db old record
        var examImage = new ExamImage
        {
            ExamId = 22,
            Gradable = false,
            NotGradableReasons = "blah",
            LateralityCodeId = 1,
            ImageLocalId = imageId,
        };
        var fakeDbSet = FakeDbSet(examImage);

        //Arrange call to handler
        _handler = new UpdateExamImagesHandler(_mapper, _context, _logger);

        //set DB old record entity
        A.CallTo(() => _context.ExamImages).Returns(fakeDbSet);

        //new record
        A.CallTo(() => _mapper.Map<ExamImage>(A<ExamImageModel>._)).Returns(new ExamImage
        {
            ExamId = 1,
            Gradable = true,
            NotGradableReasons = "",
            LateralityCode = new LateralityCode(1, "OD", "Right, Oculu"),
            ImageLocalId = imageId,
        });

        //Act
        var actual = await _handler.Handle(request, CancellationToken.None);

        //Assert
        actual.Should().Be(true);
        fakeDbSet.First().LateralityCodeId.Should().Be(LateralityCode.Right.LateralityCodeId);
    }

    [Fact]
    public async Task Should_Update_Laterality_Code_WhenServiceBusImages()
    {
        //Arrange
        var imageId = Guid.NewGuid().ToString();

        //Graded image from IRIS
        var images = new List<ExamImageModel>();
        images.Add(new ExamImageModel() { ExamId = 22, Gradable = true, NotGradableReasons = new List<string>(), ImageLocalId = imageId });

        //Generate input request
        var request = new UpdateExamImages() { Images = images };

        //Create db old record
        var examImage = new ExamImage
        {
            ExamId = 22,
            Gradable = false,
            NotGradableReasons = "blah",
            ImageLocalId = imageId
        };
        var fakeDbSet = FakeDbSet(examImage);

        //Arrange call to handler
        _handler = new UpdateExamImagesHandler(_mapper, _context, _logger);

        //set DB old record entity
        A.CallTo(() => _context.ExamImages).Returns(fakeDbSet);

        //new record
        A.CallTo(() => _mapper.Map<ExamImage>(A<ExamImageModel>._)).Returns(new ExamImage
        {
            ExamId = 1,
            Gradable = true,
            NotGradableReasons = "",
            LateralityCode = new LateralityCode(1, "OD", "Right, Oculu"),
            ImageLocalId = imageId
        });

        //Act
        var actual = await _handler.Handle(request, CancellationToken.None);

        //Assert
        actual.Should().Be(true);
        fakeDbSet.First().LateralityCodeId.Should().Be(LateralityCode.Right.LateralityCodeId);
    }
#pragma warning restore CS0618 // Type or member is obsolete

    private static DbSet<ExamImage> FakeDbSet(ExamImage examImages)
    {
        var fakeIQueryable = new List<ExamImage> { examImages }.AsQueryable();
        var fakeDbSet = A.Fake<DbSet<ExamImage>>((d => d.Implements(typeof(IQueryable<ExamImage>))));
        A.CallTo(() => ((IQueryable<ExamImage>)fakeDbSet).GetEnumerator()).Returns(fakeIQueryable.GetEnumerator());
        A.CallTo(() => ((IQueryable<ExamImage>)fakeDbSet).Provider).Returns(fakeIQueryable.Provider);
        A.CallTo(() => ((IQueryable<ExamImage>)fakeDbSet).Expression).Returns(fakeIQueryable.Expression);
        A.CallTo(() => ((IQueryable<ExamImage>)fakeDbSet).ElementType).Returns(fakeIQueryable.ElementType);
        return fakeDbSet;
    }
}