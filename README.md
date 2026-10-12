# NetAI.ResxTranslator

[![NuGet](https://img.shields.io/nuget/v/NetAI.ResxTranslator.Tasks.svg?label=NuGet%20(Tasks))](https://www.nuget.org/packages/NetAI.ResxTranslator.Tasks)
[![NuGet Downloads](https://img.shields.io/nuget/dt/NetAI.ResxTranslator.Tasks.svg)](https://www.nuget.org/packages/NetAI.ResxTranslator.Tasks)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET Standard](https://img.shields.io/badge/.NET-netstandard2.0%20%7C%20net8.0%20%7C%20net9.0%20%7C%20net10.0-blueviolet)](https://learn.microsoft.com/dotnet/standard/net-standard)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](#contributing)

> ### Stop translating `.resx` files by hand.
> **NetAI.ResxTranslator** turns every build into a localization engine — and gives you a command-line tool when you want to run the same engine on demand.
> Point it at any OpenAI-compatible LLM (local *or* cloud), and missing translations fill themselves in. No pre-commit hooks, no SaaS lock-in, no copy-paste into a chat window.

---

## Table of Contents

- [What Is It?](#what-is-it)
- [The Three Pillars](#the-three-pillars)
- [Meet LLMRunner](#meet-llmrunner)
- [Features](#features)
- [How It Works](#how-it-works)
- [Requirements](#requirements)
- [Pick Your Entry Point](#pick-your-entry-point)
  - [1. MSBuild Task (NuGet)](#1-msbuild-task-nuget)
  - [2. Command-Line Tool (`netai-resx`)](#2-command-line-tool-netai-resx)
  - [3. Programmatic API](#3-programmatic-api)
- [Configuration — `aisettings.json`](#configuration--aisettingsjson)
  - [Getting Started with Ollama + Qwen 2.5 14B](#getting-started-with-ollama--qwen-25-14b)
  - [AI Connection Variants](#ai-connection-variants)
  - [Rules & Pitfalls](#rules--pitfalls)
- [Repository Layout](#repository-layout)
- [Roadmap](#roadmap)
- [Enterprise Edition — Help Us Prioritize It](#enterprise-edition--help-us-prioritize-it)
- [License](#license)
- [Contributing](#contributing)
- [Acknowledgements](#acknowledgements)

---

## What Is It?

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
- **It understands your domain.** Before translating, it asks the LLM *what kind of software this is* — so "Save" becomes *Speichern* in a CRM and *Sichern* in a mountaineering app.

The result: your `de.resx`, `fr.resx`, `ja.resx` files stay permanently in sync with the source — with **zero ongoing effort**.

---

## The Three Pillars

NetAI.ResxTranslator is split into three projects, each with a clear job:

| Project | What it is | Audience |
|---|---|---|
| **`NetAI.ResxTranslator.Core`** | The engine. Discovery, parsing, prompting, LLM client, write-back. | Library authors / advanced users. |
| **`NetAI.ResxTranslator.Tasks`** | An MSBuild task shipped as a NuGet package. Runs the engine before compilation. | Every .NET project that wants localization on autopilot. |
| **`NetAI.ResxTranslator.Cli`** | A `dotnet tool` named **`netai-resx`**. Runs the same engine on the command line. | CI pipelines, one-off jobs, debugging. |

Both `Tasks` and `Cli` sit on top of `Core`. Same engine, same `aisettings.json`, same results. Pick whichever fits your workflow — or use both.

Detailed documentation lives next to each project:

- **Tasks** → [`src/Libraries/NetAI.ResxTranslator.Tasks/README.md`](src/Libraries/NetAI.ResxTranslator.Tasks/README.md)
- **CLI** → [`src/Libraries/NetAI.ResxTranslator.Cli/README.md`](src/Libraries/NetAI.ResxTranslator.Cli/README.md)

---

## Meet LLMRunner

> **A companion tool from the same authors.** You don't need it to use NetAI.ResxTranslator — but it removes the last excuse not to try a local model.

**LLMRunner** is a small WPF application that turns "I'd like to try a local LLM" into "I'm chatting with one in two minutes":

- **Pick from popular models** — a curated list with direct **HuggingFace** download links, so you don't have to hunt for GGUF files and quantizations that actually work.
- **Chat with the model before wiring it up** — sanity-check translation quality on your own strings, not on someone else's benchmark.
- **Serve it on `http://localhost:8080/` with one click** — exactly the address `aisettings.json` expects by default. No environment variables, no command-line flags, no guesswork.

That means the whole "get a local LLM running" step collapses into: open LLMRunner → pick a model → click *Serve*. From there, `dotnet build` will already find your model.

> 👉 Find **LLMRunner** in the same repository profile or the releases page.

If you prefer to run a model server yourself (Ollama, LM Studio, llama.cpp), that's fine too — NetAI.ResxTranslator speaks to any OpenAI-compatible endpoint. The sections below cover both.

---

## Features

- **`.resx`-aware analysis** — Discovers, parses, and understands your resource files, including satellite files per culture. Ignores `bin/`, `obj/`, and `.git/`.
- **AI-driven translation** — Fills in missing entries based on your default-language text. Already-translated entries are never re-sent.
- **Domain-aware prompting** — A one-shot analysis call detects the software's domain (e.g., *Medical Software*, *E-commerce UI*, *Automotive Infotainment*) and injects it into every subsequent translation prompt. Terminology feels native, not literal.
- **Batch processing** — Missing keys are grouped into batches of 20, so a single request covers many entries and token cost stays low.
- **Local *and* cloud LLMs** — Works with any OpenAI-compatible endpoint: LLMRunner, llama.cpp, Ollama, LM Studio, OpenAI, Azure OpenAI, or your own corporate proxy.
- **Placeholder-safe** — `{0}`, `{name}`, `%s`, `\n`, `\t` are preserved verbatim. The model is instructed to treat them as immutable, and the response parser validates the output.
- **Automatic file creation** — Declare `supportedLanguages` and missing satellite `.resx` files are generated from the neutral resource on the fly.
- **Idempotent & incremental** — Only entries with empty values are translated. Re-running is cheap.
- **One config file** — All behavior is driven by `aisettings.json` with full JSON Schema validation in VS, Rider, and VS Code.
- **Two entry points, one engine** — NuGet build task and `netai-resx` CLI share the same `Core`.
- **Safe by default** — Skips design-time builds, respects multi-targeting, can be disabled per build.
- **Companion local-LLM runner** — LLMRunner gets you from zero to `localhost:8080` without a terminal.

---

## How It Works

The same pipeline runs whether you trigger it from MSBuild or the CLI:

1. **Resource Discovery**
   `ProjectResxAnalyzer` scans the project directory for all `.resx` files and parses their entries, keyed by name.

2. **Language Resolution**
   `TranslatorLanguageResolver` validates `defaultLanguage` and `supportedLanguages` against `CultureInfo`. Invalid codes are reported and ignored — no silent misbehavior.

3. **Satellite Generation**
   For each resource base name, missing `<name>.<culture>.resx` files are created from the most complete sibling, with all values emptied.

4. **Gap Detection**
   Entries without a translation (`HasTranslation == false`) are collected. Everything else is skipped.

5. **Source Text Lookup**
   For each missing key, source text is taken from the neutral file (or any sibling that has a value) — that's what gets sent to the LLM.

6. **Domain Analysis — LLM call #1**
   A single call sends up to 15 sample strings and asks the model for a short domain label. The result is sanitized and used to prime every following prompt.

7. **Batch Translation — LLM calls #2…n**
   Missing keys are grouped into batches of 20. Each batch is sent with a strict system prompt that demands the format:

   ```
   [KEY:identifier] <translation>
   ```

8. **Response Parsing**
   A tolerant regex extracts `[KEY:...]` pairs. Keys not requested in the current batch are discarded — an anti-hallucination guard.

9. **Write-Back**
   Translations are written back into the corresponding `.resx` file in place, preserving comments, headers, and the rest of the XML document.

10. **Re-Embedding** *(MSBuild only)*
    Newly created `.resx` files are added to `EmbeddedResource` before compilation, so they land in the assembly without a second build.

Progress, warnings, and errors surface as native MSBuild messages (`ResxTranslator:` prefix) or as Spectre.Console output (CLI).

---

## Requirements

- .NET SDK **6.0 or later** for the host project. The task itself runs on `netstandard2.0` inside MSBuild's runtime.
- For the CLI: .NET **9.0 SDK** or later runtime.
- An **OpenAI-compatible endpoint**:
  - **Recommended for local use**: **LLMRunner** — pick a model, click *Serve*, done.
  - **Also supported**: [llama.cpp server](https://github.com/ggerganov/llama.cpp), [LM Studio](https://lmstudio.ai/), [Ollama](https://ollama.com/), …
  - **Hosted**: OpenAI, Azure OpenAI, or any gateway exposing `/v1/chat/completions`.

---

## Pick Your Entry Point

### 1. MSBuild Task (NuGet)

The zero-touch option: translation happens automatically before every build.

```bash
dotnet add package NetAI.ResxTranslator.Tasks
```

Then build once, edit `aisettings.json`, and build again. Your `de.resx`, `fr.resx`, `it.resx` files — even the ones that don't exist yet — will be created and filled in.

Full documentation, build integration details, and troubleshooting:
👉 **[`src/Libraries/NetAI.ResxTranslator.Tasks/README.md`](src/Libraries/NetAI.ResxTranslator.Tasks/README.md)**

### 2. Command-Line Tool (`netai-resx`)

For CI pipelines, ad-hoc runs, and debugging:

```bash
dotnet tool install --global NetAI.ResxTranslator.Cli

netai-resx init   ./src/MyApp       # scaffold aisettings.json
netai-resx translate ./src/MyApp    # translate now, once
```

Full command reference, options, exit codes, and CI examples:
👉 **[`src/Libraries/NetAI.ResxTranslator.Cli/README.md`](src/Libraries/NetAI.ResxTranslator.Cli/README.md)**

### 3. Programmatic API

Drive the engine from your own code — perfect for custom tooling:

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

## Configuration — `aisettings.json`

Whether you use the task or the CLI, everything is driven by a single `aisettings.json` at the project root. It's validated by `aisettings-schema.json` (JSON Schema, Draft 07), so VS Code, Rider, and Visual Studio give you autocomplete and inline validation.

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
    "baseUrl": "http://localhost:8080/",
    "model": "local-model",
    "temperature": 0.2,
    "timeoutMinutes": 5,
    "apiKey": ""
  }
}
```

All fields are always present. Unused fields stay at `""` (strings) or `0` (numbers) and are treated as *not set*. The loader replaces empty values with defaults before validation.

### Getting Started with Ollama + Qwen 2.5 14B

**Qwen 2.5 14B** is a strong, compact, multilingual open-weights model that fits comfortably on a 16 GB machine. It follows structured prompt formats reliably (important for our `[KEY:...]` output format), and it handles European languages — German, French, Italian, Spanish, Portuguese — with native-quality vocabulary. It's an excellent default for NetAI.ResxTranslator if you don't already have a preferred model.

**Step 1 — Install Ollama** from [ollama.com/download](https://ollama.com/download).

**Step 2 — Pull the model and start the server:**

```bash
ollama pull qwen2.5:14b
ollama serve
```

Ollama listens on `http://localhost:11434/` and exposes an OpenAI-compatible endpoint at `/v1/chat/completions` — exactly the shape NetAI.ResxTranslator expects.

**Step 3 — Point `aisettings.json` at it:**

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
    "model": "qwen2.5:14b",
    "temperature": 0.2,
    "timeoutMinutes": 10,
    "systemPrompt": "",
    "apiKey": "",
    "apiKeyEnvVar": ""
  }
}
```

That's it. Build (or run `netai-resx translate`) and the model is picked up.

> **Tips:**
> - The trailing slash on `baseUrl` is required.
> - `timeoutMinutes` of `10` is a good starting point for a 14B model on CPU; on a modern GPU, `5` is usually enough.
> - Qwen 2.5 14B handles the domain-analysis step (the first, one-shot prompt) well, but if you see generic domain labels like *General Software UI*, that's fine — the per-batch prompts are what matters most.
> - If you'd rather not touch a terminal at all, **LLMRunner** does all of this — download, chat, and serve on `localhost:8080` — in a single window. See [Meet LLMRunner](#meet-llmrunner).

### AI Connection Variants

The same `aiConfiguration` block works for every endpoint — only the values change.

**Required fields**

| Field         | Type     | Description                                      |
|---------------|----------|--------------------------------------------------|
| `model`       | `string` | Model name, e.g. `gpt-4o`, `qwen2.5:14b`.        |
| `temperature` | `number` | Creativity (0.0 = deterministic, 2.0 = maximum). |

**Optional fields** — set to `""` or `0` if unused

| Field            | Type     | Default (when empty)     | Description                                                              |
|------------------|----------|--------------------------|--------------------------------------------------------------------------|
| `baseUrl`        | `string` | `http://localhost:8080/` | Endpoint URL — **must end with `/`**.                                    |
| `timeoutMinutes` | `integer`| `5`                      | HTTP timeout in minutes.                                                 |
| `systemPrompt`   | `string` | *(none)*                 | System role and rules for the model.                                     |
| `apiKey`         | `string` | *(none)*                 | API key directly in the file — **mutually exclusive with `apiKeyEnvVar`**. |
| `apiKeyEnvVar`   | `string` | *(none)*                 | Name of the env var holding the key — **mutually exclusive with `apiKey`**. |

> **Rule of thumb:** `apiKey` = the key itself. `apiKeyEnvVar` = only the **name** of the environment variable that holds the key. Only one of the two may be non-empty.

#### Variant 1 — LLMRunner, no configuration required

If you use the companion **LLMRunner** app, it serves your chosen local model on `http://localhost:8080/` — the default. The minimal `aisettings.json` above works out of the box.

#### Variant 2 — Ollama (Qwen 2.5 14B)

See [Getting Started with Ollama + Qwen 2.5 14B](#getting-started-with-ollama--qwen-25-14b).

#### Variant 3 — Ollama with a different model

```json
"aiConfiguration": {
  "baseUrl": "http://localhost:11434/",
  "model": "llama3.1:8b",
  "temperature": 0.2,
  "timeoutMinutes": 10,
  "systemPrompt": "",
  "apiKey": "",
  "apiKeyEnvVar": ""
}
```

#### Variant 4 — LM Studio

```json
"aiConfiguration": {
  "baseUrl": "http://localhost:1234/",
  "model": "qwen2.5-14b-instruct",
  "temperature": 0.2,
  "timeoutMinutes": 10,
  "systemPrompt": "",
  "apiKey": "",
  "apiKeyEnvVar": ""
}
```

> Local servers often ignore `model`; setting it anyway is harmless.

#### Variant 5 — Local server with token

```json
"aiConfiguration": {
  "baseUrl": "http://localhost:11434/",
  "model": "qwen2.5:14b",
  "temperature": 0.2,
  "timeoutMinutes": 5,
  "systemPrompt": "",
  "apiKey": "local-secret",
  "apiKeyEnvVar": ""
}
```

> ⚠️ **Do not commit real values** into version control. Prefer Variant 6.

#### Variant 6 — Hosted API via environment variable *(recommended for cloud)*

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

Set the variable:

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

#### Variant 7 — Azure OpenAI

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

#### Variant 8 — Custom proxy / corporate endpoint

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

- **`apiKey` and `apiKeyEnvVar` are mutually exclusive.** Setting both to non-empty values produces a validation error. Both empty means *no authentication*.
- **Empty strings mean *not set*.** `"baseUrl": ""` falls back to `http://localhost:8080/`. `"timeoutMinutes": 0` falls back to `5`.
- **`baseUrl`, if given, must end with `/`.** `http://localhost:11434` is invalid; `http://localhost:11434/` is correct.
- **Env-var names are case-sensitive** on Linux and macOS. `OPENAI_API_KEY` ≠ `openai_api_key`.
- **If the env var is not set**, no `Authorization` header is sent. Cloud providers will return `401`; local servers without auth behave as expected.
- **Never commit secrets.** Prefer Variant 6 for cloud endpoints.

### Configuration Reference

**Top-level**

| Option    | Type   | Default | Description                  |
|-----------|--------|---------|------------------------------|
| `version` | string | `1.1`   | Configuration format version |

**`translator`**

| Option                | Type     | Default | Description                                                       |
|-----------------------|----------|---------|-------------------------------------------------------------------|
| `mode`                | string   | `all`   | `all` or comma-separated: `debug`, `release`, `publish` *(Tasks only)* |
| `apiKey`              | string   | `""`    | **Deprecated** — use `aiConfiguration.apiKey` / `apiKeyEnvVar`    |
| `context`             | string   | `""`    | Additional context for the translator (industry, tone, …)         |
| `glossaryPath`        | string   | `""`    | Path to a glossary file for consistent terminology *(reserved)*   |
| `defaultLanguage`     | string   | `en`    | Culture code of the neutral `.resx` files                         |
| `supportedLanguages`  | string[] | `[]`    | Target culture codes, e.g. `["de", "fr"]`                         |

**`aiConfiguration`** — see [AI Connection Variants](#ai-connection-variants).

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

NetAI.ResxTranslator.Cli/
  Program.cs               Command registration
  Commands/                InitCommand, TranslateCommand
  Logging/                 SpectreLog
  ExitCodes.cs             Well-known exit codes
```

---

## Roadmap

- ✅ Publish stable NuGet package
- ✅ MSBuild task documentation
- ✅ Command-line tool (`netai-resx`)
- ✅ Companion app **LLMRunner** for local-model setup
- **Translation memory** — avoid retranslating identical strings across builds
- **Glossary validation** — warn when a glossary term is not respected
- **Context injection** — feed `translator.context` into the domain-analysis prompt
- **Pluggable providers** — explicit provider abstractions beyond OpenAI-compatible HTTP
- **Enterprise edition** — see below

---

## Enterprise Edition — Help Us Prioritize It

> **Heads up for developers:** if you like what you see here and think your organization would benefit from a more capable, supported edition, please say so. The Enterprise roadmap is driven by real demand.

This repository is the **community edition** and will remain MIT-licensed and freely usable. An **Enterprise edition** is planned on top of this codebase, targeting teams that need additional controls around localization at scale:

- Centralized glossaries and terminology governance
- Audit trails and approval workflows
- Translation memory shared across projects
- Provider governance and policy enforcement
- CI/CD-native reporting and quality scoring
- Team dashboards for translation coverage and drift

### How you can help

You don't need to be a decision-maker to move this forward:

1. **Tell your employer.** If your team ships multi-language software, mention the project in your next 1:1, architecture meeting, or internal Slack. Real organizational interest is what gets this built next.
2. **Open an issue with the label `enterprise-interest`** and describe what *your* team would need most — centralized glossaries? audit trails? translation memory? Even a one-liner helps us prioritize.
3. **Star the repository** if you want to be kept in the loop — we'll announce Enterprise milestones there first.

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
- Built on the shoulders of [Newtonsoft.Json](https://www.newtonsoft.com/json), [Microsoft.Bcl.Memory](https://www.nuget.org/packages/Microsoft.Bcl.Memory), [Spectre.Console](https://spectreconsole.net/), and the MSBuild task infrastructure.
- Inspired by the need to keep multi-language applications in sync without manual effort.
- Thanks to the maintainers of llama.cpp, LM Studio, Ollama, and every other OpenAI-compatible runtime that makes local-first AI practical.