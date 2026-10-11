namespace NetAI.ResxTranslator.Cli;

public static class ExitCodes
{
    public const int Success            = 0;
    public const int ConfigurationError = 1;
    public const int TranslationFailed  = 2;
    public const int UnhandledError     = 99;
    public const int Cancelled          = 130;
}