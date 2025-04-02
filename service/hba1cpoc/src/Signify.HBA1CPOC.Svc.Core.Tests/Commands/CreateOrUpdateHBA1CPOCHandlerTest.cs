using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.HBA1CPOC.Svc.Core.Commands;
using Signify.HBA1CPOC.Svc.Core.Tests.Mocks.StaticEntity;
using Signify.HBA1CPOC.Svc.Core.Tests.Utilities;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.Commands;

public class CreateOrUpdateHBA1CPOCHandlerTest : IClassFixture<MockDbFixture>
{
    private readonly IMapper _mapper;
    private readonly CreateOrUpdateHBA1CPOCHandler _subject;
    private readonly MockDbFixture _mockDbFixture;

    public CreateOrUpdateHBA1CPOCHandlerTest(MockDbFixture mockDbFixture)
    {
        _mapper = A.Fake<IMapper>();
        _mockDbFixture = mockDbFixture;
        _subject = new CreateOrUpdateHBA1CPOCHandler(mockDbFixture.Context, _mapper, A.Dummy<ILogger<CreateOrUpdateHBA1CPOCHandler>>());
    }

    [Fact]
    public async Task Should_Create_HBA1CPOC_DataCheck()
    {
        A.CallTo(() => _mapper.Map<Core.Data.Entities.HBA1CPOC>(A<CreateOrUpdateHBA1CPOC>._)).Returns(StaticMockEntities.CreateHba1Cpoc);
        var result = await _subject.Handle(StaticMockEntities.CreateOrUpdateHba1Cpoc, CancellationToken.None);
        _mockDbFixture.Context.HBA1CPOC.Any(x => x.AppointmentId == result.AppointmentId).Should().BeTrue();
    }

    [Fact]
    public async Task Should_Create_HBA1CPOC_CountTest()
    {
        A.CallTo(() => _mapper.Map<Core.Data.Entities.HBA1CPOC>(A<CreateOrUpdateHBA1CPOC>._)).Returns(StaticMockEntities.CreateHba1Cpoc);
        var initialCount = _mockDbFixture.Context.HBA1CPOC.Count();
        await _subject.Handle(StaticMockEntities.CreateOrUpdateHba1Cpoc, CancellationToken.None);
        _mockDbFixture.Context.HBA1CPOC.Count().Should().BeGreaterThan(initialCount, "There should be an insert");
    }

    [Fact]
    public async Task Should_Create_HBA1CPOC()
    {
        A.CallTo(() => _mapper.Map<Core.Data.Entities.HBA1CPOC>(A<CreateOrUpdateHBA1CPOC>._)).Returns(StaticMockEntities.CreateHba1Cpoc);
        var initialCount = _mockDbFixture.Context.HBA1CPOC.Count();
        var result = await _subject.Handle(StaticMockEntities.CreateOrUpdateHba1Cpoc, CancellationToken.None);
        result.HBA1CPOCId.Should().BeGreaterThan(initialCount);
    }

    [Fact]
    public async Task Should_Update_HBA1CPOC_DataCheck()
    {
        //Arrange
        var exam = StaticMockEntities.CreateHba1Cpoc;
        exam.HBA1CPOCId = 1;
        exam.FirstName = "UpdateExamTest";
        A.CallTo(() => _mapper.Map<Core.Data.Entities.HBA1CPOC>(A<CreateOrUpdateHBA1CPOC>._)).Returns(exam);
        var createOrUpdateExam = StaticMockEntities.CreateOrUpdateHba1Cpoc;
        createOrUpdateExam.HBA1CPOCId = 1;
        createOrUpdateExam.FirstName = "UpdateExamTest";
        _mockDbFixture.Context.ChangeTracker.Clear();

        //Act
        var result = await _subject.Handle(createOrUpdateExam, CancellationToken.None);

        //Assert
        _mockDbFixture.Context.HBA1CPOC.Should().Contain(result);
        _mockDbFixture.Context.HBA1CPOC.AsNoTracking().Any(e => e.FirstName == "UpdateExamTest").Should().Be(true);
    }
}