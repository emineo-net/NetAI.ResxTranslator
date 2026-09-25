using System.Windows;
using NetAI.ResxTranslator.Core.Services;

namespace WpftranlationTestApp;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void TestButton_OnClick(object sender, RoutedEventArgs e)
    {

        //bool ok = ResxTranslatorRunner.Run(@"C:\Users\steph\source\repos\NetAI.ResxTranslator\src\UI\WpftranlationTestApp");

        var runner = new ResxTranslatorRunner
        {
            ProjectDir = @"C:\Users\steph\source\repos\NetAI.ResxTranslator\src\UI\WpftranlationTestApp",
            ApiKey = "sk-...",
            AppContext = "Rechnungs-Verwaltung für KMUs",
            //GlossaryPath = @"C:\Users\steph\source\repos\NetAI.ResxTranslator\src\UI\WpftranlationTestApp\aisettings.json",
            SupportedLanguages = "en, de, it" // optional: überschreibt <SupportedLanguage> aus der .csproj
        };
        var ok = await runner.Run(msg => Console.WriteLine($"[Info]  {msg}"), msg => Console.WriteLine($"[Fehler] {msg}"));
    }
}