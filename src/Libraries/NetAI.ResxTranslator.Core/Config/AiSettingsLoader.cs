using Newtonsoft.Json;

namespace NetAI.ResxTranslator.Core.Config;

public static class AiSettingsLoader
{
    public static AiTestingConfig Load(string projectDir)
    {
        if (string.IsNullOrWhiteSpace(projectDir))
        {
            throw new ArgumentException("ProjectDir darf nicht leer sein.", nameof(projectDir));
        }

        var path = Path.Combine(projectDir, "aitranssettings.json");

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"aitranssettings.json wurde in '{projectDir}' nicht gefunden.", path);
        }

        var json = File.ReadAllText(path);

        var config = JsonConvert.DeserializeObject<AiTestingConfig>(json);

        if (config is null)
        {
            throw new InvalidOperationException($"aitranssettings.json unter '{path}' konnte nicht geparst werden.");
        }

        config.AiConfiguration?.Normalize();

        config.AiConfiguration?.Validate();

        return config;
    }


}