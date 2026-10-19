using System.Text.Json;
using NetAI.ResxTranslator.Cli.Logging;
using NetAI.ResxTranslator.Core;
using NetAI.ResxTranslator.Core.Config;
using NetAI.ResxTranslator.Core.Models;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Cli;

namespace NetAI.ResxTranslator.Cli.Commands;

public sealed class TranslateCommand : AsyncCommand<TranslateCommandSettings>
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public override async Task<int> ExecuteAsync(CommandContext context, TranslateCommandSettings settings)
    {
        var projectDir = Path.GetFullPath(settings.ProjectDir);

        if (!Directory.Exists(projectDir))
        {
            WriteError(settings, $"Project directory not found: {projectDir}");
            return ExitCodes.ConfigurationError;
        }

        AiTestingConfig config;
        try
        {
            config = LoadConfig(projectDir, settings.ConfigPath);
        }
        catch (Exception ex)
        {
            WriteError(settings, $"Config error: {ex.Message}");
            return ExitCodes.ConfigurationError;
        }

        var translator = config.Translator ?? new TranslatorConfig();
        var log = BuildLog(settings);
        var orchestrator = new ResxTranslationOrchestrator();

        if (!settings.Json)
        {
            AnsiConsole.MarkupLine($"[blue]Translating .resx files in[/] [grey]{projectDir}[/]");
        }

        ResxTranslationResult result;
        try
        {
            if (settings.Json)
            {
                result = await orchestrator.ProcessProject(projectDir, translator, log).ConfigureAwait(false);
            }
            else
            {
                result = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .SpinnerStyle(Style.Parse("blue"))
                    .StartAsync(
                        $"Translating [yellow]{Path.GetFileName(projectDir)}[/]…",
                        _ => orchestrator.ProcessProject(projectDir, translator, log))
                    .ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            WriteResult(settings, success: false, error: ex.Message);
            return ExitCodes.UnhandledError;
        }

        if (result.Success)
        {
            WriteResult(settings, success: true, error: null);
            return ExitCodes.Success;
        }

        WriteResult(settings, success: false, error: result.ErrorMessage ?? "Translation failed.");
        return ExitCodes.TranslationFailed;
    }

    // ---------------------------------------------------------------------
    // Config loading
    // ---------------------------------------------------------------------

    private static AiTestingConfig LoadConfig(string projectDir, string? explicitPath)
    {
        string path;

        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            path = Path.GetFullPath(explicitPath);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Config file not found: {path}", path);
            }

            var json = File.ReadAllText(path);
            var config = JsonConvert.DeserializeObject<AiTestingConfig>(json)
                ?? throw new InvalidOperationException($"Failed to parse '{path}'.");

            config.AiConfiguration?.Normalize();
            config.AiConfiguration?.Validate();
            return config;
        }

        // Discover upward from the project directory.
        var dir = new DirectoryInfo(projectDir);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "aitranssettings.json")))
            {
                return AiSettingsLoader.Load(dir.FullName);
            }
            dir = dir.Parent;
        }

        throw new FileNotFoundException(
            $"No aitranssettings.json found in '{projectDir}' or any parent directory.");
    }

    // ---------------------------------------------------------------------
    // Logging + output
    // ---------------------------------------------------------------------

    private static Action<string>? BuildLog(TranslateCommandSettings settings)
    {
        if (settings.Json)
        {
            return settings.Verbose ? msg => Console.Error.WriteLine(msg) : null;
        }

        return new SpectreLog(settings.Verbose, settings.NoColor).Info;
    }

    private static void WriteError(TranslateCommandSettings settings, string message)
    {
        if (settings.Json)
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(
                new { success = false, error = message }, JsonOpts));
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]{Markup.Escape(message)}[/]");
        }
    }

    private static void WriteResult(TranslateCommandSettings settings, bool success, string? error)
    {
        if (settings.Json)
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(
                new { success, error }, JsonOpts));
        }
        else if (success)
        {
            AnsiConsole.MarkupLine("[green]✓ Translation complete.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]✗ Translation failed:[/] {Markup.Escape(error ?? "unknown")}");
        }
    }
}