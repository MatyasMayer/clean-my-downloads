# clean-my-downloads

A small personal Windows 11 tool that keeps `C:\Users\<you>\Downloads` tidy. It sorts files by **subject → topic → type**, never overwrites a file, recycles only byte-identical duplicates (kept 30 days in its own Trash), and can undo its last run. It runs hourly in the background and shows no window.

- Behaviour: [SPEC.md](SPEC.md)
- Vocabulary: [GLOSSARY.md](GLOSSARY.md)

## Install

Needs the .NET 10 runtime (SDK to build: `winget install Microsoft.DotNet.SDK.10`).

```powershell
powershell -File scripts\install.ps1
```

This copies the sorter to `%LOCALAPPDATA%\clean-my-downloads\`, writes default `settings.json`, registers an hourly Task Scheduler task and creates five Start-menu shortcuts: **Sort now**, **Preview sort**, **Undo last run**, **Show sort log**, **Edit settings**. `scripts\uninstall.ps1` removes all of it, but never your Downloads folders, settings or move log.

Without installing, point the sorter at any folder:

```powershell
dotnet run --project src\CleanMyDownloads.Sorter -- --preview <folder>
dotnet run --project src\CleanMyDownloads.Sorter -- --sort <folder>
dotnet run --project src\CleanMyDownloads.Sorter -- --undo <folder>
```

Tests: `dotnet test`, and `powershell -File tests\scripts\Test-InstallScripts.ps1` for the install scripts.

## How this project came to be

The whole project was built with Claude Code and the [mattpocock-skills](https://github.com/mattpocock/skills) plugin, in stages:

1. **Planning.** The project started with `wayfinder`, which created one planning map issue ([#1](https://github.com/MatyasMayer/clean-my-downloads/issues/1)) with a ticket per open question (layout, safety net, run schedule, subject codes, topic grouping, type table, tech stack…). Each question was settled in turn, and the answers were recorded on its ticket.
   - `grilling` and `domain-modeling` stress-tested the decisions and produced [GLOSSARY.md](GLOSSARY.md).
   - `research` looked at what real FIT subject codes look like (branch `research/subject-codes`).
   - `prototype` tried out the topic-grouping logic before it was built (branch `prototype/topic-grouping`).
2. **Spec.** The settled decisions were compiled into [SPEC.md](SPEC.md) and split into implementation tickets with blocking relationships (#12–#22).
3. **Implementation.** `implement-spec` worked through the ticket graph on one integration branch, with a subagent per ticket in its own worktree. Each subagent built its ticket test-first with `tdd`, and independent tickets ran in parallel.
4. **Review.** `code-review` checked the result against the repo's standards and against the spec. The findings were fixed in a final pass before merging.

The one hands-on ticket, the first real run on a real Downloads folder, was done by the author.
