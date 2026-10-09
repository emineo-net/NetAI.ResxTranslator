# NetAI.ResxTranslator

[![NuGet](https://img.shields.io/nuget/v/NetAI.ResxTranslator.Tasks.svg?label=NuGet)](https://www.nuget.org/packages/NetAI.ResxTranslator.Tasks)
[![NuGet Downloads](https://img.shields.io/nuget/dt/NetAI.ResxTranslator.Tasks.svg)](https://www.nuget.org/packages/NetAI.ResxTranslator.Tasks)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET Standard](https://img.shields.io/badge/.NET-netstandard2.0%20%7C%20net8.0%20%7C%20net9.0%20%7C%20net10.0-blueviolet)](https://learn.microsoft.com/dotnet/standard/net-standard)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](#contributing)

> ### Stop translating `.resx` files by hand.
> **NetAI.ResxTranslator** turns your build into a localization engine. Reference one NuGet package, point it at any OpenAI-compatible LLM — local or cloud — and every missing translation fills itself in *before your code compiles*.
>
> No CLI. No pre-commit hook. No CI glue. No copy-paste into ChatGPT.

---

## Table of Contents

- [Why NetAI.ResxTranslator?](#why-netairestranslator)
- [Features](#features)
- [How It Works](#how-it-works)
- [Requirements](#requirements)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Configuration — `aisettings.json`](#configuration--aisettingsjson)
  - [AI Connection Variants](#ai-connection-variants)
  - [Rules & Pitfalls](#rules--pitfalls)
- [Usage](#usage)
  - [As a Build Task](#as-a-build-task)
  - [Programmatic API](#programmatic-api)
- [Examples](#examples)
- [Configuration Reference](#configuration-reference)
- [Build Integration](#build-integration)
- [Repository Layout](#repository-layout)
- [Roadmap](#roadmap)
- [Enterprise](#enterprise)
- [License](#license)
- [Contributing](#contributing)
- [Acknowledgements](#acknowledgements)

---

## Why NetAI.ResxTranslator?

Localization is usually the last thing a .NET team wants to think about — and the first thing that breaks when a new string sneaks into the neutral `.resx` and nobody notices until a user in Frankfurt sees an English error message.

Traditional options are all painful:

- **Manual translation** → slow, error-prone, never in sync.
- **SaaS localization platforms** → expensive, another account, another vendor lock-in, another place your strings live.
- **Hand-rolled scripts** → a fragile mess of prompt-copying and XML surgery.

**NetAI.ResxTranslator** takes a different stance:

- **It lives where your strings live.** No export. No upload. No external dashboard.
- **It runs where your build runs.** A NuGet package and a JSON file. That's the entire integration.
- **It uses whatever model you want.** A 7B model on your laptop. A frontier model in the cloud. Your company's private gateway. The interface is identical.
- **It only translates what's missing.** Existing translations are untouched. Incremental by design.
- **It understands your domain.** Before translating, it asks the LLM *what kind of software this is* — so "Save" becomes "Speichern" in a CRM and "Sichern" in a mountaineering app.

The result: your `de.resx`, `fr.resx`, `ja.resx` files stay permanently in sync with the source — with **zero ongoing effort**.

> **Note:** This is the open-source **community edition**, released under the MIT License.
> An **Enterprise edition** with team collaboration, CI/CD analytics, and translation memory is planned — see [Enterprise](#enterprise).

---

## Features

- **`.resx`-aware analysis** — Discovers, parses, and understands your resource files, including satellite files per culture. Ignores `bin/`, `obj/`, and `.git/`.
- **AI-driven translation** — Fills in missing entries based on your default-language text. Already-translated entries are never re-sent.
- **Domain-aware prompting** — A one-shot analysis call detects the software's domain (e.g., *Medical Software*, *E-commerce UI*, *Automotive Infotainment*) and injects it into every subsequent translation prompt. Terminology feels native, not literal.
- **Batch processing** — Missing keys are grouped into batches of 20, so a single request covers many entries and token cost stays low.
- **Local *and* cloud LLMs** — Works with any OpenAI-compatible endpoint: llama.cpp, Ollama, LM Studio, OpenAI, Azure OpenAI, or your own corporate proxy. The configuration structure is identical.
- **Placeholder-safe** — `{0}`, `{name}`, `%s`, `\n`, `\t` are preserved verbatim. The model is instructed to treat them as immutable, and the response parser validates the output.
- **Automatic file creation** — Declare `supportedLanguages` and missing satellite `.resx` files are generated from the neutral resource on the fly.
- **Idempotent & incremental** — Only entries with empty values are translated. Re-running the build is cheap.
- **Glossary support** *(reserved)* — `glossaryPath` is already wired into the config schema for consistent terminology.
- **One config file** — All behavior is driven by `aisettings.json` with full JSON Schema validation in VS, Rider, and VS Code.
- **NuGet package** — One `<PackageReference>`. Automatic MSBuild injection via `.targets`.
- **Visual Studio friendly** — Uses `TaskHostFactory` so the task assembly is never locked by the IDE. Rebuild the translator while VS is open.
- **Safe by default** — Skips design-time builds, respects multi-targeting, and can be disabled per build with `-p:ResxTranslatorEnabled=false`.

---

## How It Works

On every build where the translator is active, this pipeline runs **before compilation**:

1. **Resource Discovery**
   `ProjectResxAnalyzer` scans the project directory for all `.resx` files and parses their entries, keyed by name.

2. **Language Resolution**
   `TranslatorLanguageResolver` validates `translator.defaultLanguage` and `translator.supportedLanguages` against `CultureInfo`. Invalid codes are reported and ignored — no silent misbehavior.

3. **Satellite Generation**
   For each resource base name, missing `<name>.<culture>.resx` files are created from the most complete sibling, with all values emptied.

4. **Gap Detection**
   Entries without a translation (`HasTranslation == false`) are collected. Everything else is skipped.

5. **Source Text Lookup**
   For each missing key, the source text is taken from the neutral file (or any sibling that has a value) — that's what gets sent to the LLM.

6. **Domain Analysis — LLM call #1**
   A single call sends up to 15 sample strings and asks the model for a short domain label. The result is sanitized and used to prime every following prompt.

7. **Batch Translation — LLM calls #2…n**
   Missing keys are grouped into batches of 20. Each batch is sent with a strict system prompt that demands the format:

   ```
   [KEY:identifier] <translation>
   ```

   The model is forbidden from emitting markdown fences, intros, explanations, or altered keys.

8. **Response Parsing**
   A tolerant regex extracts `[KEY:...]` pairs. Keys not requested in the current batch are discarded — an anti-hallucination guard.

9. **Write-Back**
   Translations are written back into the corresponding `.resx` file in place, preserving comments, headers, and the rest of the XML document. Keys that couldn't be translated are marked with a placeholder so nothing is silently lost.

10. **Re-Embedding**
    Newly created `.resx` files are added to `EmbeddedResource` before compilation, so they land in the assembly without a second build.

Progress, warnings, and errors surface as native MSBuild messages with the `ResxTranslator:` prefix — visible in your build log and in the Visual Studio Error List.

---

## Requirements

- .NET SDK **6.0 or later** for the host project. The task itself runs on `netstandard2.0` inside MSBuild's runtime.
- An **OpenAI-compatible endpoint**:
  - **Local**: [llama.cpp server](https://github.com/ggerganov/llama.cpp), [LM Studio](https://lmstudio.ai/), [Ollama](https://ollama.com/), …
  - **Hosted**: OpenAI, Azure OpenAI, or any gateway exposing `/v1/chat/completions`.

---

## Installation

```bash
dotnet add package NetAI.ResxTranslator.Tasks
```

Or add the reference directly to your `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="NetAI.ResxTranslator.Tasks" Version="1.1.*" PrivateAssets="all" />
</ItemGroup>
```

The package injects its `.targets` file automatically. On the **first build**, two files are copied into your project root if they don't already exist:

- **`aisettings.json`** — the configuration file *(commit this)*.
- **`aisettings-schema.json`** — a JSON Schema for editor IntelliSense *(commit this)*.

Both are added to the project as `None` items with `CopyToOutputDirectory=PreserveNewest`.

### Disabling the translator

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

## Quick Start

1. **Add the package** to your project.
2. **Build once** — `aisettings.json` is scaffolded into your project root.
3. **Edit `aisettings.json`** to point at your LLM (see [Configuration](#configuration--aisettingsjson)).
4. **Build again.** Done.

Your `de.resx`, `fr.resx`, `it.resx` files — even the ones that don't exist yet — will be created and filled in.

---

## Configuration — `aisettings.json`

All behavior is driven by a single JSON file at the project root. It's validated by `aisettings-schema.json` (JSON Schema, Draft 07), so editors like VS Code, Rider, and Visual Studio provide autocomplete and inline validation.

### Minimal example

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

All fields are always present. Unused fields stay at `""` (strings) or `0` (numbers) and are treated as *not set*. The loader replaces empty values with defaults before validation, so the file above connects to Ollama on port 11434 with a 5-minute timeout and no authentication.

### AI Connection Variants

The `aiConfiguration` block drives the connection to the LLM. The same structure works for local and hosted endpoints — only the values change.

**Required fields**

| Field         | Type     | Description                                      |
|---------------|----------|--------------------------------------------------|
| `model`       | `string` | Model name, e.g. `gpt-4o`, `llama3.1`            |
| `temperature` | `number` | Creativity (0.0 = deterministic, 2.0 = maximum)  |

**Optional fields** — set to `""` or `0` if unused

| Field            | Type     | Default (when empty)     | Description                                                              |
|------------------|----------|--------------------------|--------------------------------------------------------------------------|
| `baseUrl`        | `string` | `http://localhost:8080/` | Endpoint URL — **must end with `/`**                                      |
| `timeoutMinutes` | `integer`| `5`                      | HTTP timeout in minutes                                                   |
| `systemPrompt`   | `string` | *(none)*                 | System role and rules for the model                                       |
| `apiKey`         | `string` | *(none)*                 | API key directly in the file — **mutually exclusive with `apiKeyEnvVar`**  |
| `apiKeyEnvVar`   | `string` | *(none)*                 | Name of the env var holding the key — **mutually exclusive with `apiKey`** |

> **Rule of thumb:** `apiKey` = the key itself. `apiKeyEnvVar` = only the **name** of the environment variable that holds the key. Only one of the two may be non-empty.

#### Variant 1 — Local server, no authentication

```json
"aiConfiguration": {
  "baseUrl": "http://localhost:8080/",
  "model": "gpt-4o",
  "temperature": 0.2,
  "timeoutMinutes": 5,
  "systemPrompt": "",
  "apiKey": "",
  "apiKeyEnvVar": ""
}
```

#### Variant 2 — Local server on a different port (e.g. Ollama)

```json
"aiConfiguration": {
  "baseUrl": "http://localhost:11434/",
  "model": "llama3.1",
  "temperature": 0.2,
  "timeoutMinutes": 10,
  "systemPrompt": "",
  "apiKey": "",
  "apiKeyEnvVar": ""
}
```

> The trailing slash on `baseUrl` is required — the schema enforces it.
> Local servers often ignore `model`; setting it anyway is harmless.

#### Variant 3 — Local server with token

```json
"aiConfiguration": {
  "baseUrl": "http://localhost:11434/",
  "model": "llama3.1",
  "temperature": 0.2,
  "timeoutMinutes": 5,
  "systemPrompt": "",
  "apiKey": "local-secret",
  "apiKeyEnvVar": ""
}
```

> ⚠️ **Do not commit real values** into version control. Prefer Variant 4.

#### Variant 4 — Hosted API via environment variable *(recommended)*

The key lives outside the file and is read at runtime. This makes `aisettings.json` safe to commit.

```json
"aiConfiguration": {
  "baseUrl": "https://api.openai.com/",
  "model": "gpt-4o",
  "temperature": 0.2,
  "timeoutMinutes": 5,
  "systemPrompt": "You are an expert translator for software UI strings. Preserve placeholders like {0}, {1}, and keep translations concise.",
  "apiKey": "",
  "apiKeyEnvVar": "OPENAI_API_KEY"
}
```

Set the environment variable:

**Windows (PowerShell, persistent):**
```powershell
setx OPENAI_API_KEY "sk-…"
```

**Linux / macOS:**
```bash
export OPENAI_API_KEY="sk-…"
```

**GitHub Actions:**
```yaml
env:
  OPENAI_API_KEY: ${{ secrets.OPENAI_API_KEY }}
```

**Docker:**
```bash
docker run -e OPENAI_API_KEY="sk-…" your-image
```

#### Variant 5 — Azure OpenAI

```json
"aiConfiguration": {
  "baseUrl": "https://<your-resource>.openai.azure.com/",
  "model": "gpt-4o",
  "temperature": 0.2,
  "timeoutMinutes": 5,
  "systemPrompt": "",
  "apiKey": "",
  "apiKeyEnvVar": "AZURE_OPENAI_KEY"
}
```

#### Variant 6 — Custom proxy / corporate endpoint

```json
"aiConfiguration": {
  "baseUrl": "https://llm.company.local/",
  "model": "internal-model",
  "temperature": 0.2,
  "timeoutMinutes": 15,
  "systemPrompt": "",
  "apiKey": "",
  "apiKeyEnvVar": "INTERNAL_LLM_TOKEN"
}
```

### Rules & Pitfalls

- **`apiKey` and `apiKeyEnvVar` are mutually exclusive.** Setting both to non-empty values produces a validation error. Leaving both at `""` is valid and means *no authentication*.
- **Empty strings mean *not set*.** `"baseUrl": ""` falls back to `http://localhost:8080/`. `"timeoutMinutes": 0` falls back to `5`.
- **`baseUrl`, if given, must end with `/`.** `http://localhost:11434` is invalid; `http://localhost:11434/` is correct.
- **Env-var names are case-sensitive** on Linux and macOS. `OPENAI_API_KEY` ≠ `openai_api_key`.
- **If the environment variable is not set**, no `Authorization` header is sent. Cloud providers will return `401`; local servers without auth behave as expected.
- **Never commit secrets.** Prefer Variant 4 for cloud endpoints.

---

## Usage

### As a Build Task

Once configured, the translator runs automatically during `dotnet build` and `dotnet publish`. It will:

- Scan the project directory for `.resx` files
- Detect entries without a translation
- Send them to the configured LLM in batches
- Write translations back into the corresponding `.resx` file
- Re-embed newly created `.resx` files before compilation
- Skip entries that are already translated

No CLI, no scripts, no CI glue. Just build.

### Programmatic API

You can also drive the translator directly from your own code:

```csharp
using NetAI.ResxTranslator.Core;
using NetAI.ResxTranslator.Core.Config;

var config = AiSettingsLoader.Load(@"C:\MyProject");

var orchestrator = new ResxTranslationOrchestrator();

ResxTranslationResult result = await orchestrator.ProcessProject(
    projectDir: @"C:\MyProject",
    settings: config.Translator!,
    logInfo: msg => Console.WriteLine(msg));

if (!result.Success)
{
    Console.Error.WriteLine(result.ErrorMessage);
}
```

> **Note:** `ResxTranslationOrchestrator` is the current entry point. The name may change in future releases to better reflect its purpose.

---

## Examples

### Input passed to the model

```
[KEY:1] Greeting_Label ||| Hello, world!
[KEY:2] Save_Button ||| Save
[KEY:3] Error_FileNotFound ||| The file '{0}' could not be found.
```

### Output returned by the model

```
[KEY:1] Hallo, Welt!
[KEY:2] Speichern
[KEY:3] Die Datei '{0}' wurde nicht gefunden.
```

### Snippet from the resulting `.resx`

```xml
<data name="Greeting_Label" xml:space="preserve">
  <value>Hallo, Welt!</value>
</data>
<data name="Save_Button" xml:space="preserve">
  <value>Speichern</value>
</data>
<data name="Error_FileNotFound" xml:space="preserve">
  <value>Die Datei '{0}' wurde nicht gefunden.</value>
</data>
```

---

## Configuration Reference

### Top-level

| Option    | Type   | Default | Description                  |
|-----------|--------|---------|------------------------------|
| `version` | string | `1.1`   | Configuration format version |

### `translator`

| Option                | Type     | Default | Description                                                       |
|-----------------------|----------|---------|-------------------------------------------------------------------|
| `mode`                | string   | `all`   | `all` or a comma-separated list: `debug`, `release`, `publish`    |
| `apiKey`              | string   | `""`    | **Deprecated** — use `aiConfiguration.apiKey` / `apiKeyEnvVar`    |
| `context`             | string   | `""`    | Additional context for the translator (industry, tone, …)         |
| `glossaryPath`        | string   | `""`    | Path to a glossary file for consistent terminology                |
| `defaultLanguage`     | string   | `en`    | Culture code of the neutral `.resx` files                         |
| `supportedLanguages`  | string[] | `[]`    | Target culture codes, e.g. `["de", "fr"]`                         |

### `aiConfiguration`

| Option           | Type     | Default (empty)          | Description                                                              |
|------------------|----------|--------------------------|--------------------------------------------------------------------------|
| `baseUrl`        | string   | `http://localhost:8080/` | OpenAI-compatible endpoint — must end with `/`                           |
| `model`          | string   | `gpt-4o`                 | Model identifier                                                         |
| `temperature`    | number   | `0.2`                    | Sampling temperature (0.0–2.0)                                           |
| `timeoutMinutes` | integer  | `5`                      | HTTP timeout in minutes                                                  |
| `systemPrompt`   | string   | *(none)*                 | System prompt for the model                                              |
| `apiKey`         | string   | *(none)*                 | Inline API key (mutually exclusive with `apiKeyEnvVar`)                  |
| `apiKeyEnvVar`   | string   | *(none)*                 | Environment variable holding the API key (mutually exclusive with `apiKey`) |

---

## Build Integration

The `.targets` file that ships with the package is designed to be a **good citizen** in any project:

- **Design-time builds are skipped.** No interference with IntelliSense or the VS designer.
- **WPF-safe.** Projects ending in `_wpftmp` are silently ignored.
- **Multi-targeting aware.** Runs only for the first TFM in `TargetFrameworks`.
- **`dotnet publish` aware.** Detects `_IsPublishing`; if not set (e.g., the VS Publish button), a fallback target runs `BeforeTargets="PrepareForPublish"`.
- **Task host isolation.** `TaskFactory="TaskHostFactory"` runs the task in a separate MSBuild process, so the task assembly is **never locked** by Visual Studio. You can rebuild the translator while VS is open.
- **Explicit warning if missing.** If the task assembly can't be located, `RESXT001` is emitted instead of silently doing nothing.

### Build-time switches

| Property | Effect |
|---|---|
| `ResxTranslatorEnabled=false` | Disables the translator for this build entirely. |
| `EnableDefaultEmbeddedResourceItems=false` | Disables the auto-include step for generated `.resx` files. |

### Mode-based execution

The `translator.mode` setting controls when the translator runs:

| Mode value              | Behavior                                                              |
|-------------------------|-----------------------------------------------------------------------|
| `all`                   | Runs on every build (default).                                        |
| `debug`                 | Runs only on Debug builds (excluding publish).                        |
| `release`               | Runs only on Release builds (excluding publish).                      |
| `publish`               | Runs only when publishing.                                            |
| `debug,publish`         | Combined — comma-separated list of any of the above.                  |

---

## Repository Layout

```
NetAI.ResxTranslator.Core/
  Config/                  AiTestingConfig, LlmConnectionSettings, TranslatorConfig, AiSettingsLoader
  Models/                  ResxFileInfo, ResxEntry, ApiTranslationRequest/Response
  ProjectResxAnalyzer.cs   Discovery, parsing, writing, satellite file creation
  ResxTranslationOrchestrator.cs   End-to-end pipeline
  TranslationPromptBuilder.cs      Domain analysis + batch prompt construction
  LocalLlmClient.cs        OpenAI-compatible HTTP client
  TranslatorLanguageResolver.cs    Culture-code validation and normalization

NetAI.ResxTranslator.Tasks/
  ResxTranslatorTask.cs    MSBuild task entry point
  build/NetAI.ResxTranslator.Tasks.targets
  aisettings.json          Default config shipped with the package
  aisettings-schema.json   JSON Schema shipped with the package
```

---

## Roadmap

- Publish stable NuGet package
- Add comprehensive MSBuild task documentation
- **Translation memory** — avoid retranslating identical strings across builds
- **Glossary validation** — warn when a glossary term is not respected
- **Context injection** — feed `translator.context` into the domain-analysis prompt
- **Pluggable providers** — explicit provider abstractions beyond OpenAI-compatible HTTP
- **Enterprise version** with:
  - Team dashboards
  - CI/CD pipeline analytics
  - Translation quality scoring

> **Done:** Support for external LLM providers (OpenAI, Azure OpenAI, and any other OpenAI-compatible endpoint) is available today via the `aiConfiguration` block — see [AI Connection Variants](#ai-connection-variants).

---

## Enterprise

This repository is the **community edition** and will remain MIT-licensed and freely usable.

An **Enterprise edition** is planned on top of this codebase. It will target teams that need additional controls around localization at scale:

- Centralized glossaries and terminology governance
- Audit trails and approval workflows
- Translation memory shared across projects
- Provider governance and policy enforcement
- CI/CD-native reporting and quality scoring
- Team dashboards for translation coverage and drift

The community core will continue to evolve independently. If you're evaluating this project for a larger organization and want to talk about the enterprise roadmap, please open an issue or reach out via the contact details in the repository profile.

---

## License

This project is licensed under the **MIT License**.

See the [LICENSE](LICENSE) file for details.

You are free to use, modify, and distribute it — including in commercial and closed-source products — as long as the license and copyright notice are preserved.

---

## Contributing

Contributions are welcome! Please open an issue or submit a pull request.

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Open a pull request

Please ensure your code follows the existing style and includes appropriate tests. For bug reports, please include a minimal reproduction — a tiny host project plus the relevant `aisettings.json`.

---

## Acknowledgements

- Built around the standard `.resx` format used across the .NET ecosystem.
- Works with local and hosted LLMs through a single OpenAI-compatible interface.
- Built on the shoulders of [Newtonsoft.Json](https://www.newtonsoft.com/json), [Microsoft.Bcl.Memory](https://www.nuget.org/packages/Microsoft.Bcl.Memory), and the MSBuild task infrastructure.
- Inspired by the need to keep multi-language applications in sync without manual effort.
- Thanks to the maintainers of llama.cpp, LM Studio, Ollama, and every other OpenAI-compatible runtime that makes local-first AI practical.