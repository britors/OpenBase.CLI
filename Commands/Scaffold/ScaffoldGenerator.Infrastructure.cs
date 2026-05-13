namespace OpenBase.CLI.Commands.Scaffold;

public sealed partial class ScaffoldGenerator
{
    private IEnumerable<(string, string)> InfrastructureFiles()
    {
        yield return (
            Path.Combine(ctx.InfraContextPath, "Configurations", $"{ctx.Entity}Configuration.cs"),
            EfConfigurationTemplate());
        yield return (
            Path.Combine(ctx.InfraDataPath, "Repositories", $"{ctx.Entity}Repository.cs"),
            RepositoryTemplate());
    }

    private string EfConfigurationTemplate() => $$"""
        using Microsoft.EntityFrameworkCore;
        using Microsoft.EntityFrameworkCore.Metadata.Builders;
        using {{ctx.NS}}.Domain.Entities;

        namespace {{ctx.NS}}.Infra.Data.Context.Configurations;

        internal sealed class {{ctx.Entity}}Configuration : IEntityTypeConfiguration<{{ctx.Entity}}>
        {
            public void Configure(EntityTypeBuilder<{{ctx.Entity}}> builder)
            {
                builder.ToTable("{{ctx.TableName ?? ctx.EPlural}}");

                builder.HasKey(x => x.Id);

                builder.Property(x => x.Id).HasColumnName("Id");{{EfPropertyBlocks()}}
            }
        }
        """;

    private string RepositoryTemplate() => $$"""
        using {{ctx.NS}}.Domain.Entities;
        using {{ctx.NS}}.Domain.Interfaces.Repositories;
        using {{ctx.NS}}.Infra.Data.Context;

        namespace {{ctx.NS}}.Infra.Data.Repositories;

        public sealed class {{ctx.Entity}}Repository(
            DbSession dbSession,
            OneBaseDataBaseContext context)
            : RepositoryBase<{{ctx.Entity}}>(dbSession, context), I{{ctx.Entity}}Repository, IDataRepository
        {
        }
        """;
}
