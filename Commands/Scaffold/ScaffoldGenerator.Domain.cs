namespace OpenBase.CLI.Commands.Scaffold;

public sealed partial class ScaffoldGenerator
{
    private IEnumerable<(string, string)> DomainFiles()
    {
        yield return (Path.Combine(ctx.DomainPath, "Entities", $"{ctx.Entity}.cs"), EntityTemplate());
        yield return (Path.Combine(ctx.DomainPath, "Interfaces", "Repositories", ctx.Entity, $"I{ctx.Entity}Repository.cs"), IRepositoryTemplate());
        yield return (Path.Combine(ctx.DomainPath, "Interfaces", Services, ctx.Entity, $"I{ctx.Entity}DomainService.cs"), IDomainServiceTemplate());
        yield return (Path.Combine(ctx.DomainPath, Services, ctx.Entity, $"{ctx.Entity}DomainService.cs"), DomainServiceTemplate());
    }

    private string EntityTemplate() => $$"""
        using {{ctx.NS}}.Domain.Interfaces.Repositories;

        namespace {{ctx.NS}}.Domain.Entities;

        public sealed class {{ctx.Entity}} : IEntityOrQueryResult
        {
            public int Id { get; init; }
            {{EntityPropertyDeclarations()}}
        }
        """;

    private string IRepositoryTemplate() => $$"""
        using {{ctx.NS}}.Domain.Entities;

        namespace {{ctx.NS}}.Domain.Interfaces.Repositories;

        public partial interface I{{ctx.Entity}}Repository : IRepositoryBase<{{ctx.Entity}}>
        {
        }
        """;

    private string IDomainServiceTemplate() => $$"""
        using {{ctx.NS}}.Domain.Entities;
        using {{ctx.NS}}.Domain.QueryResults;

        namespace {{ctx.NS}}.Domain.Interfaces.Services;

        public partial interface I{{ctx.Entity}}DomainService : IDomainService<{{ctx.Entity}}, int>
        {
            Task<PaginatedQueryResult<{{ctx.Entity}}>> FindByArgumentsPagedAsync(
                {{FindByArgumentsSignatureParams()}});
        }
        """;

    private string DomainServiceTemplate() => $$"""
        using {{ctx.NS}}.Domain.Entities;
        using {{ctx.NS}}.Domain.Interfaces.Repositories;
        using {{ctx.NS}}.Domain.Interfaces.Services;
        using {{ctx.NS}}.Domain.QueryResults;
        using System.Linq.Expressions;

        namespace {{ctx.NS}}.Domain.Services;

        public sealed partial class {{ctx.Entity}}DomainService(I{{ctx.Entity}}Repository {{ctx.ECamel}}Repository)
            : DomainService<{{ctx.Entity}}, int>({{ctx.ECamel}}Repository), I{{ctx.Entity}}DomainService
        {
            public async Task<PaginatedQueryResult<{{ctx.Entity}}>> FindByArgumentsPagedAsync(
                {{FindByArgumentsSignatureParams()}})
            {
                {{FilterBodyCode()}}

                var totalRecords = await {{ctx.ECamel}}Repository.CountAsync(cancellationToken, filter);
                var resultPaginated = await {{ctx.ECamel}}Repository.FindAsync(
                    cancellationToken,
                    noTracking: true,
                    filter,
                    pageNumber: page,
                    pageSize: pageSize);

                return new PaginatedQueryResult<{{ctx.Entity}}>(page, pageSize, totalRecords, resultPaginated);
            }

            private static Expression<Func<{{ctx.Entity}}, bool>>? And(
                Expression<Func<{{ctx.Entity}}, bool>>? left,
                Expression<Func<{{ctx.Entity}}, bool>> right)
            {
                if (left is null) return right;
                var param = left.Parameters[0];
                var body = Expression.AndAlso(
                    left.Body,
                    new ReplaceParamVisitor(right.Parameters[0], param).Visit(right.Body)!);
                return Expression.Lambda<Func<{{ctx.Entity}}, bool>>(body, param);
            }

            private sealed class ReplaceParamVisitor(ParameterExpression from, ParameterExpression to)
                : ExpressionVisitor
            {
                protected override Expression VisitParameter(ParameterExpression node)
                    => node == from ? to : base.VisitParameter(node);
            }
        }
        """;
}
