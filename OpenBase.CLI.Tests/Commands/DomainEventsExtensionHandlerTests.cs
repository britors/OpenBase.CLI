using OpenBase.CLI.Commands.Extension;
using OpenBase.CLI.Commands.Extension.DomainEvents;
using OpenBase.CLI.Helpers.Execution;
using OpenBase.CLI.Helpers.IO;
using OpenBase.CLI.Localization;

namespace OpenBase.CLI.Tests.Commands;

public class DomainEventsExtensionHandlerTests
{
    private readonly Mock<IAnsiConsole> _console = new();
    private readonly Mock<IFileWriter> _fileWriter = new();

    public DomainEventsExtensionHandlerTests()
    {
        SR.Configure();
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);
        _fileWriter.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(string.Empty);
    }

    private DomainEventsExtensionHandler CreateHandler() =>
        new(_console.Object, _fileWriter.Object);

    private static ExtensionContext BuildContext(string? solutionDir = "/solution", string? ns = "MyApp") =>
        new(null, solutionDir ?? "/solution", null, [], solutionDir, ns);

    [Fact]
    public void Apply_ValidContext_ReturnsSuccess()
    {
        var result = CreateHandler().Apply(BuildContext());
        Assert.True(result.Success);
    }

    [Fact]
    public void GetFiles_ReturnsThreeFiles()
    {
        var files = DomainEventsExtensionHandler.GetFiles("MyApp",
            "/solution/src/MyApp.Application",
            "/solution/src/MyApp.Infra.Data").ToList();

        Assert.Equal(3, files.Count);
    }

    [Fact]
    public void GetFiles_ContainsExpectedFiles()
    {
        var files = DomainEventsExtensionHandler.GetFiles("MyApp",
            "/solution/src/MyApp.Application",
            "/solution/src/MyApp.Infra.Data").ToList();

        Assert.Contains(files, f => f.Path.EndsWith("IDomainEvent.cs"));
        Assert.Contains(files, f => f.Path.EndsWith("IDomainEventHandler.cs"));
        Assert.Contains(files, f => f.Path.EndsWith("AggregateRoot.cs"));
    }

    [Fact]
    public void Apply_InjectsSaveChangesAsyncOverride()
    {
        var dbContextPath = Path.Combine("/solution/src/MyApp.Infra.Data", "OneBaseDataBaseContext.cs").Replace("\\", "/");
        _fileWriter.Setup(f => f.FileExists(dbContextPath)).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(dbContextPath)).Returns("public class OneBaseDataBaseContext { }");

        CreateHandler().Apply(BuildContext("/solution", "MyApp"));

        _fileWriter.Verify(f => f.WriteAllText(
            dbContextPath,
            It.Is<string>(c => c.Contains("public override async Task<int> SaveChangesAsync"))), Times.Once);
    }
}
