# Contributing to OpenBase CLI

Thank you for your interest in contributing! This document describes the workflow we follow to keep the project organized as it grows.

---

## Before You Start

**Every contribution — bug fix or new feature — requires a GitHub issue.**

- **Bug?** Open an issue describing the problem, the expected behavior, and how to reproduce it.
- **Feature?** Open an issue describing what you want to add and why it is valuable.

Do not open a pull request without a linked issue. This keeps context permanently attached to the work and makes code review much easier.

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
4. Bump the patch version in OpenBase.CLI.csproj
5. Run the test suite — all tests must pass
6. Push the branch and open a PR targeting main
7. After merge, delete the branch (local and remote)
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

---

## Project Structure

```
OpenBase.CLI/
├── Commands/           # Command handlers (Scaffold, Extension, ...)
├── Helpers/            # Cross-cutting utilities (IO, Execution, ...)
├── Localization/       # SR.cs — EN, PT-BR and ES string resources
├── Models/             # Shared value types
└── OpenBase.CLI.Tests/ # xUnit test suite
```

When adding a new user-facing string, add it to `IStrings` and implement it in all three classes (`EnStrings`, `PtBrStrings`, `EsStrings`).

---

## Questions

Open a [GitHub Discussion](../../discussions) or an issue with the `question` label.
