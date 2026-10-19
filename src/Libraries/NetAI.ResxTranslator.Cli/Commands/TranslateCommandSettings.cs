using System.ComponentModel;
using Spectre.Console.Cli;

namespace NetAI.ResxTranslator.Cli.Commands;

public sealed class TranslateCommandSettings : CommandSettings
{
    [CommandArgument(0, "<projectDir>")]
    [Description("Directory of the project whose .resx files should be translated.")]
    public string ProjectDir { get; init; } = "";

    [CommandOption("--config <FILE>")]
    [Description("Path to aitranssettings.json. Default: nearest file found upward from <projectDir>.")]
    public string? ConfigPath { get; init; }

    [CommandOption("-v|--verbose")]
    [Description("Show every log line from the orchestrator.")]
    public bool Verbose { get; init; }

    [CommandOption("--no-color")]
    public bool NoColor { get; init; }

    [CommandOption("--json")]
    [Description("Machine-readable JSON on stdout, logs on stderr.")]
    public bool Json { get; init; }
}