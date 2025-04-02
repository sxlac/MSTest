using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.PAD.Svc.Core.Services;
using Signify.PAD.Svc.Core.Tests.Fakes.Infrastructure;
using System.IO.Abstractions;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Services;

public class DirectoryServicesTests
{
    private readonly FakeApplicationTime _applicationTime = new();
    private readonly IFile _file = A.Fake<IFile>();
    private readonly IFileSystem _fileSystem = A.Fake<IFileSystem>();

    public DirectoryServicesTests()
    {
        A.CallTo(() => _fileSystem.File)
            .Returns(_file);
    }

    private DirectoryServices CreateSubject()
        => new(A.Dummy<ILogger<DirectoryServices>>(), null, _fileSystem, _applicationTime);

    [Fact]
    public void GetCreationTimeUtc_Test()
    {
        // Arrange
        const string path = nameof(path);

        A.CallTo(() => _file.GetCreationTimeUtc(A<string>._))
            .Returns(_applicationTime.UtcNow());

        // Act
        var result = CreateSubject().GetCreationTimeUtc(path);

        // Assert
        A.CallTo(() => _file.GetCreationTimeUtc(A<string>.That.Matches(p => p == path)))
            .MustHaveHappened();

        Assert.Equal(_applicationTime.UtcNow(), result);
    }
}
