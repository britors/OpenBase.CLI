# OpenBase CLI

![OpenBase CLI](https://raw.githubusercontent.com/britors/OpenBase.CLI/main/banner.png)

The official command-line interface for the **OpenBase** ecosystem.

---

## Installation

Distributed as a .NET global tool:

```bash
dotnet tool install -g w3ti.OpenBase.Cli
```

To update:

```bash
dotnet tool update -g w3ti.OpenBase.Cli
```

---

## Usage

### 1. Build the project

```bash
openbase build
```

Runs `dotnet restore → dotnet build → dotnet test` in sequence, stopping immediately on the first failure.

| Flag | Description | Default |
|------|-------------|---------|
| `--configuration` | `Debug` or `Release` | `Debug` |
| `--no-restore` | Skip `dotnet restore` | `false` |

The command automatically detects the nearest `.sln` file (or `.csproj` if no solution is found).

---

### 2. Run the project

```bash
openbase run
```

Runs `dotnet restore → dotnet build` (without tests), starts the `Presentation.Api` project with live console output, and automatically opens the browser at the Swagger UI after 5 seconds.

The Swagger URL is read from `Properties/launchSettings.json` (prefers HTTPS). Fallback: `https://localhost:5001/swagger`.

| Flag | Description | Default |
|------|-------------|---------|
| `--configuration` | `Debug` or `Release` | `Debug` |
| `--no-build` | Skip restore + build | `false` |

Press `Ctrl+C` to stop the application.

---

### 3. Install the templates

```bash
openbase install
```

### 4. Create a new project

```bash
# SQL Server
openbase new --type api --template sqlserver --name MyProject

# PostgreSQL
openbase new --type api --template pgsql --name MyProject

# Oracle
openbase new --type api --template oracle --name MyProject
```

The wizard will prompt for project configuration:

```
Project configuration

MediatR license (leave blank if you don't have one): <your-license>
AutoMapper license (leave blank if you don't have one): <your-license>
Database server [.]: .
Database user:
Database password:
```

The settings are automatically written to the `appsettings.json` and `appsettings.Development.json` files of the generated project.

### 5. Scaffold an entity

From the root of the created project:

```bash
openbase scaffold --entity Product
```

The command automatically detects whether the project uses **SQL Server**, **PostgreSQL** or **Oracle** and opens an interactive wizard to define the entity's properties:

```
Entity properties
Database: SqlServer | Available types: int, long, short, string, bool, decimal, ...

Prop 1 — Name (PascalCase): Name
  Type: string
  Not null (required)? [y/n] (y): y
  + Name (string)

Add another property? [y/n] (n): y

Prop 2 — Name (PascalCase): Price
  Type: decimal
  Not null (required)? [y/n] (y): y
  + Price (decimal)

Add another property? [y/n] (n): n

┌────────────┬─────────┬────┬──────────┐
│ Property   │ Type    │ PK │ Not Null │
├────────────┼─────────┼────┼──────────┤
│ Id         │ int     │ ✓  │ ✓        │
│ Name       │ string  │ -  │ ✓        │
│ Price      │ decimal │ -  │ ✓        │
└────────────┴─────────┴────┴──────────┘
```

At the end, **47 files** are generated covering all Clean Architecture layers, and the entity's `DbSet` is **automatically injected** into `OneBaseDataBaseContext`:

| Layer          | What is generated                                             |
|----------------|---------------------------------------------------------------|
| Domain         | Entity, IRepository, IDomainService, DomainService            |
| Application    | DTOs, Commands/Queries, Handlers, Validators, Mapper, Service |
| Infrastructure | EF Core Configuration, Repository                             |
| Presentation   | Controller with full CRUD endpoints                           |
| Tests          | Unit tests for handlers, validators and services              |

#### Available property types

| Type            | SQL Server | PostgreSQL | Oracle |
|-----------------|:----------:|:----------:|:------:|
| `int`           | ✓          | ✓          | ✓      |
| `long`          | ✓          | ✓          | ✓      |
| `short`         | ✓          | ✓          | ✓      |
| `string`        | ✓          | ✓          | ✓      |
| `bool`          | ✓          | ✓          | ✓      |
| `decimal`       | ✓          | ✓          | ✓      |
| `float`         | ✓          | ✓          | ✓      |
| `double`        | ✓          | ✓          | ✓      |
| `DateTime`      | ✓          | ✓          | ✓      |
| `DateOnly`      | ✓          | ✓          | ✓      |
| `TimeOnly`      | ✓          | ✓          | ✓      |
| `DateTimeOffset`| ✓          | ✓          | ✓      |
| `Guid`          | ✓          | ✓          | ✓      |
| `byte[]`        | ✓          | ✓          | ✓      |
| `JsonDocument`  |            | ✓          |        |

#### Validation rules auto-generated in Validators

- `string` required → `NotEmpty().MinimumLength(1).MaximumLength(255)`
- `Guid` required → `NotEmpty()`
- String fields on Update → rule with `.When(x => !string.IsNullOrWhiteSpace(x.Prop))`

#### Next steps after scaffold

The `DbSet` is automatically injected into `OneBaseDataBaseContext.cs`. Just run the migrations:

```bash
dotnet ef migrations add AddProduct
dotnet ef database update
```

#### Updating an existing entity

When the database table changes (new column, removed column, type change), use `--update` to sync the generated files:

```bash
openbase scaffold --entity Product --update
```

The command reads the current table structure from the database (**Model First**), compares it with the existing `Entity.cs`, and shows a diff before applying any changes:

```
Detected differences:
  + Description (string?)   → new column
  - Weight (decimal)        → removed column
  ~ Price: decimal → float  → type changed

Apply changes? [y/n]
⚠️  1 property(ies) will be REMOVED from multiple files.
Confirm removal? [y/n]

16 file(s) updated:
  src/Domain/Entities/Product.cs
  src/Application/DTOs/Product/Requests/CreateProductRequest.cs
  ...

Scaffold for entity Product updated successfully!
Don't forget to create a migration: dotnet ef migrations add UpdateProduct
```

**Files updated by `--update` (16):**

| Layer | Files |
|-------|-------|
| Domain | `Entity.cs`, `IDomainService.cs`, `DomainService.cs` |
| Application DTOs | `Create/Update/GetRequest`, `EntityResponse`, `Create/UpdateResponse` |
| Application Features | `CreateCommand`, `UpdateCommand`, `Create/UpdateValidator`, `GetQuery`, `GetQueryHandler` |
| Infrastructure | `{Entity}Configuration.cs` (EF Core) |

Files **not overwritten** (may contain custom code): handlers, repositories, controller, and test files.

> **Recommended**: commit or stash any uncommitted changes before running `--update`. The command warns you if it detects modified files related to the entity.

After the update, generate a new migration:

```bash
dotnet ef migrations add UpdateProduct
dotnet ef database update
```

---

### 6. Add specialist methods

After scaffolding an entity, add custom Query or Command methods to extend all Clean Architecture layers at once:

```bash
openbase specialist --entity Product
```

The wizard guides you through each method definition:

```
Method name (PascalCase): FindProductByCategory
Method type:
> Query — MediatR query (read)
  Command — MediatR command (write)

Enter the SQL/operation with parameters as {{ paramName }}:
  SELECT p.Name, p.Price, c.Description
  FROM Products p JOIN Categories c ON c.Id = p.CategoryId
  WHERE c.Name LIKE @CategoryName

Parameters detected: {{ CategoryName }}
  CategoryName — C# type: string

Does the query return paginated results? [y/n] (n): n

Define the result columns:
  Column 1 name (blank to finish): Name
  Name — C# type: string
  Column 2 name (blank to finish): Price
  Price — C# type: decimal
  Column 3 name (blank to finish): Description
  Description — C# type: string
  Column 4 name (blank to finish):

Add another specialist method? [y/n] (n): n
```

#### Files generated per Query specialist (14 files)

| Layer | File | Notes |
|---|---|---|
| Domain | `QueryResults/{method}QueryResult.cs` | `readonly record struct` — Dapper target, stack-allocated |
| Domain | `Interfaces/Repositories/I{Entity}Repository.{method}.cs` | Partial interface |
| Domain | `Interfaces/Services/I{Entity}DomainService.{method}.cs` | Partial interface |
| Domain | `Services/{Entity}DomainService.{method}.cs` | Partial class |
| Infrastructure | `Repositories/{Entity}Repository.{method}.cs` | Dapper query using `{method}QueryResult` |
| Application | `DTOs/{Entity}/Requests/{method}Request.cs` | HTTP request DTO |
| Application | `DTOs/{Entity}/Responses/{method}Response.cs` | Response DTO with result columns |
| Application | `Mappers/{method}MapperProfile.cs` | `CreateMap<{method}QueryResult, {method}Response>()` |
| Application | `Features/{method}Feature/{method}Query.cs` | MediatR query record |
| Application | `Features/{method}Feature/{method}QueryHandler.cs` | Handler with AutoMapper |
| Application | `Features/{method}Feature/{method}QueryValidator.cs` | FluentValidation validator |
| Application | `Interfaces/Services/I{Entity}ApplicationService.{method}.cs` | Partial interface |
| Application | `Services/{Entity}ApplicationService.{method}.cs` | Partial class |
| Presentation | `Controllers/{Entity}Controller.{method}.cs` | `[HttpGet]` with `[FromQuery]` |

For **paginated** queries the return types change to `PaginatedQueryResult<{method}QueryResult>` / `PaginatedResponse<{method}Response>` across all layers, and the mapper profile also includes:

```csharp
CreateMap<PaginatedQueryResult<{method}QueryResult>, PaginatedResponse<{method}Response>>();
```

#### Files generated per Command specialist (13 files)

| Layer | File | Notes |
|---|---|---|
| Domain | `Interfaces/Repositories/I{Entity}Repository.{method}.cs` | Partial interface |
| Domain | `Interfaces/Services/I{Entity}DomainService.{method}.cs` | Partial interface |
| Domain | `Services/{Entity}DomainService.{method}.cs` | Partial class |
| Infrastructure | `Repositories/{Entity}Repository.{method}.cs` | Dapper execute |
| Application | `DTOs/{Entity}/Requests/{method}Request.cs` | HTTP request DTO |
| Application | `DTOs/{Entity}/Responses/{method}Response.cs` | `record {method}Response(bool Success)` |
| Application | `Features/{method}Feature/{method}Command.cs` | MediatR command record |
| Application | `Features/{method}Feature/{method}CommandHandler.cs` | Returns `{method}Response` |
| Application | `Features/{method}Feature/{method}CommandValidator.cs` | FluentValidation validator |
| Application | `Interfaces/Services/I{Entity}ApplicationService.{method}.cs` | Partial interface |
| Application | `Services/{Entity}ApplicationService.{method}.cs` | Partial class |
| Presentation | `Controllers/{Entity}Controller.{method}.cs` | `[HttpPost]` with `[FromBody]` |

> Specialist methods can also be added right after running `openbase scaffold` — the wizard offers the option at the end of the scaffold flow.

---

### 7. Add an extension

Extensions add cross-cutting capabilities to an existing OpenBase project. Run from the solution root:

```bash
openbase extension add <name>
openbase extension add <name> --provider <provider>
```

The command:
1. Detects the project structure automatically
2. Adds the required NuGet packages to the relevant `.csproj` files
3. Generates the source files in the correct Clean Architecture layers
4. Injects configuration into `appsettings.json` where applicable
5. Injects the required middleware calls into `Program.cs` automatically
6. Registers the extension in `.openbase/extensions.json` to prevent duplicate installs

#### Available extensions

| Extension      | Command                               | Description                            |
|----------------|---------------------------------------|----------------------------------------|
| `jwt`          | `openbase extension add jwt`          | JWT Bearer authentication              |
| `healthchecks` | `openbase extension add healthchecks` | Health Checks with UI dashboard        |
| `redis`        | `openbase extension add redis`        | Distributed cache with Redis           |

#### `jwt` — JWT Authentication

```bash
openbase extension add jwt
```

Generates:

| File                                               | Layer          | Description                                      |
|----------------------------------------------------|----------------|--------------------------------------------------|
| `Application/Interfaces/Services/ITokenService.cs` | Application    | Interface for token generation                   |
| `Infra.Data/Services/TokenService.cs`              | Infrastructure | Implementation using `JwtSecurityTokenHandler`   |
| `Presentation.Api/Extensions/JwtExtensions.cs`     | Presentation   | `AddJwtAuthentication` extension method          |

Also automatically:

- Injects the `Jwt` section into `appsettings.json`:

```json
"Jwt": {
  "Secret": "CHANGE-ME-USE-AT-LEAST-32-CHARS-SECRET",
  "Issuer": "YourNamespace",
  "Audience": "YourNamespace",
  "ExpirationMinutes": 60
}
```

- Injects the required middleware into `Program.cs`:

```csharp
builder.Services.AddJwtAuthentication(builder.Configuration);
// ...
app.UseAuthentication();
app.UseAuthorization();
```

- Adds `[Authorize]` to all existing controllers in the project.
- New controllers generated with `openbase scaffold` are automatically created with `[Authorize]` when the JWT extension is installed.

> **After installation**, update the `Jwt:Secret` in `appsettings.json` with a strong secret (at least 32 characters).

#### `healthchecks` — Health Checks

```bash
openbase extension add healthchecks
```

Automatically detects installed services and adds the corresponding checks:

| Detected service | How it's detected                              | Package added                              |
|------------------|------------------------------------------------|--------------------------------------------|
| SQL Server       | `Microsoft.EntityFrameworkCore.SqlServer` in `Infra.Data.csproj` | `AspNetCore.HealthChecks.SqlServer` |
| PostgreSQL       | `Npgsql.EntityFrameworkCore` in `Infra.Data.csproj`              | `AspNetCore.HealthChecks.NpgSql`    |
| Redis            | `redis` extension installed via registry       | `AspNetCore.HealthChecks.Redis`            |
| RabbitMQ         | `rabbitmq` extension installed via registry    | `AspNetCore.HealthChecks.RabbitMQ`         |

Generates:

| File                                                        | Layer        | Description                                           |
|-------------------------------------------------------------|--------------|-------------------------------------------------------|
| `Presentation.Api/Extensions/HealthChecksExtensions.cs`     | Presentation | `AddOpenBaseHealthChecks` and `MapOpenBaseHealthChecks` extension methods |

Also automatically injects into `Program.cs`:

```csharp
builder.Services.AddOpenBaseHealthChecks(builder.Configuration);
// ...
app.MapOpenBaseHealthChecks();
```

Exposes three endpoints:

| Endpoint      | Description                                      |
|---------------|--------------------------------------------------|
| `/health`     | Full health report (JSON, compatible with UI)    |
| `/health/ready` | Only checks tagged as `ready`                  |
| `/health-ui`  | Visual dashboard (HealthChecks UI)               |

#### `redis` — Redis Cache

```bash
openbase extension add redis
```

Generates:

| File                                                        | Layer          | Description                                       |
|-------------------------------------------------------------|----------------|---------------------------------------------------|
| `Application/Interfaces/Services/ICacheService.cs`          | Application    | Interface with `GetAsync`, `SetAsync`, `RemoveAsync`, `ExistsAsync` (with TTL support) |
| `Infra.Data/Services/RedisCacheService.cs`                  | Infrastructure | Implementation via `IDistributedCache`            |
| `Presentation.Api/Extensions/RedisExtensions.cs`            | Presentation   | `AddRedisCache` extension method                  |

Also automatically:

- Adds the `Microsoft.Extensions.Caching.StackExchangeRedis` NuGet package
- Injects the `Redis` section into `appsettings.json`:

```json
"Redis": {
  "ConnectionString": "localhost:6379",
  "InstanceName": "openbase_"
}
```

- Injects the required call into `Program.cs`:

```csharp
builder.Services.AddRedisCache(builder.Configuration);
```

> **After installation**, update the `Redis:ConnectionString` in `appsettings.json` to point to your Redis instance.

> **HealthChecks integration**: if both extensions are installed, `openbase extension add healthchecks` automatically adds the Redis health check (`AspNetCore.HealthChecks.Redis`).

---

## Available commands

| Command                  | Description                                              | Example                                                        |
|--------------------------|----------------------------------------------------------|----------------------------------------------------------------|
| `build`                  | Restores, builds and tests the project (fail-fast)       | `openbase build`                                               |
| `run`                    | Builds and runs the project, opening Swagger in browser  | `openbase run`                                                 |
| `install`                | Installs the required NuGet templates                    | `openbase install`                                             |
| `new`                    | Creates a new project from the templates                 | `openbase new --type api --template sqlserver --name X`<br>`openbase new --type api --template pgsql --name X`<br>`openbase new --type api --template oracle --name X` |
| `scaffold`               | Generates all layers for an entity; `--update` syncs with table changes | `openbase scaffold --entity Product`<br>`openbase scaffold --entity Product --update` |
| `specialist`             | Adds specialist Query/Command methods to an existing entity across all layers | `openbase specialist --entity Product` |
| `extension add`          | Adds an installable extension to the project             | `openbase extension add jwt`                                   |
| `update`                 | Updates the CLI and templates to the latest version      | `openbase update`                                              |
| `history`                | Shows the update history per component                   | `openbase history --type cli`                                  |
| `version show`           | Shows the installed CLI and template versions            | `openbase version show`                                        |
| `version restore`        | Restores a component to a specific version               | `openbase version restore 10.5.9 --type cli`                   |
| `help`                   | Full guide to arguments and flags                        | `openbase help`                                                |

### Update history

```bash
# Show full history
openbase history

# Filter by component
openbase history --type cli
openbase history --type sqlserver
openbase history --type postgres
openbase history --type oracle
```

### Restore a version

Restores a component to a specific version. Useful to roll back a problematic update.

```bash
# Restore the CLI to a previous version
openbase version restore 10.5.9 --type cli

# Restore a template
openbase version restore 2.0.0 --type sqlserver
openbase version restore 1.5.3 --type postgres
openbase version restore 0.0.2 --type oracle
```

The `--type` argument is required and accepts:

| Value       | Component                               |
|-------------|----------------------------------------|
| `cli`       | OpenBase CLI (`w3ti.OpenBase.CLI`)     |
| `sqlserver` | SQL Server Template                    |
| `postgres`  | PostgreSQL Template                    |
| `oracle`    | Oracle Template                        |

---

## Requirements

- .NET SDK 10 or higher

---

## Security & compatibility

- **Cross-platform**: Windows, macOS (Intel/Apple Silicon) and Linux
- **Security**: Process execution protected against command injection (S4036 compliance)
- Monitored by **SonarCloud**

---

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for the branch naming conventions, commit message format, and pull request workflow.

---

## License

Distributed under the MIT License. See `LICENSE.txt` for more information.

Developed by Rodrigo Brito <rodrigo@w3ti.com.br>.
