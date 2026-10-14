# Kasheif — Instructions for Claude Code

Before exploring this codebase, read:

- [context/project-overview.md](context/project-overview.md) — what this project is, its features, architecture, and key workflows.
- [context/coding-standards.md](context/coding-standards.md) — naming, structure, style, and error-handling conventions to follow when writing or reviewing code here.

These two files are the fast path to understanding this project. Read them first instead of scanning the codebase from scratch — they cover most of what you need to plan a change before touching any source file.

## When to go beyond `/context`
- `/context` is a summary, not a spec. For exact method signatures, current field lists, or the precise state of a specific file, read the actual source — don't assume the summary is byte-accurate.
- For fuller engineering/business background than the overview covers, see [project.md](project.md) (architecture reference) and [BusinessContext.md](BusinessContext.md) (business/domain context) — the two source documents `context/project-overview.md` was distilled from.
- If a change you make contradicts something in `/context` (a new architectural exception, a renamed convention, a behavior change), update the relevant `/context` file as part of that change. It's only useful if it stays current.

## Quick Facts
- Solution `Kasheif`, namespace `HalalStocks` — ASP.NET Core 8 MVC, EF Core 8 + SQL Server, server-rendered Razor (no SPA). No automated tests, no CI/CD.
- Common commands: `dotnet build`, `dotnet ef migrations add <Name>`, `dotnet ef database update`, `dotnet run`.
- Production deploys to SmarterASP.NET (shared IIS hosting); DB migrations are applied separately from publishing the app binaries.