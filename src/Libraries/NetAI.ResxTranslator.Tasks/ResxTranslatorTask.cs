using Microsoft.Build.Framework;
using NetAI.ResxTranslator.Core;
using NetAI.ResxTranslator.Core.Config;
using Task = Microsoft.Build.Utilities.Task;

namespace NetAI.ResxTranslator.Tasks;

public class ResxTranslatorTask : Task
{
    [Required] public string ProjectDir { get; set; } = string.Empty;

    // Nur noch das, was sich aus dem Build-Kontext ergibt,
    // bleibt als MSBuild-Property. Alles andere kommt aus aisettings.json.
    public string? CurrentConfiguration { get; set; }
    public bool IsPublishing { get; set; }

    public override bool Execute()
    {
        // WPF protection: if project name ends with "_wpftmp", abort silently
        if (!string.IsNullOrEmpty(ProjectDir) && ProjectDir.EndsWith("_wpftmp", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        AiTestingConfig config;
        try
        {
            config = AiSettingsLoader.Load(ProjectDir);
        }
        catch (Exception ex)
        {
            Log.LogError($"[ResxTranslator] aisettings.json konnte nicht geladen werden: {ex.Message}");
            return false;
        }

        var translatorConfig = config.Translator ?? new TranslatorConfig();

        var modeInput = (translatorConfig.Mode ?? "all").ToLowerInvariant();
        var config_ = (CurrentConfiguration ?? "Debug").ToLowerInvariant();

        var activeModes = modeInput.Split(',').Select(m => m.Trim()).ToList();

        Log.LogMessage(MessageImportance.High,
            $"[ResxTranslator] Mode check: Config={config_}, IsPublish={IsPublishing}, Active modes=[{string.Join(", ", activeModes)}]");

        var shouldRun = false;

        if (activeModes.Contains("all"))
        {
            shouldRun = true;
        }
        else
        {
            if (activeModes.Contains("debug") && config_ == "debug" && !IsPublishing)
            {
                shouldRun = true;
            }

            if (activeModes.Contains("release") && config_ == "release" && !IsPublishing)
            {
                shouldRun = true;
            }

            if (activeModes.Contains("publish") && IsPublishing)
            {
                shouldRun = true;
            }
        }

        if (!shouldRun)
        {
            Log.LogMessage(MessageImportance.High,
                $"🤖 [ResxTranslator] Skipped. The current state (Config={CurrentConfiguration}, Publish={IsPublishing}) " +
                $"is not included in the allowed modes '{translatorConfig.Mode}'.");
            return true;
        }

        Log.LogMessage(MessageImportance.High, "🤖 [ResxTranslator] Mode condition met. Starting analysis...");

        if (!string.IsNullOrEmpty(translatorConfig.Context))
        {
            Log.LogMessage(MessageImportance.High, $"[ResxTranslator] App context received: {translatorConfig.Context}");
        }

        if (!string.IsNullOrEmpty(translatorConfig.GlossaryPath))
        {
            Log.LogMessage(MessageImportance.High, $"[ResxTranslator] Glossary path received: {translatorConfig.GlossaryPath}");
        }

        Log.LogMessage(MessageImportance.High, "🤖 NetAI.ResxTranslator: Starting analysis...");

        var collectedIssues = new List<string>();
        var orchestrator = new ResxTranslationOrchestrator();

        var result = orchestrator.ProcessProject(ProjectDir, translatorConfig, message =>
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            var isError = message.Contains("[AI-Translator Error]") || message.Contains("[AI-Translator CRITICAL]");
            var isWarning = message.Contains("[AI-Translator Warning]") ||
                            (message.Contains("[AI-Translator]") && message.Contains("No <SupportedLanguage> tag found"));

            if (isError)
            {
                var cleanMessage = message.Replace("[AI-Translator Error]", "").Replace("[AI-Translator CRITICAL]", "").Trim();
                Log.LogError($"ResxTranslator: {cleanMessage}");
                collectedIssues.Add($"[ERROR] {cleanMessage}");
            }
            else if (isWarning)
            {
                var cleanMessage = message.Replace("[AI-Translator Warning]", "").Replace("[AI-Translator]", "").Trim();
                Log.LogWarning($"ResxTranslator: {cleanMessage}");
                collectedIssues.Add($"[WARNING] {cleanMessage}");
            }
            else
            {
                Log.LogMessage(MessageImportance.High, message);
            }
        }).GetAwaiter().GetResult();

        if (!result.Success)
        {
            Log.LogError($"ResxTranslator fatal error: {result.ErrorMessage}");
            collectedIssues.Add($"[FATAL ERROR] {result.ErrorMessage}");
        }

        var uniqueIssues = collectedIssues.Distinct().ToList();

        if (uniqueIssues.Count > 0)
        {
            TryOpenSummaryLog(uniqueIssues);
        }

        return result.Success;
    }

    private void TryOpenSummaryLog(List<string> issues)
    {
        /* unverändert */
    }
}