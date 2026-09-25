using Newtonsoft.Json;

namespace NetAI.ResxTranslator.Core.Config;

public record AiTestingConfig
{
    [JsonConstructor]
    public AiTestingConfig(string version, AiConfigurationConfig aiConfiguration, TranslatorConfig? translator = null)
    {
        Version = version;
        AiConfiguration = aiConfiguration;
        Translator = translator;
    }

    [JsonProperty("version")] public string Version { get; set; }

    [JsonProperty("aiConfiguration")] public AiConfigurationConfig AiConfiguration { get; set; }

    [JsonProperty("translator")] public TranslatorConfig? Translator { get; set; }
}


public record AiConfigurationConfig
{
    [JsonConstructor]
    public AiConfigurationConfig(string model, double temperature, string systemPrompt)
    {
        Model = model;
        Temperature = temperature;
        SystemPrompt = systemPrompt;
    }

    public string Model { get; set; }
    public double Temperature { get; set; }
    public string SystemPrompt { get; set; }
}