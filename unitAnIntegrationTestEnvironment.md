1. Central Package Management 

cli: 
dotnet new packagesprops
# 1. Das korrekte Tool global installieren
dotnet tool install --global CentralisedPackageConverter

# 2. Den Konverter im Ordner Ihrer Solution ausführen
central-pkg-converter .




2. solution struktur mit test projekten.

MyProjekt/
├── MyProjekt.slnx                        # Die moderne XML-Solution-Datei
│
├── 📁 src/                               # PRODUKTIVCODE
│   ├── 📁 Libraries/                     # Kernarchitektur (Domain-Driven / Clean Architecture)
│   │   ├── MyProjekt.Core/               # Domänen-Entitäten, Value Objects, Core-Interfaces
│   │   ├── MyProjekt.Application/        # Anwendungslogik, Use Cases, CQRS (Handlers)
│   │   └── MyProjekt.Infrastructure/     # EF Core, Repositories, Datei-I/O, Externe APIs
│   │
│   ├── 📁 UI/                            # Benutzeroberflächen
│   │   ├── MyProjekt.BlazorApp/          # Web-Frontend (Blazor WebAssembly / Server)
│   │   ├── MyProjekt.WpfApp/             # Windows Desktop App (net8.0-windows)
│   │   └── MyProjekt.MauiApp/            # Cross-Plattform Mobile/Desktop (Android, iOS, macOS)
│   │
│   ├── 📁 Apis/                          # Netzwerk-Schnittstellen
│   │   ├── MyProjekt.WebApi/             # REST API (Einstiegspunkt für Blazor/WPF, Swagger, JWT)
│   │   └── MyProjekt.GrpcService/        # gRPC für schnelle Inter-Service-Kommunikation
│   │
│   ├── 📁 CLI/                           # Werkzeuge und Wartung
│   │   ├── MyProjekt.ConsoleApp/         # Einmalige Daten-Migratoren oder Admin-Skripte
│   │   └── MyProjekt.CliTool/            # Entwickler-Tooling für die Kommandozeile
│   │
│   ├── 📁 Workers/                       # Async Hintergrund-Verarbeitung
│   │   ├── MyProjekt.BackgroundWorker/   # .NET Worker Service (zeitgesteuerte Cronjobs)
│   │   └── MyProjekt.QueueConsumer/      # Nachrichtenschleife (RabbitMQ, Azure Service Bus)
│   │
│   ├── 📁 Cloud/                         # Serverless Computing
│   │   └── MyProjekt.AzureFunctions/     # Event-getriebene Cloud-Funktionen
│   │
│   └── 📁 Shared/                        # Projektübergreifender Vertragscode
│       └── MyProjekt.Contracts/          # Gemeinsame DTOs, Requests/Responses & Event-Klassen
│
└── 📁 tests/                             # TESTCODE
    ├── 📁 UnitTests/                     # Schnell, isoliert, 100% In-Memory (kein I/O)
    │   ├── MyProjekt.Libraries.UnitTests/# Testet Core & Application (Mocks für Infrastructure)
    │   ├── MyProjekt.UI.UnitTests/       # Testet WPF-ViewModels, Blazor-Komponenten (bUnit)
    │   └── MyProjekt.Shared.UnitTests/   # Testet Validierungen von DTOs oder Extensions
    │
    └── 📁 IntegrationTests/              # Träge, testen echtes Zusammenspiel und I/O
        ├── MyProjekt.Infrastructure.IntegrationTests/ # Testet EF Core gegen echte Test-DBs (Docker)
        ├── MyProjekt.Apis.IntegrationTests/           # Testet WebApi-Endpunkte via WebApplicationFactory
        └── MyProjekt.Workers.IntegrationTests/        # Testet Queue-Verarbeitung und Hintergrund-Flüsse






innter tesatproject:

MyProjekt.Libraries.UnitTests/ (Das Projekt)
├── 📁 Core/                      # Ordner spiegelt "MyProjekt.Core"
│   ├── 📁 Domain/
│   └── 📁 Services/
│       └── OrderServiceTests.cs  # Testet Klassen aus MyProjekt.Core.Services
│
└── 📁 Application/               # Ordner spiegelt "MyProjekt.Application"
    ├── 📁 UseCases/
    └── 📁 Validators/
        └── CreateUserValidatorTests.cs



# Architektur-Abhängigkeiten (Clean Architecture / DDD)

| Projekt-Kategorie | Darf referenzieren | Darf niemals referenzieren |
| :--- | :--- | :--- |
| **Libraries.Core** | Nichts (reines C#). | Frameworks, Datenbanken, APIs. |
| **Libraries.Application** | `Core`, `Shared.Contracts`. | `Infrastructure`, UI-Projekte. |
| **Libraries.Infrastructure** | `Application` (implementiert deren Interfaces). | UI-Projekte, `WebApi`. |
| **Apis / UI / Workers** | `Application`, `Shared.Contracts` *(bindet Infrastructure in der Program.cs via DI an)*. | Andere UI- oder Web-Projekte untereinander. |
| **UnitTests** | Das konkrete Projekt, das es isoliert testen soll. | `Infrastructure` *(alles wird gemockt!)*. |
| **IntegrationTests** | `WebApi` oder `Infrastructure`. | *Darf direkt im Code verwendet werden, um echte Datenbanken/APIs anzusprechen.* |

 
WPF-Unit-Tests Besonderheit:Dein MyProjekt.UI.UnitTests-Projekt muss für WPF-Tests das SDK Microsoft.NET.Sdk nutzen, aber das TargetFramework net8.0-windows besitzen und <UseWpf>true</UseWpf> in der .csproj aktiviert haben, da WPF-ViewModels zur Instanziierung oft Windows-Bibliotheken benötigen.