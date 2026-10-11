using Spectre.Console;
using Spectre.Console.Cli;

namespace NetAI.ResxTranslator.Cli.Commands;

public sealed class InitCommandSettings : CommandSettings
{
    [CommandArgument(0, "[directory]")]
    public string? Directory { get; init; }
}

public sealed class InitCommand : Command<InitCommandSettings>
{
    public override int Execute(CommandContext context, InitCommandSettings settings)
    {
        var dir = Path.GetFullPath(settings.Directory ?? Directory.GetCurrentDirectory());
        var target = Path.Combine(dir, "aisettings.json");

        if (File.Exists(target))
        {
            AnsiConsole.MarkupLine($"[yellow]aisettings.json already exists:[/] {target}");
            return 0;
        }

        Directory.CreateDirectory(dir);

        var defaultJson = """
        {
          "$schema": "aisettings-schema.json",
          "version": "1.1",
          "translator": {
            "mode": "all",
            "apiKey": "",
            "context": "",
            "glossaryPath": "",
            "defaultLanguage": "en",
            "supportedLanguages": []
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
        """;

        File.WriteAllText(target, defaultJson);
        AnsiConsole.MarkupLine($"[green]✓[/] Created {target}");
        return 0;
    }
}