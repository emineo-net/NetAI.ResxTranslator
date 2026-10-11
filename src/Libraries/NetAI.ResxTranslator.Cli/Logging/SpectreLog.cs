using Spectre.Console;

namespace NetAI.ResxTranslator.Cli.Logging;

public sealed class SpectreLog
{
    private readonly bool _verbose;
    private readonly bool _noColor;

    public SpectreLog(bool verbose, bool noColor)
    {
        _verbose = verbose;
        _noColor = noColor;
    }

    public void Info(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        // Warnings and errors are always shown; everything else only with --verbose.
        var alwaysShow = message.Contains(" Warning]", StringComparison.Ordinal)
                      || message.Contains(" Error]", StringComparison.Ordinal);

        if (!_verbose && !alwaysShow) return;

        if (_noColor)
        {
            Console.WriteLine(message);
        }
        else
        {
            AnsiConsole.MarkupLine($"[grey]{Markup.Escape(message)}[/]");
        }
    }
}