using Newtonsoft.Json;

namespace NetAI.ResxTranslator.Core.Config;

public record AiTestingConfig
{
    [JsonConstructor]
    public AiTestingConfig(string version, LlmConnectionSettings aiConfiguration, TranslatorConfig? translator = null)
    {
        Version = version;
        AiConfiguration = aiConfiguration;
        Translator = translator;
    }

    [JsonProperty("version")] public string Version { get; set; }

    [JsonProperty("aiConfiguration")] public LlmConnectionSettings AiConfiguration { get; set; }

    [JsonProperty("translator")] public TranslatorConfig? Translator { get; set; }
}