using System;
using System.Collections.Generic;

namespace NetAI.ResxTranslator.Core.Config;

/// <summary>
///     Die Property-Namen werden von Newtonsoft standardmäßig case-insensitiv gemappt
///     ("supportedLanguages" -> SupportedLanguages).
/// </summary>
public class TranslatorConfig
{
    public string Mode { get; set; } = "all";

    /// <summary>
    ///     DEPRECATED – bitte <see cref="AiTestingConfig.AiConfiguration"/> mit
    ///     <see cref="LlmConnectionSettings.ApiKey"/> oder
    ///     <see cref="LlmConnectionSettings.ApiKeyEnvVar"/> verwenden.
    /// </summary>
    [Obsolete("Use AiConfiguration.ApiKey or AiConfiguration.ApiKeyEnvVar instead.")]
    public string ApiKey { get; set; } = string.Empty;

    public string Context { get; set; } = string.Empty;
    public string GlossaryPath { get; set; } = string.Empty;
    public string DefaultLanguage { get; set; } = "en";
    public List<string> SupportedLanguages { get; set; } = new();
}