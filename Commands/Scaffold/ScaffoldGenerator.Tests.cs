namespace OpenBase.CLI.Commands.Scaffold;

public sealed partial class ScaffoldGenerator
{
    private IEnumerable<(string, string)> TestFiles()
    {
        var domainTests = Path.Combine(ctx.TestsPath, "Domain", Services, ctx.Entity);
        var featTests   = Path.Combine(ctx.TestsPath, "Application", "Features", $"{ctx.Entity}Features");
        var appSvcTests = Path.Combine(ctx.TestsPath, "Application", Services, ctx.Entity);

        yield return (Path.Combine(ctx.AppPath, "Properties", "AssemblyInfo.cs"), AssemblyInfoTemplate());
        yield return (Path.Combine(domainTests, $"{ctx.Entity}DomainServiceTests.cs"), DomainServiceTestsTemplate());
        yield return (Path.Combine(featTests, $"Create{ctx.Entity}CommandHandlerTests.cs"), CreateCommandHandlerTestsTemplate());
        yield return (Path.Combine(featTests, $"Delete{ctx.Entity}CommandHandlerTests.cs"), DeleteCommandHandlerTestsTemplate());
        yield return (Path.Combine(featTests, $"Update{ctx.Entity}CommandHandlerTests.cs"), UpdateCommandHandlerTestsTemplate());
        yield return (Path.Combine(featTests, $"Find{ctx.Entity}ByIdQueryHandlerTests.cs"), FindByIdQueryHandlerTestsTemplate());
        yield return (Path.Combine(featTests, $"Get{ctx.Entity}QueryHandlerTests.cs"), GetQueryHandlerTestsTemplate());
        yield return (Path.Combine(featTests, $"Create{ctx.Entity}CommandValidatorTests.cs"), CreateCommandValidatorTestsTemplate());
        yield return (Path.Combine(featTests, $"Delete{ctx.Entity}CommandValidatorTests.cs"), DeleteCommandValidatorTestsTemplate());
        yield return (Path.Combine(featTests, $"Update{ctx.Entity}CommandValidatorTests.cs"), UpdateCommandValidatorTestsTemplate());
        yield return (Path.Combine(featTests, $"Find{ctx.Entity}ByIdQueryValidatorTests.cs"), FindByIdQueryValidatorTestsTemplate());
        yield return (Path.Combine(featTests, $"Get{ctx.Entity}QueryValidatorTests.cs"), GetQueryValidatorTestsTemplate());
        yield return (Path.Combine(appSvcTests, $"{ctx.Entity}ApplicationServiceTests.cs"), ApplicationServiceTestsTemplate());
    }

    private string AssemblyInfoTemplate() => $$"""
        using System.Runtime.CompilerServices;

        [assembly: InternalsVisibleTo("{{ctx.NS}}.Tests.Unit")]
        """;

