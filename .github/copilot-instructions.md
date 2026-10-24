# Copilot Instructions

## Build, test, run

.NET 10 solution: `LatinoNetOnline.Backend.sln`. There is no linter config.

- Restore: CI first copies local NuGet packages (`cp -r offline-packages packages`, since `NuGet.Config` sets `globalPackagesFolder=./packages`; the `AivenEcommerce.V1.Modules.GitHub*` packages only exist in `offline-packages`), then `dotnet restore`.
- Build: `dotnet build src/Bootstrapper/LatinoNetOnline.Backend.Bootstrapper/LatinoNetOnline.Backend.Bootstrapper.csproj -c Release`
- Tests (xUnit + Moq + EF Core InMemory; only the Webinars module has tests):
  - All: `dotnet test src/Modules/Webinars/LatinoNetOnline.Backend.Modules.Webinars.Tests`
  - Single: `dotnet test src/Modules/Webinars/LatinoNetOnline.Backend.Modules.Webinars.Tests --filter "FullyQualifiedName~ProposalServiceTests.MethodName"`
- Run: the Bootstrapper project (`dotnet run --project src/Bootstrapper/LatinoNetOnline.Backend.Bootstrapper`). Needs PostgreSQL via `ConnectionStrings:Default`; secrets (GitHub, Google, Mailjet, Identity) come from user secrets/env and are substituted into `appsettings.json` at deploy (`.github/workflows/deploy.yml`, push to `main`).

## Architecture

Modular monolith. `src/Bootstrapper` is the only host (`Program.cs` + `Startup`); it references each module's `*.Api` project.

- `src/Modules/<Name>/<Name>.Api` – controllers, requests, and the `Module` subclass (e.g. `WebinarsModule`). Modules: Webinars, Links, Notifications.
- `src/Modules/<Name>/<Name>.Core` – services, entities, DTOs, validators (FluentValidation), DbContext, event handlers.
- `src/Shared/` – `Abstractions` (interfaces: events, options), `Commons` (`OperationResult`), `Infrastructure` (module loading, in-process event/messaging/module-request plumbing).

Module lifecycle (`Shared.Infrastructure.DependencyInjection.Module`): modules are discovered by reflection (every non-abstract subclass of `Module` in loaded assemblies) and invoked through three hooks: `Load` (register services/DbContext), `Configure` (middleware, static files, subscriptions), `InitialConfiguration` (run at host start, e.g. `SeedData.EnsureSeedData`). A new module needs a `Module` subclass and a project reference from the Bootstrapper.

Cross-module communication never references another module's Core directly:
- Request/response: `app.UseModuleRequests().Subscribe<TInput>("modules/<path>", ...)` in `Configure`.
- Broadcast events: `app.UseModuleBroadcast().Subscribe<TEvent>(...)`; handlers implement `IEventHandler<TEvent>` and are resolved from a new scope.

Each module owns its own DbContext (PostgreSQL/Npgsql). Webinars' `ApplicationDbContext` uses `MigrationsAssembly("LatinoNetOnline.Backend.Bootstrapper")`, and design-time factories (`*DbContextFactory`) load `appsettings.json` from `../../Bootstrapper/LatinoNetOnline.Backend.Bootstrapper` relative to the current directory, so run `dotnet ef` from the module's Core project directory.

## Conventions

- Services, their interfaces, and event handlers are `internal`; `AssemblyInfo.cs` exposes internals to the Bootstrapper (Core also to the Tests project). Keep new types internal and register them as scoped in the module's `Load`.
- Services return `OperationResult<T>` (Shared.Commons) and are written as CSharpFunctionalExtensions railway chains: `Validate(input).Map(...).Map(...).FinallyOperationResult()`. Follow this style (see `SpeakerService`, `ProposalService`) rather than throwing exceptions.
- Input validation uses a `*Validator` class per input DTO in `Core/Validators`, wrapped via `ValidationResultExtensions`.
- Folder layout inside Core: `Dto/<Area>/` (`Create*Input`, `Update*Input`, `*Dto`, `*Filter`), `Events/` (event inputs; `Events/External/` for events received from other modules), `Extensions/` (entity↔DTO conversion such as `ConvertToDto`).
- User-facing log/error messages are in Spanish; keep that language.
- Tests use `MockObject.cs` helpers in the Tests project to build services over an in-memory DbContext.
