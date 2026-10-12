# NetAI.ResxTranslator.Cli

> **`netai-resx`** — translate your `.resx` files from the command line.
> Same engine as the MSBuild task. Same `aisettings.json`. No IDE required.

This README covers the **CLI tool**. For the full project overview, see the [solution README](../../../README.md). If you'd rather have translations happen automatically during `dotnet build`, use the sibling [`NetAI.ResxTranslator.Tasks`](../NetAI.ResxTranslator.Tasks/README.md) package.

---

## Table of Contents

- [Why a CLI?](#why-a-cli)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Commands](#commands)
  - [`init`](#init)
  - [`translate`](#translate)
- [Options](#options)
- [Exit Codes](#exit-codes)
- [JSON Output (for CI)](#json-output-for-ci)
- [Examples](#examples)
- [Configuration](#configuration)
- [License](#license)

---

## Why a CLI?

The build task is great when you want localization on autopilot. Sometimes, though, you want it *on demand*:

- **You're in CI** and want a dedicated translation stage that fails the pipeline on missing strings.
- **You're debugging a prompt** and want to see what the LLM actually returns — without rebuilding the whole solution.
- **You're migrating** an existing project and want to translate everything once, up front.
- **You're in a repo** where you don't want a build task poking at files on every compile.

The CLI is exactly that: the same engine, exposed as a `dotnet tool` named `netai-resx`.

---

## Installation

```bash
dotnet tool install --global NetAI.ResxTranslator.Cli
```

Once installed, the tool is available as `netai-resx`:

```bash
netai-resx --help
```

**Local install (recommended for CI):**

```bash
dotnet tool install --local NetAI.ResxTranslator.Cli
dotnet tool restore          # in CI, before running the tool
dotnet netai-resx translate ./src/MyApp
```

Or add a manifest:

```bash
dotnet new tool-manifest
dotnet tool install NetAI.ResxTranslator.Cli
```

---

## Quick Start

```bash
# 1. Scaffold a default aisettings.json in your project
netai-resx init ./src/MyApp

# 2. Point it at a local model. The easiest way:
#    - open LLMRunner (companion app, same authors)
#    - pick a model, click "Serve on localhost:8080"
#    - the default aisettings.json already expects exactly that address.
#
#    Prefer a terminal? Use Ollama with Qwen 2.5 14B:
#      ollama pull qwen2.5:14b
#      ollama serve
#    ... then set baseUrl to http://localhost:11434/ and model to qwen2.5:14b.

# 3. Translate now
netai-resx translate ./src/MyApp
```

That's it. Your `de.resx`, `fr.resx`, `it.resx` files will be created and filled in — the same way the build task would do it.

> **Just want to try it with a local model?** Grab **LLMRunner** from the same repository profile or the releases page. It downloads, chats, and serves a model on `http://localhost:8080/` — one window, no terminal.

---

## Commands

### `init`

Create a default `aisettings.json` in the given directory.

```
netai-resx init [directory]
```

| Argument      | Required | Default                    | Description                                |
|---------------|----------|----------------------------|--------------------------------------------|
| `[directory]` | no       | current working directory  | Where to create `aisettings.json`.         |

Behavior:

- If `aisettings.json` already exists, it prints a yellow notice and exits `0` — **it never overwrites an existing config**.
- Otherwise it writes a fully-populated default file (all fields present, safe to commit) and prints the path.

**Example:**

```bash
netai-resx init
netai-resx init ./src/MyApp
```

---

### `translate`

Translate missing entries in every `.resx` file under a project directory.

```
netai-resx translate <projectDir> [options]
```

| Argument       | Required | Description                                                |
|----------------|----------|------------------------------------------------------------|
| `<projectDir>` | yes      | Directory of the project whose `.resx` files to translate. |

The command:

1. Loads `aisettings.json` from `<projectDir>` (or upward — see [`--config`](#options)).
2. Validates `aiConfiguration` and language codes.
3. Discovers `.resx` files, creates missing satellite files, and translates empty entries.
4. Writes everything back in place.

**Example:**

```bash
netai-resx translate ./src/MyApp
```

---

## Options

### `translate` options

| Option | Description |
|---|---|
| `--config <FILE>` | Path to `aisettings.json`. Default: nearest file found **upward** from `<projectDir>`. |
| `-v`, `--verbose` | Show every log line from the orchestrator. |
| `--no-color` | Disable ANSI colors (useful when piping to a file). |
| `--json` | Machine-readable JSON on **stdout**, logs on **stderr**. Ideal for CI. |

### Global

| Option | Description |
|---|---|
| `-h`, `--help` | Show help. |
| `--version` | Show the tool version. |

---

## Exit Codes

The CLI uses well-defined exit codes so your pipeline can react intelligently:

| Code | Name                | Meaning                                                       |
|-----:|---------------------|---------------------------------------------------------------|
| `0`  | `Success`           | Translation completed successfully.                           |
| `1`  | `ConfigurationError`| Project directory missing, config file invalid, schema error. |
| `2`  | `TranslationFailed` | The engine ran but reported a failure (e.g., LLM unreachable). |
| `99` | `UnhandledError`    | Unexpected exception. Check stderr for details.               |
| `130`| `Cancelled`         | Terminated by Ctrl+C.                                         |

**Example CI snippet:**

```bash
netai-resx translate ./src/MyApp --json
case $? in
  0)   echo "✅ Translations up to date." ;;
  1)   echo "❌ Configuration problem." ; exit 1 ;;
  2)   echo "❌ Translation failed."   ; exit 1 ;;
  *)   echo "💥 Unexpected error."      ; exit 1 ;;
esac
```

---

## JSON Output (for CI)

With `--json`, the tool writes a single JSON object to **stdout** and sends every diagnostic line to **stderr**. This means you can pipe stdout straight into `jq`, your build system, or a dashboard without parsing human-readable text.

**On success:**

```json
{
  "success": true,
  "error": null
}
```

**On failure:**

```json
{
  "success": false,
  "error": "aiConfiguration.baseUrl is not a valid absolute URI: 'localhost:11434'."
}
```

Verbose logs (`-v`) go to stderr, so they never pollute the JSON.

**Example — fail a GitHub Actions step on any translation error:**

```yaml
- name: Translate .resx files
  run: |
    dotnet tool restore
    dotnet netai-resx translate ./src/MyApp --json > result.json
    jq -e '.success == true' result.json
```

---

## Examples

### Translate a single project once, verbosely

```bash
netai-resx translate ./src/MyApp -v
```

### Use an explicit config file

```bash
netai-resx translate ./src/MyApp --config ./config/aisettings.ci.json
```

### Quiet machine-readable run (for scripts)

```bash
netai-resx translate ./src/MyApp --json
```

### Pipe friendly, no ANSI colors

```bash
netai-resx translate ./src/MyApp --no-color | tee translation.log
```

### Translate several projects in a loop

```bash
for project in src/App src/Shared src/Plugins/*; do
  echo "→ $project"
  netai-resx translate "$project" || exit 1
done
```

### GitHub Actions — fail the build on translation problems

```yaml
- name: Setup .NET
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: '9.0.x'

- name: Install netai-resx
  run: dotnet tool install --global NetAI.ResxTranslator.Cli

- name: Translate resources
  env:
    OPENAI_API_KEY: ${{ secrets.OPENAI_API_KEY }}
  run: netai-resx translate ./src/MyApp --json
```

---

## Configuration

The CLI reads the same `aisettings.json` as the build task — there is only one config format. See the [solution README](../../../README.md#configuration--aisettingsjson) for:

- The full configuration reference.
- All AI connection variants (local, OpenAI, Azure, custom proxy).
- A ready-to-use **Ollama + Qwen 2.5 14B** example.
- Rules & pitfalls (trailing slash on `baseUrl`, mutually-exclusive key fields, …).

**Note:** `translator.mode` is **ignored by the CLI** — a CLI invocation is always explicit. Mode-based gating only applies to the MSBuild task.

---

## License

MIT — see the [LICENSE](../../../LICENSE) file.