    private string DomainServiceTestsTemplate() => $$"""
        using System.Linq.Expressions;
        using Moq;
        using {{ctx.NS}}.Domain.Entities;
        using {{ctx.NS}}.Domain.Interfaces.Repositories;
        using {{ctx.NS}}.Domain.Services;

        namespace {{ctx.NS}}.Tests.Unit.Domain.Services;

        public sealed class {{ctx.Entity}}DomainServiceTests
        {
            private readonly Mock<I{{ctx.Entity}}Repository> _{{ctx.ECamel}}RepositoryMock = new();
            private readonly {{ctx.Entity}}DomainService _service;

            public {{ctx.Entity}}DomainServiceTests()
            {
                _service = new {{ctx.Entity}}DomainService(_{{ctx.ECamel}}RepositoryMock.Object);
            }

            [Fact]
            public async Task FindByArgumentsPagedAsync_ReturnsResult_WhenNoFilterProvided()
            {
                var entities = new List<{{ctx.Entity}}> { new() { {{KeyName}} = {{DefaultTestValue}}, {{EntityTestInitializer()}} } };
                _{{ctx.ECamel}}RepositoryMock
                    .Setup(r => r.CountAsync(It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<{{ctx.Entity}}, bool>>?>()))
                    .ReturnsAsync(1);
                _{{ctx.ECamel}}RepositoryMock
                    .Setup(r => r.FindAsync(It.IsAny<CancellationToken>(), It.IsAny<bool>(),
                        It.IsAny<Expression<Func<{{ctx.Entity}}, bool>>?>(), It.IsAny<int>(), It.IsAny<int>()))
                    .ReturnsAsync(entities);

                var result = await _service.FindByArgumentsPagedAsync({{FindByArgumentsNullTestArgs()}});

                Assert.Equal(1, result.TotalRecords);
            }

            [Fact]
            public async Task FindByArgumentsPagedAsync_ReturnsResult_WhenFilterProvided()
            {
                var entities = new List<{{ctx.Entity}}> { new() { {{KeyName}} = {{DefaultTestValue}}, {{EntityTestInitializer()}} } };
                _{{ctx.ECamel}}RepositoryMock
                    .Setup(r => r.CountAsync(It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<{{ctx.Entity}}, bool>>?>()))
                    .ReturnsAsync(1);
                _{{ctx.ECamel}}RepositoryMock
                    .Setup(r => r.FindAsync(It.IsAny<CancellationToken>(), It.IsAny<bool>(),
                        It.IsAny<Expression<Func<{{ctx.Entity}}, bool>>?>(), It.IsAny<int>(), It.IsAny<int>()))
                    .ReturnsAsync(entities);

                var result = await _service.FindByArgumentsPagedAsync({{FindByArgumentsOneFilterTestArgs()}});

                Assert.Equal(1, result.TotalRecords);
            }
        }
        """;

    private string MapperCommandHandlerTestsTemplate(
        string verb, string commandArgs, string testVerb, string domainServiceMethod) => $$"""
        using AutoMapper;
        using Moq;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.{{verb}}{{ctx.Entity}}Feature;
        using {{ctx.NS}}.Domain.Entities;
        using {{ctx.NS}}.Domain.Interfaces.Services;

        namespace {{ctx.NS}}.Tests.Unit.Application.Features.{{ctx.Entity}}Features;

        public sealed class {{verb}}{{ctx.Entity}}CommandHandlerTests
        {
            private readonly Mock<I{{ctx.Entity}}DomainService> _{{ctx.ECamel}}DomainServiceMock = new();
            private readonly Mock<IMapper> _mapperMock = new();
            private readonly {{verb}}{{ctx.Entity}}CommandHandler _handler;

            public {{verb}}{{ctx.Entity}}CommandHandlerTests()
            {
                _handler = new {{verb}}{{ctx.Entity}}CommandHandler(_{{ctx.ECamel}}DomainServiceMock.Object, _mapperMock.Object);
            }

            [Fact]
            public async Task Handle_ReturnsResponse_WhenEntityIs{{testVerb}}()
            {
                var command = new {{verb}}{{ctx.Entity}}Command({{commandArgs}});
                var entity = new {{ctx.Entity}} { {{KeyName}} = {{DefaultTestValue}}, {{EntityTestInitializer()}} };
                var response = new {{verb}}{{ctx.Entity}}Response({{IdAndPropertiesTestArgs()}});

                _mapperMock.Setup(m => m.Map<{{ctx.Entity}}>(command)).Returns(entity);
                _{{ctx.ECamel}}DomainServiceMock.Setup(s => s.{{domainServiceMethod}}(entity, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
                _mapperMock.Setup(m => m.Map<{{verb}}{{ctx.Entity}}Response>(entity)).Returns(response);

                var result = await _handler.Handle(command, CancellationToken.None);

                Assert.Equal({{DefaultTestValue}}, result.{{KeyName}});
                {{HandlerTestAssertions("result")}}
            }
        }
        """;

    private string CreateCommandHandlerTestsTemplate() =>
        MapperCommandHandlerTestsTemplate("Create", CreateTestArgs(), "Created", "AddAsync");

