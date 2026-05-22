# Contributing to OpenBase CLI

Thank you for your interest in contributing! This document describes the workflow, conventions, and technical guidelines we follow to keep the project organized.

---

## Table of Contents

1. [Before You Start](#before-you-start)
2. [Development Setup](#development-setup)
3. [Branch Naming](#branch-naming)
4. [Workflow](#workflow)
5. [Commit Messages](#commit-messages)
6. [Pull Requests](#pull-requests)
7. [Running the Tests](#running-the-tests)
8. [Project Structure](#project-structure)
9. [Localization](#localization)
10. [How to Add a New Command](#how-to-add-a-new-command)
11. [How to Add a New Extension](#how-to-add-a-new-extension)
12. [CI / Quality Gate](#ci--quality-gate)

---

## Before You Start

**Every contribution — bug fix or new feature — requires a GitHub issue.**

- **Bug?** Open an issue describing the problem, the expected behavior, and how to reproduce it.
- **Feature?** Open an issue describing what you want to add and why it is valuable.

Do not open a pull request without a linked issue. This keeps context permanently attached to the work and makes code review much easier.

---

## Development Setup

### Prerequisites

- [.NET SDK 10](https://dotnet.microsoft.com/download) or later
- Git

### Clone and build

```bash
git clone https://github.com/britors/OpenBase.CLI.git
cd OpenBase.CLI
dotnet build
```

### Run as a local tool

To test changes without publishing, install the CLI from the local source:

```bash
dotnet pack
dotnet tool install --global --add-source ./nupkg w3ti.OpenBase.CLI
```

Or update an existing global install:

```bash
dotnet pack
dotnet tool update --global --add-source ./nupkg w3ti.OpenBase.CLI
```

### Run the tests

```bash
dotnet test
```

---

## Branch Naming

| Type    | Pattern                  | Example                              |
|---------|--------------------------|--------------------------------------|
| Feature | `feature/<id>-<slug>`    | `feature/41-scaffold-auth-attribute` |
| Bug fix | `hotfix/<id>-<slug>`     | `hotfix/42-fix-jwt-inject-programcs` |

- `<id>` is the GitHub issue number.
- `<slug>` is a short, lowercase, hyphen-separated description.
- Always branch off `main`.

---

## Workflow

```
1. Open or pick an issue on GitHub
2. Create a branch from main
3. Implement your changes
4. Add or update tests
5. Add localization strings for any new user-facing text (EN, PT-BR, ES)
6. Bump the patch version in OpenBase.CLI.csproj
7. Run the test suite — all tests must pass
8. Push the branch and open a PR targeting main
9. After merge, delete the branch (local and remote)
```

### Version bump

Before pushing, increment the patch number in `OpenBase.CLI.csproj`:

```xml
<Version>10.9.8</Version>  →  <Version>10.9.9</Version>
```

---

## Commit Messages

Use the [Conventional Commits](https://www.conventionalcommits.org/) format:

```
<type>: <short description in imperative mood>
```

Common types: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`.

Examples:
```
feat: gera controller com [Authorize] quando JWT está instalado
fix: corrige injeção duplicada de UseAuthentication no Program.cs
test: adiciona cobertura para ProtectExistingControllers
```

- Write the message in the same language as the rest of the commit history (Portuguese is fine).
- Keep the subject line under 72 characters.
- Do not add `Co-Authored-By` trailers.

---

## Pull Requests

- Title should match the commit message style.
- Link the issue in the PR body (`Closes #<id>`).
- The PR description should explain **what** changed and **why**, not just list files.
- All CI checks and tests must be green before merge.

---

## Running the Tests

```bash
dotnet test
```

All 265+ tests must pass. Do not submit a PR with failing tests.

### Test categories

| Folder | What it covers |
|--------|----------------|
| `OpenBase.CLI.Tests/Commands/` | Command handler unit tests |
| `OpenBase.CLI.Tests/Helpers/` | Cross-cutting utility tests |
| `OpenBase.CLI.Tests/Syntax/` | Roslyn-based tests that parse every generated C# file and assert it compiles without syntax errors |

When you add or modify a code generator, add a corresponding **Syntax test** that validates the generated output using Roslyn. This is mandatory — the CI pipeline enforces it.

---

## Project Structure

```
OpenBase.CLI/
├── Commands/           # Command handlers (one file per top-level command)
│   ├── Extension/      # Sub-commands for `openbase extension`
│   ├── Procedure/      # Sub-commands for `openbase procedure`
│   └── Scaffold/       # Sub-commands for `openbase scaffold`
├── Helpers/            # Cross-cutting utilities (IO, execution, template rendering, ...)
├── Localization/       # SR.cs — IStrings interface + EN, PT-BR, ES implementations
├── Models/             # Shared value types and enums
├── shell/              # Shell wrapper scripts (fish, bash/zsh)
└── OpenBase.CLI.Tests/ # xUnit test suite
    ├── Commands/
    ├── Helpers/
    └── Syntax/         # Roslyn syntax validation tests
```

---

## Localization

Every string shown to the user must go through the localization layer. **Do not hardcode strings** in command handlers.

### How it works

`Localization/SR.cs` contains:

1. `IStrings` — the interface declaring every user-facing string property.
2. `EnStrings` — English implementation.
3. `PtBrStrings` — Brazilian Portuguese implementation.
4. `EsStrings` — Spanish implementation.
5. `SR` — static helper that resolves the current locale and exposes `SR.Strings`.

### Adding a new string

1. Add the property to `IStrings`:

```csharp
// SR.cs — IStrings
string MyNewMessage { get; }   // {0}=someParam
```

2. Implement it in all three classes in the same file:

```csharp
// EnStrings
public string MyNewMessage => "Something happened: {0}";

// PtBrStrings
public string MyNewMessage => "Algo aconteceu: {0}";

// EsStrings
public string MyNewMessage => "Algo ocurrió: {0}";
```

3. Use it in the command handler via `SR.Strings`:

```csharp
AnsiConsole.MarkupLine(string.Format(SR.Strings.MyNewMessage, param));
```

The PR will not be merged if a new `IStrings` property is missing from any of the three implementations.

---

## How to Add a New Command

1. **Open an issue** describing the command (name, arguments, behavior).

2. **Create the command class** in `Commands/` (or a subfolder for sub-commands):

```csharp
// Commands/MyNewCommand.cs
[Description("Short description shown in --help")]
public sealed class MyNewCommand : AsyncCommand<MyNewCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("-e|--entity")]
        [Description("Entity name (PascalCase)")]
        public string Entity { get; set; } = string.Empty;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        // validate → generate → report
        return 0;
    }
}
```

3. **Register the command** in `Program.cs`:

```csharp
app.AddCommand<MyNewCommand>("mynew");
```

4. **Add localization strings** for all user-facing output (see [Localization](#localization)).

5. **Add tests** in `OpenBase.CLI.Tests/Commands/` and, if the command generates C# files, a Syntax test in `OpenBase.CLI.Tests/Syntax/`.

6. **Update `README.md`** — add a row to the *Available commands* table and a usage section if warranted.

---

## How to Add a New Extension

Extensions live under `Commands/Extension/` and follow a consistent pattern.

1. **Open an issue** describing the extension (NuGet packages, generated files, `Program.cs` injections, `appsettings.json` keys).

2. **Create the extension handler** in `Commands/Extension/`:

```csharp
// Commands/Extension/MyExtensionHandler.cs
public static class MyExtensionHandler
{
    public static async Task<int> HandleAsync(string cwd, string? provider)
    {
        // 1. Detect project structure
        // 2. Add NuGet packages to correct .csproj files
        // 3. Generate source files
        // 4. Inject into Program.cs
        // 5. Inject into appsettings.json
        // 6. Register in .openbase/extensions.json
        return 0;
    }
}
```

3. **Register the name** in the extension name list so `openbase extension list` picks it up and duplicate-install prevention works.

4. **Add localization strings** for setup messages and errors.

5. **Add tests** for the handler and a Syntax test for each generated file.

6. **Update `README.md`** — add a row to the *Available extensions* table and document the generated files, `appsettings.json` keys, and `Program.cs` injections.

---

## CI / Quality Gate

Every push and pull request runs three GitHub Actions workflows:

| Workflow | File | What it does |
|----------|------|--------------|
| CI | `.github/workflows/ci.yml` | Builds, runs all tests, uploads results to SonarCloud |
| Publish | `.github/workflows/publish.yml` | Publishes to NuGet on push to `main` |
| Main | `.github/workflows/main.yml` | Additional pipeline steps on `main` |

**SonarCloud quality gate** must pass before a PR can be merged. Common gate failures:

- Uncovered new code (add tests).
- Duplicated blocks (refactor into a shared helper).
- Security hotspots (review and mark as safe or fix).

You can preview the SonarCloud analysis for your branch at [sonarcloud.io](https://sonarcloud.io/summary/new_code?id=britors_OpenBase.CLI) after the CI run completes.

---

## Questions

Open a [GitHub Discussion](../../discussions) or an issue with the `question` label.
