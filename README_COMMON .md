# Automatic .resx Translation with AI

> This project helps automatically generate missing translations in .NET projects using AI.

> [!NOTE]
> This NuGet package is the foundation of an **enterprise version** with additional pro features. These include support for **JSON, YAML, and YML** files, **self-hosted translations** where a manager can approve or edit translations, **glossary management**, and more.  
> Learn more at [https://emineo-net.github.io](https://emineo-net.github.io).

## Table of Contents

- [What Does This Project Do?](#what-does-this-project-do)
- [Who Is This For?](#who-is-this-for)
- [Benefits](#benefits)
- [Installation](#installation)
- [Configuration](#configuration)
- [How Does It Work?](#how-does-it-work)
- [Tips](#tips)
- [Limitations](#limitations)
- [Frequently Asked Questions](#frequently-asked-questions)
- [Support](#support)
- [License](#license)

---

## What Does This Project Do?

Many .NET projects use `.resx` files for translations.  
When new texts are added, translations for other languages are often missing.

**NetAI.ResxTranslator** is a NuGet package that helps with exactly that:

- It is installed into a project.
- During build, missing translations are detected.
- The AI generates the missing texts.
- The results are written directly into the `.resx` files.

Configuration is done via a simple JSON file.

---

## Who Is This For?

- Developers localizing software
- Teams supporting many languages
- Projects that want to save time on translations
- Users who want to use local or cloud AI
- Enterprises that require a telemetry-free, privacy-friendly solution

---

## Benefits

- Saves time on missing translations
- Supports multiple languages
- Works with cloud AI or local models
- Simple configuration via `aisettings.json`
- Placeholders like `{0}` are preserved
- Runs automatically during build or publish
- Can be disabled if needed
- **Zero telemetry – no data is ever collected**

---

## Installation

```bash
dotnet add package NetAI.ResxTranslator
```

Then build the project.

On first build, two files are automatically created:

- `aisettings.json`
- `aisettings-schema.json`

---

## Configuration

Example for `aisettings.json`:

```json
{
  "$schema": "aisettings-schema.json",
  "version": "1.1",
  "translator": {
    "mode": "all",
    "apiKey": "",
    "defaultLanguage": "en",
    "supportedLanguages": [ "de", "fr", "it" ]
  },
  "aiConfiguration": {
    "model": "gpt-4o",
    "temperature": 0.2
  }
}
```

### Important Settings

| Setting | Meaning |
| --- | --- |
| `mode` | When should translation run? `all`, `debug`, `release`, `publish` |
| `apiKey` | Key for cloud AI. Leave empty for local models. |
| `defaultLanguage` | Source language, e.g., `en` |
| `supportedLanguages` | Target languages, e.g., `de`, `fr`, `it` |
| `model` | AI model, e.g., `gpt-4o` |
| `temperature` | How creative the AI should be |

---

## How Does It Work?

1. During build, all `.resx` files are searched.
2. Missing language files are created automatically.
3. Missing translations are collected.
4. The AI analyzes the domain context.
5. Texts are translated in small batches.
6. Results are written into the `.resx` files.

---

## Tips

- Do not commit `apiKey` to Git.
- For technical terms, use `context` or `glossaryPath`.
- Use `mode: "publish"` to translate only on publish.
- Disable completely with `ResxTranslatorEnabled=false`:

```bash
dotnet build -p:ResxTranslatorEnabled=false
```

---

## Limitations

- AI translations should be reviewed before release.
- For highly technical texts, a glossary may be necessary.
- Cloud AI may incur costs.
- Local models require appropriate hardware.

---

## Frequently Asked Questions

### Will every `.resx` file be overwritten?

No. Only missing translations are added.

### Are new language files created?

Yes, for all languages in `supportedLanguages` if they do not already exist.

### Can I use local models?

Yes. Leave `apiKey` empty and configure the desired model.

### What happens to placeholders?

Placeholders like `{0}`, `{name}`, or `%s` are preserved.

### Can I disable translation?

Yes, with `-p:ResxTranslatorEnabled=false`.

### Is any data collected?

**No.** This package does not collect any telemetry, usage data, or personal information. It is completely private and enterprise-safe.

---

## Support

For questions, issues, or feature requests, please open an issue in the GitHub repository.

---

## License

This project is licensed under the **MIT License**.

See the [LICENSE](LICENSE) file for details.

**No telemetry. No data collection. Enterprise-ready.**