    private string DeleteCommandHandlerTestsTemplate() => $$"""
        using Moq;
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Delete{{ctx.Entity}}Feature;
        using {{ctx.NS}}.Domain.Interfaces.Services;

        namespace {{ctx.NS}}.Tests.Unit.Application.Features.{{ctx.Entity}}Features;

        public sealed class Delete{{ctx.Entity}}CommandHandlerTests
        {
            private readonly Mock<I{{ctx.Entity}}DomainService> _{{ctx.ECamel}}DomainServiceMock = new();
            private readonly Delete{{ctx.Entity}}CommandHandler _handler;

            public Delete{{ctx.Entity}}CommandHandlerTests()
            {
                _handler = new Delete{{ctx.Entity}}CommandHandler(_{{ctx.ECamel}}DomainServiceMock.Object);
            }

            [Fact]
            public async Task Handle_ReturnsSuccess_WhenEntityIsDeleted()
            {
                var command = new Delete{{ctx.Entity}}Command({{DefaultTestValue}});
                _{{ctx.ECamel}}DomainServiceMock.Setup(s => s.RemoveByIdAsync({{DefaultTestValue}}, It.IsAny<CancellationToken>())).ReturnsAsync(true);

                var result = await _handler.Handle(command, CancellationToken.None);

                Assert.True(result.Success);
            }

            [Fact]
            public async Task Handle_ReturnsFailure_WhenEntityNotFound()
            {
                var zeroVal = {{ (KeyType == "string" ? "\"\"" : "0") }};
                var command = new Delete{{ctx.Entity}}Command(zeroVal);
                _{{ctx.ECamel}}DomainServiceMock.Setup(s => s.RemoveByIdAsync(zeroVal, It.IsAny<CancellationToken>())).ReturnsAsync(false);

                var result = await _handler.Handle(command, CancellationToken.None);

                Assert.False(result.Success);
            }
        }
        """;

    private string UpdateCommandHandlerTestsTemplate() =>
        MapperCommandHandlerTestsTemplate("Update", IdAndPropertiesTestArgs(), "Updated", "UpdateAsync");

    private string FindByIdQueryHandlerTestsTemplate() => $$"""
        using AutoMapper;
        using Moq;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Find{{ctx.Entity}}ByIdFeature;
        using {{ctx.NS}}.Domain.Interfaces.Services;

        namespace {{ctx.NS}}.Tests.Unit.Application.Features.{{ctx.Entity}}Features;

        public sealed class Find{{ctx.Entity}}ByIdQueryHandlerTests
        {
            private readonly Mock<I{{ctx.Entity}}DomainService> _{{ctx.ECamel}}DomainServiceMock = new();
            private readonly Mock<IMapper> _mapperMock = new();
            private readonly Find{{ctx.Entity}}ByIdQueryHandler _handler;

            public Find{{ctx.Entity}}ByIdQueryHandlerTests()
            {
                _handler = new Find{{ctx.Entity}}ByIdQueryHandler(_{{ctx.ECamel}}DomainServiceMock.Object, _mapperMock.Object);
            }

            [Fact]
            public async Task Handle_ReturnsResponse_WhenEntityIsFound()
            {
                var query = new Find{{ctx.Entity}}ByIdQuery({{DefaultTestValue}});
                var response = new {{ctx.Entity}}Response({{IdAndPropertiesTestArgs()}});

                _mapperMock.Setup(m => m.Map<{{ctx.Entity}}Response>(It.IsAny<object>())).Returns(response);

                var result = await _handler.Handle(query, CancellationToken.None);

                Assert.Equal({{DefaultTestValue}}, result.{{KeyName}});
            }
        }
        """;

    private string GetQueryHandlerTestsTemplate() => $$"""
        using AutoMapper;
        using Moq;
        using {{ctx.NS}}.Application.DTOs.Base.Response;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Get{{ctx.EPlural}}Feature;
        using {{ctx.NS}}.Domain.Interfaces.Services;

        namespace {{ctx.NS}}.Tests.Unit.Application.Features.{{ctx.Entity}}Features;

        public sealed class Get{{ctx.Entity}}QueryHandlerTests
        {
            private readonly Mock<I{{ctx.Entity}}DomainService> _{{ctx.ECamel}}DomainServiceMock = new();
            private readonly Mock<IMapper> _mapperMock = new();
            private readonly Get{{ctx.Entity}}QueryHandler _handler;

            public Get{{ctx.Entity}}QueryHandlerTests()
            {
                _handler = new Get{{ctx.Entity}}QueryHandler(_{{ctx.ECamel}}DomainServiceMock.Object, _mapperMock.Object);
            }

            [Fact]
            public async Task Handle_CallsServiceWithCorrectParameters()
            {
                var query = new Get{{ctx.Entity}}Query({{FilterNullArgsWithComma}}2, 10);

                await _handler.Handle(query, CancellationToken.None);

                _{{ctx.ECamel}}DomainServiceMock.Verify(
                    s => s.FindByArgumentsPagedAsync({{FilterNullArgsWithComma}}2, 10, It.IsAny<CancellationToken>()),
                    Times.Once());
            }
        }
        """;

