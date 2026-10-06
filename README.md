## NetAI.ResxTranslator

**AI-powered .resx translation for .NET**

NetAI.ResxTranslator is a tool that analyzes your `.resx` resource files and automatically translates missing entries using a Large Language Model (LLM). It runs against a local OpenAI-compatible endpoint (llama.cpp, Ollama, LM Studio, …) or a hosted API such as OpenAI — you choose. It integrates seamlessly into your build process as a NuGet package or build task, keeping every supported language in sync without manual effort.

> **Note:** This is the open-source community edition, released under the MIT License.
>
> An Enterprise version with advanced features (e.g., team collaboration, CI/CD analytics, translation memory) is planned for the future.

---

## Table of Contents

- Features
- How It Works
- Installation
- Configuration
  - Configuration File
  - AI Connection Variants
- Usage
  - As a Build Task
  - Programmatic API
- Examples
- Configuration Options
- Roadmap
- License
- Contributing

---

## Features

- **`.resx`-aware analysis** – Finds, parses, and understands your resource files, including satellite files per culture.
- **AI-driven translation** – Uses an LLM to fill in missing translations based on your default-language text.
- **Domain-aware prompting** – Performs a lightweight domain analysis first (e.g., "Medical Software", "E-commerce UI") to give the model context and improve translation quality.
- **Batch processing** – Groups missing keys into batches so a single request covers many entries.
- **Local and cloud LLMs** – Works with any OpenAI-compatible endpoint: local servers (llama.cpp, Ollama, LM Studio) or hosted APIs (OpenAI, Azure OpenAI, custom proxies).
- **Automatic file creation** – Creates missing `.resx` files for configured target languages.
- **Glossary support** – Optional glossary file for consistent terminology across translations.
- **Configurable** – Fine-tune behavior via a single `aisettings.json` file.
- **NuGet package** – Easy to add to any .NET project.

---

## How It Works

1. **Resource Discovery**

   `ProjectResxAnalyzer` scans the project directory for all `.resx` files and parses their entries, keyed by name.

2. **Language Resolution**

   The default language (from `translator.defaultLanguage`) and the list of supported languages (from `translator.supportedLanguages`) are resolved. Missing files for supported languages are created on the fly.

3. **Gap Detection**

   For each resource file, entries without a translation are collected. Entries already translated are skipped.

4. **Source Text Lookup**

   For each missing key, the source text is taken from the default-language file (or any sibling file that has a value). This is the text sent to the LLM.

5. **Domain Analysis (LLM call #1)**

   The LLM receives a short prompt with sample source texts and returns a detected domain (e.g., "General Software UI"). This is used to prime the following translations.

6. **Batch Translation (LLM call #2…n)**

   Missing keys are grouped into batches. Each batch is sent to the LLM with the domain and optional glossary. The model replies with a strict format:

   ```
   [KEY:1] Übersetzung Eins
   [KEY:2] Übersetzung Zwei
   ```

   The response is parsed with a regex; unmatched lines are ignored.

7. **Write-back**

   Translations are written back into the corresponding `.resx` file. Keys that couldn't be translated are marked with a placeholder so nothing is silently lost.

---

## Installation

### NuGet Package

```
dotnet add package NetAI.ResxTranslator
```

> Replace `NetAI.ResxTranslator` with the actual package ID once published.

### Build Task

Add the package reference to your project and configure it to run during build. Example `.csproj` snippet:

```xml
<ItemGroup>
  <PackageReference Include="NetAI.ResxTranslator" Version="1.0.0" />
</ItemGroup>

<Target Name="TranslateResx" BeforeTargets="Build">
  <NetAITranslateResx ProjectDirectory="$(MSBuildProjectDirectory)"
                      SolutionPath="$(SolutionPath)" />
</Target>
```

> The exact MSBuild task name and parameters will be documented once the build task is finalized.

---

## Configuration

Configuration is provided via `aisettings.json`. The file is validated by `aisettings-schema.json` (JSON Schema, Draft 07), so editors like VS Code and Rider provide autocomplete and inline validation.

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
    "baseUrl": "http://localhost:8080/",
    "model": "gpt-4o",
    "temperature": 0.2,
    "timeoutMinutes": 5,
    "systemPrompt": "",
    "apiKey": "",
    "apiKeyEnvVar": ""
  }
}
```

All fields are always present. Unused fields stay at `""` (strings) or `0` (numbers) and are treated as *not set*. The loader replaces empty values with defaults before validation, so the file above connects to a local endpoint at `http://localhost:8080/` with a 5-minute timeout and no authentication.

### AI Connection Variants

The `aiConfiguration` block drives the connection to the LLM. The same structure works for local and hosted endpoints — only the values change.

**Required fields**

