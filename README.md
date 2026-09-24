# NetAI.ResxTranslator

> AI-powered automatic translation of missing `.resx` entries in .NET projects — integrated through an MSBuild task.

![.NET](https://img.shields.io/badge/.NET-netstandard2.0%20%7C%20net8.0%20%7C%20net9.0%20%7C%20net10.0-blue)
![MSBuild](https://img.shields.io/badge/MSBuild-Task-green)
![AI](https://img.shields.io/badge/AI-Translation-purple)
![License](https://img.shields.io/badge/License-MIT-yellow)

> [!NOTE]
> This NuGet package is the foundation of an **enterprise version** with additional pro features. These include support for **JSON, YAML, and YML** files, **self-hosted translations** where a manager can approve or edit translations, **glossary management**, and more.  
> Learn more at [https://emineo-net.github.io](https://emineo-net.github.io).

## Table of Contents

- [Overview](#overview)
- [Components](#components)
- [Features](#features)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Configuration](#configuration)
- [Modes](#modes)
- [How It Works](#how-it-works)
- [Prompt Format](#prompt-format)
- [Placeholders](#placeholders)
- [MSBuild Integration](#msbuild-integration)
- [Troubleshooting](#troubleshooting)
- [Security & Privacy](#security--privacy)
- [Development](#development)
- [License](#license)

---

## Overview

**NetAI.ResxTranslator** is an MSBuild-integrated translation component consisting of a Core library and an MSBuild Tasks library.  
During build or publish, it detects missing translations in `.resx` files and generates them through a local OpenAI-compatible LLM endpoint.

Configuration is done via an `aisettings.json` file with a JSON schema.  
The package injects the configuration file and schema into the target project if they do not already exist.

Supported:

- Local OpenAI-compatible models through `http://localhost:8080/v1/chat/completions`
- Multiple target languages
- Mode-dependent execution (`debug`, `release`, `publish`, `all`)
- Multi-targeting
- Automatic creation of missing language files

---

## Components

The project consists of two libraries:

### `NetAI.ResxTranslator.Core`

Contains the core logic:

- `ProjectResxAnalyzer` – finds, reads, writes, and creates `.resx` files
- `ResxTranslationOrchestrator` – orchestrates the entire translation process
- `TranslationPromptBuilder` – generates domain and batch prompts
- `TranslatorLanguageResolver` – validates and normalizes language codes
- Models and configuration classes

Target frameworks: `netstandard2.0`, `net8.0`, `net9.0`, and `net10.0`.  
Project dependencies: `Microsoft.Bcl.Memory` and `Newtonsoft.Json`.

### `NetAI.ResxTranslator.Tasks`

Contains the MSBuild task:

- `ResxTranslatorTask` – executed during build/publish
- Loads `aisettings.json`
- Checks the configured mode
- Calls the orchestrator
- Writes MSBuild logs and warnings

Target framework: `netstandard2.0`.  
The project references Core and `Microsoft.Build.Utilities.Core`, and packages the MSBuild targets file,
settings files, task assembly, and copied dependencies.

---

## Features

- Recursively finds all `.resx` files – excluding `bin`, `obj`, and `.git`
- Detects languages from file names, e.g., `Resources.de.resx`
- Creates missing `.resx` files for all `supportedLanguages`
- Translates only missing entries
- Batch processing with 20 entries per request
- Per-request domain analysis for better contextual understanding
- Instructs the model to preserve technical placeholders like `{0}`, `{name}`, `%s`, `\n`, and `\t`
- Mode control: `all`, `debug`, `release`, `publish`
- Multi-targeting capable – translation runs only in the first target framework
- `TaskHostFactory` prevents DLL locking in Visual Studio
- Can be disabled via `-p:ResxTranslatorEnabled=false`
- Schema and sample configuration are automatically copied to the project
- **No telemetry – zero data collection**

---

## Installation

> **TODO:** Verify the published package ID before documenting an installation command. The current
> project files do not declare a `PackageId`.

> **TODO:** Add the verified `PackageReference` only after the published package ID and version
> are confirmed. They are not declared in the reviewed project files.

On first build, the following files are copied to the project directory if they are missing:

- `aisettings.json`
- `aisettings-schema.json`

---

## Quick Start

1. Install the published package after verifying its package ID.
2. Build the project.
3. Adjust `aisettings.json`.
4. Build again – missing translations will be added.

Example `aisettings.json`:

```json
{
  "$schema": "aisettings-schema.json",
  "version": "1.1",
  "translator": {
    "mode": "all",
    "apiKey": "",
    "context": "",
    "glossaryPath": "",
    "defaultLanguage": "en",
    "supportedLanguages": [ "de", "fr", "it" ]
  },
   "environment": {
      "targetDotNetVersion": "net9.0",
      "testProjectName": "{ProjectName}.Tests"
   },
   "frameworks": {
      "testFramework": "xunit",
      "mockingFramework": "moq",
      "useFluentAssertions": true,
      "useAutoFixture": true
   },
   "codeStyle": {
      "useFileScopedNamespace": true,
      "useAsyncSuffix": true,
      "maxLineLength": 120
   },
   "generationBehavior": {
      "testStrategy": "Both",
      "splitTestsByMethod": false,
      "maxTestsPerClass": 15
   },
  "aiConfiguration": {
    "model": "gpt-4o",
    "temperature": 0.2,
    "systemPrompt": "You are an expert .NET developer. Write clean, maintainable code following Clean Code principles. Always use the Arrange-Act-Assert (AAA) pattern."
  }
}
```

---

## Configuration

### `translator`

| Field | Type | Description | Default |
| --- | --- | --- | --- |
| `mode` | string | `all`, `debug`, `release`, `publish`, or a comma-separated combination | `all` |
| `apiKey` | string | Configured API key field; not consumed by the current local translation path | `""` |
| `context` | string | Configured context field; not consumed by the current translation path | `""` |
| `glossaryPath` | string | Configured glossary path field; not consumed by the current translation path | `""` |
| `defaultLanguage` | string | Source language, e.g., `en` | `en` |
| `supportedLanguages` | array | Target languages, e.g., `["de", "fr", "it"]` | `[]` |

### `aiConfiguration`

| Field | Type | Description |
| --- | --- | --- |
| `model` | string | Configured model field; not consumed by the current local translation path |
| `temperature` | number | Configured temperature field; not consumed by the current local translation path |
| `systemPrompt` | string | Configured system prompt field; not consumed by the current local translation path |

> [!NOTE]
> The actual translation prompts are generated in `TranslationPromptBuilder` and sent to the local
> endpoint at `http://localhost:8080/v1/chat/completions`. The `aiConfiguration` values are currently
> loaded but are not consumed by the translation execution path.

---

## Modes

The mode is controlled via `translator.mode`.

| Mode | Meaning |
| --- | --- |
| `all` | Always run |
| `debug` | Only on Debug build and not on Publish |
| `release` | Only on Release build and not on Publish |
| `publish` | Only on Publish |

Multiple modes can be combined:

```json
"mode": "debug,publish"
```

---

## How It Works

### MSBuild Targets

The file `NetAI.ResxTranslator.Tasks.targets` controls the integration:

1. **`SetupResxTranslatorSettings`**  
   Copies `aisettings.json` and `aisettings-schema.json` into the project if they are missing.  
   Adds them as `None` items with `CopyToOutputDirectory`.

2. **`RunResxTranslation`**  
   Calls the `ResxTranslatorTask` – depending on the mode and only in the first target framework.

3. **`IncludeGeneratedResx`**  
   Includes newly created `.resx` files as `EmbeddedResource` within the same build.

4. **`RunResxTranslationOnPublish`**  
   Fallback for Visual Studio Publish if `_IsPublishing` is not set.

### Task

The `ResxTranslatorTask`:

- Loads `aisettings.json`
- Checks the configured mode against `Configuration` and `IsPublishing`
- Skips WPF temp projects (`*_wpftmp`)
- Calls the `ResxTranslationOrchestrator`
- Logs errors, warnings, and information to MSBuild

### Orchestrator

The `ResxTranslationOrchestrator`:

1. Finds all `.resx` files.
2. Reads them into `ResxFileInfo` objects.
3. Resolves default and supported languages.
4. Creates missing language files via `EnsureSupportedLanguageFiles`.
5. Collects missing translations per file.
6. Determines the source text via `FindSourceTextForKey`.
7. Sends the source strings to the local OpenAI-compatible endpoint for domain analysis.
8. Translates in batches of 20 entries through the same local endpoint.
9. Parses the AI response in the format `[KEY:...] Translation`.
10. Saves the translations to the `.resx` file.

### PromptBuilder

The `TranslationPromptBuilder`:

- Generates a prompt for domain analysis.
- Splits entries into batches of 20.
- Creates strict formatting rules for the AI.
- Uses a standard or alternative prompt depending on the key style.

### LanguageResolver

The `TranslatorLanguageResolver`:

- Validates culture codes via `CultureInfo`.
- Normalizes `defaultLanguage`.
- Filters invalid `supportedLanguages`.
- Fallback is `en`.

---

## Prompt Format

The AI receives input in the following format:

```text
[KEY:Buttons.Cancel] ||| Cancel
[KEY:Messages.WelcomeUser] ||| Welcome back, {0}!
```

Expected output:

```text
[KEY:Buttons.Cancel] Abbrechen
[KEY:Messages.WelcomeUser] Willkommen zurück, {0}!
```

Rules:

- Each line starts with `[KEY:...]`.
- The key is not changed.
- No Markdown code blocks.
- No explanations.
- Only the raw translation lines.

---

## Placeholders

The AI is instructed to preserve technical placeholders:

- `{0}`, `{name}`
- `%s`
- `\n`, `\t`
- HTML/XML fragments, if present

The AI is instructed not to translate or alter placeholders.

---

## MSBuild Integration

### Disable

```bash
dotnet build -p:ResxTranslatorEnabled=false
```

### Multi-Targeting

With multiple target frameworks, translation runs only in the first TFM:

```xml
<TargetFrameworks>net8.0;net48</TargetFrameworks>
```

### DLL Lookup

The task searches for the assembly:

1. In the NuGet package under `..\tasks\netstandard2.0\NetAI.ResxTranslator.Tasks.dll`
2. Locally in the `build` folder

### Warning for Missing Assembly

If the task DLL is not found, the following appears:

```text
RESXT001: NetAI.ResxTranslator: Task assembly not found.
```

---

## Troubleshooting

| Problem | Solution |
| --- | --- |
| `RESXT001` | Check that the NuGet package is installed correctly. |
| No translation | Check `translator.mode`. |
| No `.resx` found | Are the files in the project folder? |
| Invalid language codes | Use culture codes, e.g., `de`, `fr`, `it`. |
| API error | Check the local endpoint at `http://localhost:8080/v1/chat/completions` and its network availability. |
| DLL lock in Visual Studio | `TaskHostFactory` is already active. |
| WPF temp project | Automatically skipped. |
| Placeholders corrupted | Check prompt rules, possibly change model. |

---

## Security & Privacy

- **No telemetry:** This package does **not** collect, transmit, or store any telemetry data. No usage statistics, no analytics, no tracking. Your data stays within your project and your chosen AI provider.
- The configured `apiKey` field is not consumed by the current local translation path.
- Review AI translations before release, especially for technical content.
- Enterprise customers can rely on a completely transparent, telemetry-free integration.

---

## Development

### Prerequisites

- .NET SDK
- MSBuild
- Visual Studio or JetBrains Rider

### Build

```bash
dotnet build
```

### Test

No test project or test sources are contained in either reviewed project, so `dotnet test` is not
a project-specific command for them.

### Run

These projects are a class library and an MSBuild task library. They do not contain a standalone
executable or a `Program.cs` entry point, so there is no standalone run command.

### Use Local DLL

The targets support a local DLL in the `build` folder:

```text
build/NetAI.ResxTranslator.Tasks.dll
```

---

## License

This project is licensed under the **MIT License**.

See the [LICENSE](LICENSE) file for details.

**No telemetry. No data collection. Enterprise-ready.**