    private string CreateCommandValidatorTestsTemplate() => $$"""
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Create{{ctx.Entity}}Feature;

        namespace {{ctx.NS}}.Tests.Unit.Application.Features.{{ctx.Entity}}Features;

        public sealed class Create{{ctx.Entity}}CommandValidatorTests
        {
            private readonly Create{{ctx.Entity}}CommandValidator _validator = new();

            {{BuildCreateValidatorTestMethods()}}
        }
        """;

    private string DeleteCommandValidatorTestsTemplate() => $$"""
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Delete{{ctx.Entity}}Feature;

        namespace {{ctx.NS}}.Tests.Unit.Application.Features.{{ctx.Entity}}Features;

        public sealed class Delete{{ctx.Entity}}CommandValidatorTests
        {
            private readonly Delete{{ctx.Entity}}CommandValidator _validator = new();

            {{BuildIdValidatorTestMethods($"Delete{ctx.Entity}Command")}}
        }
        """;

    private string UpdateCommandValidatorTestsTemplate() => $$"""
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Update{{ctx.Entity}}Feature;

        namespace {{ctx.NS}}.Tests.Unit.Application.Features.{{ctx.Entity}}Features;

        public sealed class Update{{ctx.Entity}}CommandValidatorTests
        {
            private readonly Update{{ctx.Entity}}CommandValidator _validator = new();

            {{BuildUpdateValidatorTestMethods()}}
        }
        """;

    private string FindByIdQueryValidatorTestsTemplate() => $$"""
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Find{{ctx.Entity}}ByIdFeature;

        namespace {{ctx.NS}}.Tests.Unit.Application.Features.{{ctx.Entity}}Features;

        public sealed class Find{{ctx.Entity}}ByIdQueryValidatorTests
        {
            private readonly Find{{ctx.Entity}}ByIdQueryValidator _validator = new();

            {{BuildIdValidatorTestMethods($"Find{ctx.Entity}ByIdQuery")}}
        }
        """;

    private string GetQueryValidatorTestsTemplate() => $$"""
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Get{{ctx.EPlural}}Feature;

        namespace {{ctx.NS}}.Tests.Unit.Application.Features.{{ctx.Entity}}Features;

        public sealed class Get{{ctx.Entity}}QueryValidatorTests
        {
            private readonly Get{{ctx.Entity}}QueryValidator _validator = new();

            [Fact]
            public void Validate_IsValid_WhenPageAndPageSizeAreValid()
            {
                var result = _validator.Validate(new Get{{ctx.Entity}}Query({{FilterNullArgsWithComma}}1, 5));
                Assert.True(result.IsValid);
            }

            [Fact]
            public void Validate_IsInvalid_WhenPageIsZero()
            {
                var result = _validator.Validate(new Get{{ctx.Entity}}Query({{FilterNullArgsWithComma}}0, 5));
                Assert.False(result.IsValid);
                Assert.Contains(result.Errors, e => e.PropertyName == "Page");
            }

            [Fact]
            public void Validate_IsInvalid_WhenPageSizeIsBelowMinimum()
            {
                var result = _validator.Validate(new Get{{ctx.Entity}}Query({{FilterNullArgsWithComma}}1, 4));
                Assert.False(result.IsValid);
                Assert.Contains(result.Errors, e => e.PropertyName == "PageSize");
            }
        }
        """;

