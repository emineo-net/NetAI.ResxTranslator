# NetAI.ResxTranslator.Tasks

[![NuGet](https://img.shields.io/nuget/v/NetAI.ResxTranslator.Tasks.svg?label=NuGet)](https://www.nuget.org/packages/NetAI.ResxTranslator.Tasks)
[![NuGet Downloads](https://img.shields.io/nuget/dt/NetAI.ResxTranslator.Tasks.svg)](https://www.nuget.org/packages/NetAI.ResxTranslator.Tasks)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

> **The build task.** Add one NuGet package, point it at any OpenAI-compatible LLM, and every missing `.resx` translation fills itself in *before your code compiles*.

This README covers the **MSBuild task / NuGet package**. For the full project overview, see the [solution README](../../../README.md). If you'd rather translate on the command line, use the sibling [`NetAI.ResxTranslator.Cli`](../NetAI.ResxTranslator.Cli/README.md) package.

---

## Table of Contents

- [Why a Build Task?](#why-a-build-task)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Build Integration](#build-integration)
- [Build-Time Switches](#build-time-switches)
- [Mode-Based Execution](#mode-based-execution)
- [Multi-Targeting & WPF](#multi-targeting--wpf)
- [Troubleshooting](#troubleshooting)
- [Configuration](#configuration)
- [License](#license)

---

## Why a Build Task?

Because localization shouldn't be a separate step. Ever.

A build task means:

- **No CLI to remember.** `dotnet build` is the whole workflow.
- **No pre-commit hook to maintain.** The task runs on every build where it's active.
- **No CI glue.** Whatever runs your build runs the translator.
- **Nothing to forget.** If a developer adds a string to the neutral `.resx` and forgets the German one, the next build fixes it — before the code compiles, before the pull request, before anyone in Frankfurt sees an English error message.

---

## Installation

```bash
dotnet add package NetAI.ResxTranslator.Tasks
```

Or add the reference directly to your `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="NetAI.ResxTranslator.Tasks" Version="1.0.*" PrivateAssets="all" />
</ItemGroup>
```

The package injects its `.targets` file automatically. On the **first build**, two files are copied into your project root if they don't already exist:

- **`aisettings.json`** — the configuration file *(commit this)*.
- **`aisettings-schema.json`** — a JSON Schema for editor IntelliSense *(commit this)*.

Both are added to the project as `None` items with `CopyToOutputDirectory=PreserveNewest`.

---

## Quick Start

1. **Add the package** to your project.
2. **Build once** — `aisettings.json` is scaffolded into your project root.
3. **Edit `aisettings.json`** to point at your LLM (see [Configuration](#configuration) or the [solution README](../../../README.md#configuration--aisettingsjson)).
4. **Build again.** Done.

Your `de.resx`, `fr.resx`, `it.resx` files — even the ones that don't exist yet — will be created and filled in.

Minimal `aisettings.json`:

```json
{
  "$schema": "aisettings-schema.json",
  "version": "1.1",
  "translator": {
    "mode": "all",
    "defaultLanguage": "en",
    "supportedLanguages": ["de", "fr", "it"]
  },
  "aiConfiguration": {
    "baseUrl": "http://localhost:11434/",
    "model": "llama3.1",
    "temperature": 0.2,
    "timeoutMinutes": 5,
    "apiKey": ""
  }
}
```

---

## Build Integration

The `.targets` file that ships with the package is designed to be a **good citizen** in any project:

- **Design-time builds are skipped.** No interference with IntelliSense or the VS designer.
- **WPF-safe.** Projects ending in `_wpftmp` are silently ignored.
- **Multi-targeting aware.** Runs only for the first TFM in `TargetFrameworks`.
- **`dotnet publish` aware.** Detects `_IsPublishing`; if not set (e.g., the VS Publish button), a fallback target runs `BeforeTargets="PrepareForPublish"`.
- **Task host isolation.** `TaskFactory="TaskHostFactory"` runs the task in a separate MSBuild process, so the task assembly is **never locked** by Visual Studio. You can rebuild the translator while VS is open.
- **Explicit warning if missing.** If the task assembly can't be located, `RESXT001` is emitted instead of silently doing nothing.

The pipeline runs **before compilation**, so newly created `.resx` files are added to `EmbeddedResource` and land in the assembly without a second build.

---

## Build-Time Switches

| Property | Effect |
|---|---|
| `ResxTranslatorEnabled=false` | Disables the translator for this build entirely. |
| `EnableDefaultEmbeddedResourceItems=false` | Disables the auto-include step for generated `.resx` files. |

Per project:

```xml
<PropertyGroup>
  <ResxTranslatorEnabled>false</ResxTranslatorEnabled>
</PropertyGroup>
```

Per invocation:

```bash
dotnet build -p:ResxTranslatorEnabled=false
```

---

## Mode-Based Execution

The `translator.mode` setting controls **when** the task runs:

| Mode value              | Behavior                                                              |
|-------------------------|-----------------------------------------------------------------------|
| `all`                   | Runs on every build (default).                                        |
| `debug`                 | Runs only on Debug builds (excluding publish).                        |
| `release`               | Runs only on Release builds (excluding publish).                      |
| `publish`               | Runs only when publishing.                                            |
| `debug,publish`         | Combined — comma-separated list of any of the above.                  |

When the mode doesn't match the current build state, the task logs a single `Skipped.` line and exits cleanly — no noise in your build log.

---

## Multi-Targeting & WPF

- **Multi-targeting:** When `TargetFrameworks` contains more than one TFM, the translator runs **only for the first TFM**. This avoids running the (potentially expensive) LLM translation multiple times per build.
- **WPF:** Projects whose name ends in `_wpftmp` (WPF's temporary build projects) abort silently. Nothing runs, nothing is logged.
- **Design-time:** `DesignTimeBuild=true` disables the task, so IntelliSense edits don't trigger translations.

---

## Troubleshooting

### `RESXT001` — Task-Assembly not found

```
warning RESXT001: NetAI.ResxTranslator: Task-Assembly nicht gefunden (...). Es wird nichts übersetzt.
```

The `.targets` file couldn't locate the task DLL. This usually means the NuGet package didn't restore correctly, or a local development build is missing its dependencies. Try:

```bash
dotnet restore --force
dotnet build
```

If you're using the package from a local feed, make sure the `.nupkg` was fully installed.

### "My translations aren't being written."

Check the following in order:

1. Is the translator enabled? Look for `🤖 [ResxTranslator] Mode condition met` in the build log. If it says `Skipped`, check [Mode-Based Execution](#mode-based-execution).
2. Is `aisettings.json` at the project root? The task searches upward from `ProjectDir` and stops at the first match.
3. Are there actually missing translations? Only entries with an empty `<value>` are translated.
4. Is the LLM reachable? A wrong `baseUrl` (or missing trailing `/`) will produce an error in the build log.

### "The build is slow."

That's the LLM, not the task. Options:

- Use a local model on a fast machine (Ollama with a 7B–8B model is plenty for UI strings).
- Switch `mode` to `publish` so translation only runs before deployment.
- Keep `supportedLanguages` focused — every declared language is a full pipeline run.

---

## Configuration

All behavior is driven by `aisettings.json` at the project root. The **full reference** — including all `aiConfiguration` variants (local, OpenAI, Azure, custom proxy) and rules — lives in the [solution README](../../../README.md#configuration--aisettingsjson).

Task-specific setting:

| Option                | Type     | Default | Description                                                       |
|-----------------------|----------|---------|-------------------------------------------------------------------|
| `translator.mode`     | string   | `all`   | `all` or comma-separated: `debug`, `release`, `publish`           |

---

## License

MIT — see the [LICENSE](../../../LICENSE) file.