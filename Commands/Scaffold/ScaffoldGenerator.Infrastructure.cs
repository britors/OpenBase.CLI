namespace OpenBase.CLI.Commands.Scaffold;

public sealed partial class ScaffoldGenerator
{
    private IEnumerable<(string, string)> InfrastructureFiles()
    {
        yield return (
            Path.Combine(ctx.InfraContextPath, "Configurations", $"{ctx.Entity}Configuration.cs"),
            EfConfigurationTemplate());
        yield return (
            Path.Combine(ctx.InfraDataPath, "Repositories", ctx.Entity, $"{ctx.Entity}Repository.cs"),
            RepositoryTemplate());
    }

    private string EfConfigurationTemplate()
    {
        var toTable = string.IsNullOrWhiteSpace(ctx.Schema)
            ? $"builder.ToTable(\"{ctx.TableName ?? ctx.EPlural}\");"
            : $"builder.ToTable(\"{ctx.TableName ?? ctx.EPlural}\", \"{ctx.Schema}\");";

        var pkColName = PkProperty?.DbColumnName ?? KeyName;

        return $$"""
        using Microsoft.EntityFrameworkCore;
        using Microsoft.EntityFrameworkCore.Metadata.Builders;
        using {{ctx.NS}}.Domain.Entities;

        namespace {{ctx.NS}}.Infra.Data.Context.Configurations;

        internal sealed class {{ctx.Entity}}Configuration : IEntityTypeConfiguration<{{ctx.Entity}}>
        {
            public void Configure(EntityTypeBuilder<{{ctx.Entity}}> builder)
            {
                {{toTable}}

                builder.HasKey(x => x.{{KeyName}});

                builder.Property(x => x.{{KeyName}}).HasColumnName("{{pkColName}}");{{EfPropertyBlocks()}}
            }
        }
        """;
    }

    private string RepositoryTemplate() => $$"""
        using {{ctx.NS}}.Domain.Entities;
        using {{ctx.NS}}.Domain.Interfaces.Repositories;
        using {{ctx.NS}}.Infra.Data.Context;

        namespace {{ctx.NS}}.Infra.Data.Repositories;

        public sealed partial class {{ctx.Entity}}Repository(
            DbSession dbSession,
            OneBaseDataBaseContext context)
            : RepositoryBase<{{ctx.Entity}}>(dbSession, context), I{{ctx.Entity}}Repository, IDataRepository
        {
        }
        """;
}