    private string ApplicationServiceTestsTemplate() => $$"""
        using AutoMapper;
        using MediatR;
        using Moq;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Requests;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Create{{ctx.Entity}}Feature;
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Delete{{ctx.Entity}}Feature;
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Find{{ctx.Entity}}ByIdFeature;
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Get{{ctx.EPlural}}Feature;
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Update{{ctx.Entity}}Feature;
        using {{ctx.NS}}.Application.Services;

        namespace {{ctx.NS}}.Tests.Unit.Application.Services;

        public sealed class {{ctx.Entity}}ApplicationServiceTests
        {
            private readonly Mock<IMediator> _mediatorMock = new();
            private readonly Mock<IMapper> _mapperMock = new();
            private readonly {{ctx.Entity}}ApplicationService _service;

            public {{ctx.Entity}}ApplicationServiceTests()
            {
                _service = new {{ctx.Entity}}ApplicationService(_mediatorMock.Object, _mapperMock.Object);
            }

            [Fact]
            public async Task CreateAsync_SendsCommand_ToMediator()
            {
                var request = new Create{{ctx.Entity}}Request({{CreateTestArgs()}});
                var command = new Create{{ctx.Entity}}Command({{CreateTestArgs()}});
                _mapperMock.Setup(m => m.Map<Create{{ctx.Entity}}Command>(request)).Returns(command);

                await _service.CreateAsync(request, CancellationToken.None);

                _mediatorMock.Verify(m => m.Send(It.IsAny<Create{{ctx.Entity}}Command>(), It.IsAny<CancellationToken>()), Times.Once());
            }

            [Fact]
            public async Task UpdateAsync_SendsCommand_ToMediator()
            {
                var request = new Update{{ctx.Entity}}Request({{IdAndPropertiesTestArgs()}});
                var command = new Update{{ctx.Entity}}Command({{IdAndPropertiesTestArgs()}});
                _mapperMock.Setup(m => m.Map<Update{{ctx.Entity}}Command>(request)).Returns(command);

                await _service.UpdateAsync(request, CancellationToken.None);

                _mediatorMock.Verify(m => m.Send(It.IsAny<Update{{ctx.Entity}}Command>(), It.IsAny<CancellationToken>()), Times.Once());
            }

            [Fact]
            public async Task DeleteAsync_SendsCommand_ToMediator()
            {
                var request = new Delete{{ctx.Entity}}Request({{DefaultTestValue}});
                var command = new Delete{{ctx.Entity}}Command({{DefaultTestValue}});
                _mapperMock.Setup(m => m.Map<Delete{{ctx.Entity}}Command>(request)).Returns(command);

                await _service.DeleteAsync(request, CancellationToken.None);

                _mediatorMock.Verify(m => m.Send(It.IsAny<Delete{{ctx.Entity}}Command>(), It.IsAny<CancellationToken>()), Times.Once());
            }

            [Fact]
            public async Task GetByIdAsync_SendsQuery_ToMediator()
            {
                var request = new Find{{ctx.Entity}}ByIdRequest({{DefaultTestValue}});
                var query = new Find{{ctx.Entity}}ByIdQuery({{DefaultTestValue}});
                _mapperMock.Setup(m => m.Map<Find{{ctx.Entity}}ByIdQuery>(request)).Returns(query);

                await _service.GetByIdAsync(request, CancellationToken.None);

                _mediatorMock.Verify(m => m.Send(It.IsAny<Find{{ctx.Entity}}ByIdQuery>(), It.IsAny<CancellationToken>()), Times.Once());
            }

            [Fact]
            public async Task GetAsync_SendsQuery_ToMediator()
            {
                var request = new Get{{ctx.Entity}}Request({{FilterNullArgsWithComma}}1, 5);
                var query = new Get{{ctx.Entity}}Query({{FilterNullArgsWithComma}}1, 5);
                _mapperMock.Setup(m => m.Map<Get{{ctx.Entity}}Query>(request)).Returns(query);

                await _service.GetAsync(request, CancellationToken.None);

                _mediatorMock.Verify(m => m.Send(It.IsAny<Get{{ctx.Entity}}Query>(), It.IsAny<CancellationToken>()), Times.Once());
            }
        }
        """;
}
