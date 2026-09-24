# NetAI.ResxTranslator

> AI-powered automatic translation of missing `.resx` entries in .NET projects — integrated as a NuGet package and MSBuild task.

![.NET](https://img.shields.io/badge/.NET-Standard%202.0-blue)
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

**NetAI.ResxTranslator** is a NuGet package that can be integrated into any .NET project.  
During build or publish, it automatically detects missing translations in `.resx` files and generates them using AI.

Configuration is done via an `aisettings.json` file with a JSON schema.  
The package injects the configuration file and schema into the target project if they do not already exist.

Supported:

- Cloud AI models (e.g., OpenAI-compatible)
- Local models
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

### `NetAI.ResxTranslator.Tasks`

Contains the MSBuild task:

- `ResxTranslatorTask` – executed during build/publish
- Loads `aisettings.json`
- Checks the configured mode
- Calls the orchestrator
- Writes MSBuild logs and warnings

---

## Features

- Recursively finds all `.resx` files – excluding `bin`, `obj`, and `.git`
- Detects languages from file names, e.g., `Resources.de.resx`
- Creates missing `.resx` files for all `supportedLanguages`
- Translates only missing entries
- Batch processing with 20 entries per request
- One-time domain analysis for better contextual understanding
- Preserves technical placeholders like `{0}`, `{name}`, `%s`, `\n`, `\t`
- Mode control: `all`, `debug`, `release`, `publish`
- Multi-targeting capable – translation runs only in the first target framework
- `TaskHostFactory` prevents DLL locking in Visual Studio
- Can be disabled via `-p:ResxTranslatorEnabled=false`
- Schema and sample configuration are automatically copied to the project
- **No telemetry – zero data collection**

---

## Installation

```bash
dotnet add package NetAI.ResxTranslator
```

Or via `PackageReference`:

```xml
<PackageReference Include="NetAI.ResxTranslator" Version="1.0.0" />
```

On first build, the following files are copied to the project directory if they are missing:

- `aisettings.json`
- `aisettings-schema.json`

---

## Quick Start

1. Install the package.
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
| `apiKey` | string | API key for cloud AI. Leave empty for local models. | `""` |
| `context` | string | Optional context for translation | `""` |
| `glossaryPath` | string | Path to a glossary | `""` |
| `defaultLanguage` | string | Source language, e.g., `en` | `en` |
| `supportedLanguages` | array | Target languages, e.g., `["de", "fr", "it"]` | `[]` |

### `aiConfiguration`

| Field | Type | Description |
| --- | --- | --- |
| `model` | string | AI model, e.g., `gpt-4o` or a local model |
| `temperature` | number | Creativity of the AI, e.g., `0.2` |
| `systemPrompt` | string | Optional global system prompt |

> [!NOTE]
> The actual translation prompts are generated in `TranslationPromptBuilder`. The `systemPrompt` can serve as a global hint.

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
7. Performs a domain analysis.
8. Translates in batches of 20 entries.
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

Technical placeholders are preserved:

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
| API error | Check `apiKey`, model, and network. |
| DLL lock in Visual Studio | `TaskHostFactory` is already active. |
| WPF temp project | Automatically skipped. |
| Placeholders corrupted | Check prompt rules, possibly change model. |

---

## Security & Privacy

- **No telemetry:** This package does **not** collect, transmit, or store any telemetry data. No usage statistics, no analytics, no tracking. Your data stays within your project and your chosen AI provider.
- Never commit `apiKey` to Git.
- Use environment variables, User Secrets, or CI secrets.
- For local models, `apiKey` can remain empty.
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

```bash
dotnet test
```

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