| Field         | Type     | Description                                      |
|---------------|----------|--------------------------------------------------|
| `model`       | `string` | Model name, e.g. `gpt-4o`, `llama3.1`            |
| `temperature` | `number` | Creativity (0.0 = deterministic, 2.0 = maximum)  |

**Optional fields** — set to `""` or `0` if unused

| Field            | Type     | Default (when empty)     | Description                                                        |
|------------------|----------|--------------------------|--------------------------------------------------------------------|
| `baseUrl`        | `string` | `http://localhost:8080/` | Endpoint URL — **must end with `/`**                                |
| `timeoutMinutes` | `integer`| `5`                      | HTTP timeout in minutes                                             |
| `systemPrompt`   | `string` | *(none)*                 | System role and rules for the model                                 |
| `apiKey`         | `string` | *(none)*                 | API key directly in the file — **mutually exclusive with `apiKeyEnvVar`** |
| `apiKeyEnvVar`   | `string` | *(none)*                 | Name of the environment variable holding the key — **mutually exclusive with `apiKey`** |

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

#### Variant 4 — Hosted API via environment variable (recommended)

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

#### Rules & pitfalls

- **`apiKey` and `apiKeyEnvVar` are mutually exclusive.** Setting both to non-empty values produces a validation error. Leaving both at `""` is valid and means *no authentication*.
- **Empty strings mean *not set*.** `"baseUrl": ""` falls back to `http://localhost:8080/`. `"timeoutMinutes": 0` falls back to `5`.
- **`baseUrl`, if given, must end with `/`.** `http://localhost:11434` is invalid; `http://localhost:11434/` is correct.
- **Env-var names are case-sensitive** on Linux and macOS. `OPENAI_API_KEY` ≠ `openai_api_key`.
- **If the environment variable is not set**, no `Authorization` header is sent. Cloud providers will return `401`; local servers without auth behave as expected.
- **Never commit secrets.** Prefer Variant 4 for cloud endpoints.

---

## Usage

### As a Build Task

Once configured, the translator runs automatically during your build. It will:

- Scan the project directory for `.resx` files
- Detect entries without a translation
- Send them to the configured LLM in batches
- Write translations back into the corresponding `.resx` file
- Skip entries that are already translated

### Programmatic API

You can also use the orchestrator directly in your own code:

```csharp
using NetAI.ResxTranslator.Core;
using NetAI.ResxTranslator.Core.Config;

var config = AiSettingsLoader.Load(@"C:\MyProject");

var orchestrator = new ResxTranslationOrchestrator();

ResxTranslationResult result = await orchestrator.ProcessProject(
    projectDir: @"C:\MyProject",
    settings: config.Translator!,
    logInfo: msg => Console.WriteLine(msg));

if (!result.IsSuccess)
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

## Configuration Options

### Top-level

| Option    | Type   | Default | Description                     |
|-----------|--------|---------|---------------------------------|
| `version` | string | `1.1`   | Configuration format version    |

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

| Option           | Type     | Default (empty)          | Description                                                        |
|------------------|----------|--------------------------|--------------------------------------------------------------------|
| `baseUrl`        | string   | `http://localhost:8080/` | OpenAI-compatible endpoint — must end with `/`                     |
| `model`          | string   | `gpt-4o`                 | Model identifier                                                   |
| `temperature`    | number   | `0.2`                    | Sampling temperature (0.0–2.0)                                     |
| `timeoutMinutes` | integer  | `5`                      | HTTP timeout in minutes                                            |
| `systemPrompt`   | string   | *(none)*                 | System prompt for the model                                        |
| `apiKey`         | string   | *(none)*                 | Inline API key (mutually exclusive with `apiKeyEnvVar`)            |
| `apiKeyEnvVar`   | string   | *(none)*                 | Environment variable holding the API key (mutually exclusive with `apiKey`) |

---

## Roadmap

- Publish stable NuGet package
- Add MSBuild task documentation
- Translation memory to avoid retranslating identical strings
- Glossary validation (warn when a glossary term is not respected)
- Enterprise version with:
  - Team dashboards
  - CI/CD pipeline analytics
  - Translation quality scoring

> **Done:** Support for external LLM providers (OpenAI, Azure OpenAI, and any other OpenAI-compatible endpoint) is available today via the `aiConfiguration` block — see *AI Connection Variants* above.

---

## License

This project is licensed under the **MIT License**.

See the [LICENSE](LICENSE) file for details.

---

## Contributing

Contributions are welcome! Please open an issue or submit a pull request.

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Open a pull request

Please ensure your code follows the existing style and includes appropriate tests.

---

## Acknowledgments

- Built around the standard `.resx` format used across the .NET ecosystem
- Works with local and hosted LLMs through a single OpenAI-compatible interface
- Inspired by the need to keep multi-language applications in sync without manual effort