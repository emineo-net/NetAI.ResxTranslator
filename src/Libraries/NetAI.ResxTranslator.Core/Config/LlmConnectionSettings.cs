using System;
using Newtonsoft.Json;

namespace NetAI.ResxTranslator.Core.Config;

/// <summary>
/// Definiert die Verbindung zu einem OpenAI-kompatiblen LLM-Endpoint.
/// Funktioniert für lokale Server (llama.cpp, LM Studio, Ollama, …) und
/// gehostete APIs (OpenAI, Azure OpenAI, …) – die Struktur ist immer identisch.
/// </summary>
public class LlmConnectionSettings
{
    /// <summary>Basis-URL des Endpoints. Muss mit "/" enden.</summary>
    [JsonProperty("baseUrl")]
    public string BaseUrl { get; set; } = "http://localhost:8080/";

    /// <summary>Modell-Bezeichner (wird von manchen lokalen Servern ignoriert).</summary>
    [JsonProperty("model")]
    public string Model { get; set; } = "gpt-4o";

    /// <summary>Sampling-Temperatur (0.0 = deterministisch, 2.0 = maximal kreativ).</summary>
    [JsonProperty("temperature")]
    public double Temperature { get; set; } = 0.2;

    /// <summary>HTTP-Timeout in Minuten.</summary>
    [JsonProperty("timeoutMinutes")]
    public int TimeoutMinutes { get; set; } = 5;

    /// <summary>System-Prompt, der die KI-Rolle und -Regeln definiert.</summary>
    [JsonProperty("systemPrompt")]
    public string? SystemPrompt { get; set; }

    /// <summary>Inline-API-Key. Gegenseitig ausschließend mit <see cref="ApiKeyEnvVar"/>.</summary>
    [JsonProperty("apiKey")]
    public string? ApiKey { get; set; }

    /// <summary>Name der Umgebungsvariable mit dem API-Key. Gegenseitig ausschließend mit <see cref="ApiKey"/>.</summary>
    [JsonProperty("apiKeyEnvVar")]
    public string? ApiKeyEnvVar { get; set; }

    /// <summary>Liefert den effektiven API-Key (Umgebungsvariable schlägt Inline-Wert).</summary>
    public string? ResolveApiKey()
    {
        if (!string.IsNullOrWhiteSpace(ApiKeyEnvVar))
        {
            var fromEnv = Environment.GetEnvironmentVariable(ApiKeyEnvVar);
            if (!string.IsNullOrWhiteSpace(fromEnv)) return fromEnv;
        }

        return string.IsNullOrWhiteSpace(ApiKey) ? null : ApiKey;
    }


    /// <summary>
    /// Ersetzt leere Strings durch die Default-Werte, damit das JSON alle Felder
    /// enthalten darf, ohne dass "nicht gesetzt" als Fehler gilt.
    /// </summary>
    public void Normalize()
    {
        if (string.IsNullOrWhiteSpace(BaseUrl)) BaseUrl = "http://localhost:8080/";
        if (string.IsNullOrWhiteSpace(Model)) Model = "gpt-4o";
        if (TimeoutMinutes <= 0) TimeoutMinutes = 5;
        if (SystemPrompt is null) SystemPrompt = string.Empty;
        if (ApiKey is null) ApiKey = string.Empty;
        if (ApiKeyEnvVar is null) ApiKeyEnvVar = string.Empty;
    }


    /// <summary>Validiert Pflichtwerte, Bereiche und die gegenseitige Ausschließung der Key-Felder.</summary>
    public void Validate()
    {
        // baseUrl ist nach Normalize() garantiert nicht mehr leer
        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out _))
            throw new InvalidOperationException($"aiConfiguration.baseUrl is not a valid absolute URI: '{BaseUrl}'.");
        if (!BaseUrl.EndsWith("/", StringComparison.Ordinal))
            throw new InvalidOperationException("aiConfiguration.baseUrl must end with a trailing slash.");
        if (Temperature < 0 || Temperature > 2)
            throw new InvalidOperationException("aiConfiguration.temperature must be between 0 and 2.");

        var hasInline = !string.IsNullOrWhiteSpace(ApiKey);
        var hasEnv = !string.IsNullOrWhiteSpace(ApiKeyEnvVar);
        if (hasInline && hasEnv)
            throw new InvalidOperationException(
                "aiConfiguration.apiKey and aiConfiguration.apiKeyEnvVar are mutually exclusive – set only one.");
    }


}