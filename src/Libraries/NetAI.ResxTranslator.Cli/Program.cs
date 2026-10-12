using NetAI.ResxTranslator.Cli.Commands;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(cfg =>
{
    cfg.SetApplicationName("netai-resx");
    cfg.ValidateExamples();

    cfg.AddCommand<TranslateCommand>("translate")
        .WithDescription("Translate .resx files in a project directory using the configured LLM.")
        .WithExample("translate", "./src/MyApp")
        .WithExample("translate", "./src/MyApp", "--config", "./aisettings.json")
        .WithExample("translate", "./src/MyApp", "--json");

    cfg.AddCommand<InitCommand>("init")
        .WithDescription("Create a default aisettings.json in the given directory.");
});


//gggg

return await app.RunAsync(args);