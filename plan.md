# 📋 PLAN DE DESARROLLO — SKILL FORGE MANAGEMENT (SFManagement) v1.0

## Stack Confirmado
- **Runtime:** .NET 10 (`net10.0`) — SDK `10.0.301`
- **Solución:** `.sln` clásico
- **Proyectos:** `Domain`, `Application`, `Infrastructure`, `Web` (MVC) + 3 tests
- **BD:** PostgreSQL + pgvector via Docker
- **Tests:** xUnit + FluentAssertions + NSubstitute + Testcontainers + Playwright
- **Idioma BD:** Strict English (skills_catalogue, name, vector_position, description_md)
- **Git Hooks:** Husky.Net — pre-commit (`dotnet format`) + pre-push (`dotnet test`)

---

## 🔷 FASE 1 — Inicialización de Proyectos e Infraestructura Base

| # | Tarea | Descripción |
|---|-------|-------------|
| 1.1 | Crear solución `.sln` clásica | `dotnet new sln -n SFManagement` |
| 1.2 | Crear `SFManagement.Domain` (classlib net10.0) | `dotnet new classlib -o src/SFManagement.Domain` |
| 1.3 | Crear `SFManagement.Application` (classlib net10.0) | `dotnet new classlib -o src/SFManagement.Application` |
| 1.4 | Crear `SFManagement.Infrastructure` (classlib net10.0) | `dotnet new classlib -o src/SFManagement.Infrastructure` |
| 1.5 | Crear `SFManagement.Web` (mvc net10.0) | `dotnet new mvc -o src/SFManagement.Web` |
| 1.6 | Agregar los 4 proyectos a la solución | `dotnet sln add src/*/*.csproj` |
| 1.7 | Referencias entre proyectos: Web→Application→Domain, Infrastructure→Application, Web→Infrastructure | `dotnet add src/.../ reference src/.../` |
| 1.8 | NuGet a Infrastructure: `Npgsql` + `Pgvector` | `dotnet add package Npgsql` |
| 1.9 | NuGet a Web: `Npgsql` (solo para arranque con `UseVector()`) | `dotnet add package Npgsql` |
| 1.10 | Crear manifest de herramientas locales | `dotnet new tool-manifest` |
| 1.11 | Instalar Husky.Net como herramienta local | `dotnet tool install Husky` |
| 1.12 | Inicializar Husky en el repositorio | `dotnet husky init` |
| 1.13 | Configurar hook `pre-commit` para validar formato y análisis de código | `.husky/pre-commit` → `dotnet format --verify-no-changes` |
| 1.14 | Configurar hook `pre-push` para ejecutar tests unitarios | `.husky/pre-push` → `dotnet test tests/SFManagement.UnitTests` |
| 1.15 | Crear `SFManagement.UnitTests` (xUnit net10.0) | `dotnet new xunit -o tests/SFManagement.UnitTests` |
| 1.16 | Crear `SFManagement.IntegrationTests` (xUnit net10.0) | `dotnet new xunit -o tests/SFManagement.IntegrationTests` |
| 1.17 | Crear `SFManagement.E2ETests` (xUnit net10.0) | `dotnet new xunit -o tests/SFManagement.E2ETests` |
| 1.18 | Agregar tests a solución | `dotnet sln add tests/*/*.csproj` |
| 1.19 | NuGet tests unitarios: `FluentAssertions`, `NSubstitute` | `dotnet add package ...` |
| 1.20 | NuGet tests integración: `Testcontainers.PostgreSql`, `Microsoft.AspNetCore.Mvc.Testing` | `dotnet add package ...` |
| 1.21 | NuGet tests E2E: `Playwright` | `dotnet add package ...` |
| 1.22 | Referencias tests → proyectos src | `dotnet add tests/.../ reference src/.../` |
| 1.23 | Crear `database/init.sql` con schema completo (skills_catalogue, projects, workers, project_workers, tasks, task_assignments, performance_evaluations) + enums (task_status, criticality, performance_rating) + seeding (12 skills, 4 workers, 2 projects, 4 tasks) | `database/init.sql` |
| 1.24 | Crear `Dockerfile` multi-stage (build + runtime) | `Dockerfile` |
| 1.25 | Crear `docker-compose.yml` (Postgres:16-pgvector + app MVC) | `docker-compose.yml` |
| 1.26 | Configurar `Nullable enable` en todos los `.csproj` | Editar cada `.csproj` |
| 1.27 | Mejorar `.gitignore` (bin/, obj/, node_modules/, .env) | `.gitignore` |
| 1.28 | ADR 002: documentar estructura de proyectos y schema BD | `docs/002-decision-project-structure.md` |

---

## 🔷 FASE 2 — Capa de Dominio, CQRS Manual y Pruebas Unitarias

| # | Tarea | Archivo / Ruta |
|---|-------|----------------|
| 2.1 | Enum `TaskStatus` (Queued, InProgress, Blocked, Finish) | `Domain/Enums/TaskStatus.cs` |
| 2.2 | Enum `Criticality` (low, medium, high, critical) | `Domain/Enums/Criticality.cs` |
| 2.3 | Enum `PerformanceRating` (Poor, Average, Good, Excellent) con método `ToBasePoints()` → -0.5, 0.0, +0.2, +0.5 | `Domain/Enums/PerformanceRating.cs` |
| 2.4 | Value Object `SkillVector` con: constructor desde `float[]`, clamping `[0.0, 10.0]`, método `ApplyImpact(basePoints, criticalityMultiplier)`, indexador `[position]` | `Domain/ValueObjects/SkillVector.cs` |
| 2.5 | Entidad `SkillCatalogue` (Id, Name, VectorPosition) | `Domain/Entities/SkillCatalogue.cs` |
| 2.6 | Entidad `Worker` (Id, Name, SkillsVector) | `Domain/Entities/Worker.cs` |
| 2.7 | Entidad `Project` (Id, Name, DescriptionMd) | `Domain/Entities/Project.cs` |
| 2.8 | Entidad `ProjectTask` (Id, ProjectId, Title, Description, Criticality, Status, RequiredSkillsVector) | `Domain/Entities/ProjectTask.cs` |
| 2.9 | Entidad `TaskAssignment` (Id, TaskId, WorkerId, AssignedAt) | `Domain/Entities/TaskAssignment.cs` |
| 2.10 | Entidad `PerformanceEvaluation` (Id, TaskId, WorkerId, SkillPosition, Rating, Criticality, BasePoints, Impact, PreviousLevel, NewLevel, CreatedAt) | `Domain/Entities/PerformanceEvaluation.cs` |
| 2.11 | Interface `ICommandHandler<TCommand>` con `Task HandleAsync(TCommand command)` | `Application/Abstractions/ICommandHandler.cs` |
| 2.12 | Interface `IQueryHandler<TQuery, TResult>` con `Task<TResult> HandleAsync(TQuery query)` | `Application/Abstractions/IQueryHandler.cs` |
| 2.13 | Command `CreateProjectCommand` (Name, DescriptionMd) + Result (ProjectId) | `Application/Commands/CreateProjectCommand.cs` |
| 2.14 | Command `CreateTaskCommand` (ProjectId, Title, Description, Criticality, RequiredSkillsVector) + Result (TaskId) | `Application/Commands/CreateTaskCommand.cs` |
| 2.15 | Command `AssignWorkerCommand` (TaskId, WorkerId) | `Application/Commands/AssignWorkerCommand.cs` |
| 2.16 | Command `EvaluateTaskCommand` (TaskId, Evaluations: [{SkillPosition, Rating}]) | `Application/Commands/EvaluateTaskCommand.cs` |
| 2.17 | Command `ChangeTaskStatusCommand` (TaskId, NewStatus) | `Application/Commands/ChangeTaskStatusCommand.cs` |
| 2.18 | Command `UpdateProjectCommand` (ProjectId, Name, DescriptionMd) | `Application/Commands/UpdateProjectCommand.cs` |
| 2.19 | Command `UpdateTaskCommand` (TaskId, Title, Description, Criticality, RequiredSkillsVector) — solo válido si status es Queued | `Application/Commands/UpdateTaskCommand.cs` |
| 2.20 | Command `UpdateWorkerCommand` (WorkerId, Name) | `Application/Commands/UpdateWorkerCommand.cs` |
| 2.21 | Command `UpdateSkillCatalogueCommand` (SkillId, Name) — vector_position inmutable | `Application/Commands/UpdateSkillCatalogueCommand.cs` |
| 2.22 | Query `GetDashboardTasksQuery` (ProjectId) → List<TaskDto> | `Application/Queries/GetDashboardTasksQuery.cs` |
| 2.23 | Query `GetRecommendedWorkersQuery` (ProjectId, TaskId) → List<WorkerScoreDto> | `Application/Queries/GetRecommendedWorkersQuery.cs` |
| 2.24 | Query `GetWorkerHistoryQuery` (WorkerId) → List<EvaluationHistoryDto> | `Application/Queries/GetWorkerHistoryQuery.cs` |
| 2.25 | Query `GetWorkersByProjectQuery` (ProjectId) → List<WorkerDto> | `Application/Queries/GetWorkersByProjectQuery.cs` |
| 2.26 | DTOs: `TaskDto`, `WorkerScoreDto`, `EvaluationHistoryDto`, `WorkerDto` | `Application/DTOs/` |
| 2.27 | Test: `SkillVectorTests` — creación, clamping superior 10.0, clamping inferior 0.0, ApplyImpact (todas las combinaciones criticality × rating) | `tests/SFManagement.UnitTests/SkillVectorTests.cs` |
| 2.28 | Test: `XpCalculationTests` — verificar multiplicadores criticalidad × puntos base | `tests/SFManagement.UnitTests/XpCalculationTests.cs` |
| 2.29 | Test: `DomainValidationTests` — validar estados Kanban, enums, reglas de edición | `tests/SFManagement.UnitTests/DomainValidationTests.cs` |
| 2.30 | ADR 003: documentar diseño de SkillVector y algoritmo XP | `docs/003-decision-skill-vector-xp.md` |
| 2.31 | Actualizar CHANGELOG.md con v0.1.0 | `CHANGELOG.md` |

---

## 🔷 FASE 3 — Infraestructura ADO.NET, Handlers PostgreSQL e Integration Tests

| # | Tarea | Archivo / Ruta |
|---|-------|----------------|
| 3.1 | Interface `INpgsqlConnectionFactory` (GetOpenConnectionAsync) | `Infrastructure/Data/INpgsqlConnectionFactory.cs` |
| 3.2 | Implementación `NpgsqlConnectionFactory` (usa NpgsqlDataSource) | `Infrastructure/Data/NpgsqlConnectionFactory.cs` |
| 3.3 | Configurar `Program.cs` con `NpgsqlDataSourceBuilder`, `UseVector()`, DI de handlers y connection factory | `Web/Program.cs` |
| 3.4 | Handler: `CreateProjectCommandHandler` (INSERT parametrizado) | `Infrastructure/Handlers/Commands/CreateProjectCommandHandler.cs` |
| 3.5 | Handler: `CreateTaskCommandHandler` (INSERT con vector) | `Infrastructure/Handlers/Commands/CreateTaskCommandHandler.cs` |
| 3.6 | Handler: `AssignWorkerCommandHandler` (INSERT task_assignments) | `Infrastructure/Handlers/Commands/AssignWorkerCommandHandler.cs` |
| 3.7 | Handler: `ChangeTaskStatusCommandHandler` (UPDATE status con validación de reglas de transición) | `Infrastructure/Handlers/Commands/ChangeTaskStatusCommandHandler.cs` |
| 3.8 | Handler: `EvaluateTaskCommandHandler` (INSERT evaluations + UPDATE workers.skills_vector con clamping) | `Infrastructure/Handlers/Commands/EvaluateTaskCommandHandler.cs` |
| 3.9 | Handler: `UpdateProjectCommandHandler` (UPDATE projects) | `Infrastructure/Handlers/Commands/UpdateProjectCommandHandler.cs` |
| 3.10 | Handler: `UpdateTaskCommandHandler` (UPDATE tasks — solo si status = Queued) | `Infrastructure/Handlers/Commands/UpdateTaskCommandHandler.cs` |
| 3.11 | Handler: `UpdateWorkerCommandHandler` (UPDATE workers) | `Infrastructure/Handlers/Commands/UpdateWorkerCommandHandler.cs` |
| 3.12 | Handler: `UpdateSkillCatalogueCommandHandler` (UPDATE skills_catalogue — solo name) | `Infrastructure/Handlers/Commands/UpdateSkillCatalogueCommandHandler.cs` |
| 3.13 | Handler: `GetDashboardTasksQueryHandler` (SELECT tasks con JOIN project_workers) | `Infrastructure/Handlers/Queries/GetDashboardTasksQueryHandler.cs` |
| 3.14 | Handler: `GetRecommendedWorkersQueryHandler` (SELECT con `<#>` de pgvector, restringido por project_workers) | `Infrastructure/Handlers/Queries/GetRecommendedWorkersQueryHandler.cs` |
| 3.15 | Handler: `GetWorkerHistoryQueryHandler` (SELECT performance_evaluations) | `Infrastructure/Handlers/Queries/GetWorkerHistoryQueryHandler.cs` |
| 3.16 | Handler: `GetWorkersByProjectQueryHandler` (SELECT workers JOIN project_workers) | `Infrastructure/Handlers/Queries/GetWorkersByProjectQueryHandler.cs` |
| 3.17 | Mappers manuales: `DataReaderRowParser` con métodos de extensión para hidratar DTOs desde `NpgsqlDataReader` | `Infrastructure/Mappers/DataReaderMapper.cs` |
| 3.18 | Integration Test: fixture con `Testcontainers.PostgreSql` + `WebApplicationFactory` | `tests/Integration/Shared/SfManagementIntegrationFixture.cs` |
| 3.19 | Integration Test: CreateProject + actualizar proyecto | `tests/Integration/ProjectLifecycleTests.cs` |
| 3.20 | Integration Test: Asignar worker + consultar recomendados (validar `<#>` score) | `tests/Integration/WorkerAssignmentTests.cs` |
| 3.21 | Integration Test: Evaluar tarea + verificar clamping en BD | `tests/Integration/PerformanceEvaluationTests.cs` |
| 3.22 | Integration Test: Transición estados Kanban (Queued→InProgress→Finish) | `tests/Integration/TaskStatusTransitionTests.cs` |
| 3.23 | Integration Test: Editar tarea solo cuando status es Queued | `tests/Integration/TaskEditRestrictionTests.cs` |
| 3.24 | ADR 004: documentar implementación ADO.NET + pgvector | `docs/004-decision-adonet-pgvector-implementation.md` |
| 3.25 | Actualizar CHANGELOG.md con v0.2.0 | `CHANGELOG.md` |

---

## 🔷 FASE 4 — Frontend: Vistas Razor, Kanban, Vanilla JS y E2E Tests

| # | Tarea | Archivo / Ruta |
|---|-------|----------------|
| 4.1 | Layout principal con Tailwind CSS (CDN v3), meta viewport, fuente sistema | `Web/Views/Shared/_Layout.cshtml` |
| 4.2 | `DashboardController` (Index: carga tareas del proyecto, AssignPopup: GET workers recomendados) | `Web/Controllers/DashboardController.cs` |
| 4.3 | Vista Dashboard Kanban: 4 columnas responsivas (Queued, In Progress, Blocked, Finish) | `Web/Views/Dashboard/Index.cshtml` |
| 4.4 | `ProjectController` (Create, Detail) | `Web/Controllers/ProjectController.cs` |
| 4.5 | Vista Create Project: formulario con nombre + subida archivo .md | `Web/Views/Project/Create.cshtml` |
| 4.6 | `TaskController` (Create, ChangeStatus) | `Web/Controllers/TaskController.cs` |
| 4.7 | Vista Create Task: dropdown skills desde catálogo cerrado (posiciones fijas 0-11), selector criticalidad | `Web/Views/Task/Create.cshtml` |
| 4.8 | `WorkerController` (Detail con historial) | `Web/Controllers/WorkerController.cs` |
| 4.9 | Vista Worker Detail: tabla cronológica inversa de evaluaciones | `Web/Views/Worker/Detail.cshtml` |
| 4.10 | Partial View: Popup asignación worker (glassmorphism backdrop-blur-sm, hoja inferior móvil, centrado escritorio) | `Web/Views/Dashboard/_AssignWorkerModal.cshtml` |
| 4.11 | Partial View: Modal evaluación desempeño (rating por skill involucrada, 4 opciones cualitativas) | `Web/Views/Dashboard/_EvaluationModal.cshtml` |
| 4.12 | ViewModels: `DashboardViewModel`, `TaskViewModel`, `AssignWorkerViewModel`, `EvaluationViewModel`, `WorkerHistoryViewModel` | `Web/ViewModels/` |
| 4.13 | **Vanilla JS — kanban.js**: fetch asíncrono para cambiar estado tarea, actualizar columna sin recargar | `Web/wwwroot/js/kanban.js` |
| 4.14 | **Vanilla JS — modal.js**: abrir/cerrar popup, focus trap, animación entrada/salida, click outside para cerrar | `Web/wwwroot/js/modal.js` |
| 4.15 | **Vanilla JS — evaluation.js**: submit evaluación asíncrono, mostrar resultado | `Web/wwwroot/js/evaluation.js` |
| 4.16 | E2E Test: navegar a Dashboard, cambiar estado tarea | `tests/E2E/KanbanStateChangeTests.cs` |
| 4.17 | E2E Test: abrir popup asignación, seleccionar worker, confirmar | `tests/E2E/WorkerAssignmentFlowTests.cs` |
| 4.18 | E2E Test: completar tarea, rellenar evaluación, verificar redirección | `tests/E2E/PerformanceEvaluationFlowTests.cs` |
| 4.19 | Actualizar CHANGELOG.md con v0.3.0 | `CHANGELOG.md` |

---

## 🔷 FASE 5 — Validación Extrema de XP, Auditoría OWASP y Cierre

| # | Tarea | Archivo / Ruta |
|---|-------|----------------|
| 5.1 | Test extremo: worker con skill en 6.0, recibe evaluación "mal" con criticidad "high" (×1.5), debe bajar a 5.25 (6.0 + (-0.5 × 1.5) = 5.25) | `tests/Unit/SkillVectorEdgeCaseTests.cs` |
| 5.2 | Test extremo: worker con skill en 0.0, recibe evaluación "mal" con criticidad "critical" (×2.0), debe quedar en 0.0 (clamping inferior) | `tests/Unit/SkillVectorEdgeCaseTests.cs` |
| 5.3 | Test extremo: worker con skill en 9.5, recibe "muy_bien" con criticidad "critical" (×2.0), debe quedar en 10.0 (clamping superior: 9.5 + 0.5×2.0 = 10.5 → clamp a 10.0) | `tests/Unit/SkillVectorEdgeCaseTests.cs` |
| 5.4 | ADR 005: documentar algoritmo de cálculo de XP con fórmula, multiplicadores y clamping | `docs/005-decision-xp-calculation-algorithm.md` |
| 5.5 | Auditoría OWASP: revisar inyección SQL (parametrización), XSS en Razor (Html.Encode), saneamiento subida .md, cabeceras HTTP security | `docs/006-owasp-security-audit.md` |
| 5.6 | Actualizar `CHANGELOG.md` con v1.0.0 (Added: todas las fases, Fixed: clamping edge cases, Security: OWASP mitigaciones) | `CHANGELOG.md` |
| 5.7 | Actualizar `README.md` con descripción técnica, instrucciones de despliegue (docker-compose up), y enlaces a ADRs | `README.md` |

---

## 📋 Reglas de Edición por Entidad

### SkillsCatalogue
- Editable solo `name`. `vector_position` es inmutable (cambiarlo rompe vectores existentes).

### Project
- Editables `name` y `description_md`.
- `project_workers` (alcance) se pueden añadir/remover libremente.

### Worker
- Editable solo `name`. `skills_vector` lo recalcula el sistema vía evaluaciones (no manual).

### Task

| Campo | Queued | In Progress | Blocked | Finish |
|-------|--------|-------------|---------|--------|
| title, description | ✅ editable | ❌ | ❌ | ❌ inmutable |
| criticality | ✅ editable | ❌ | ❌ | ❌ inmutable |
| required_skills_vector | ✅ editable | ❌ | ❌ | ❌ inmutable |
| assigned_worker | ✅ editable | ❌ | ❌ | ❌ inmutable |
| status | ✅ any transition | ✅ solo → Blocked o → Finish | ✅ solo → Queued o → In Progress | ❌ terminal |

**Regla clave:** Para editar una tarea en *In Progress* o *Blocked*, debe revertirse a *Queued*. Una tarea *Finish* es **inmutable** porque ya tiene evaluaciones asociadas.

---

## 📁 Estructura Final Completa

```
C:\Users\leisa\source\repos\sfmanagement\
│
├── .github\agents\                    → 5 agentes (existente)
├── .husky\
│   ├── pre-commit                     → dotnet format --verify-no-changes
│   └── pre-push                       → dotnet test tests/SFManagement.UnitTests
├── .config\
│   └── dotnet-tools.json              → Husky tool manifest
├── database\
│   └── init.sql                       → Schema + seeding
├── docs\
│   ├── 001-decision-arquitectura-persistencia.md  (existente)
│   ├── 002-decision-project-structure.md
│   ├── 003-decision-skill-vector-xp.md
│   ├── 004-decision-adonet-pgvector-implementation.md
│   ├── 005-decision-xp-calculation-algorithm.md
│   └── 006-owasp-security-audit.md
│
├── src\
│   ├── SFManagement.Domain\
│   │   ├── Entities\                   → Worker, Project, ProjectTask, etc.
│   │   ├── ValueObjects\              → SkillVector
│   │   └── Enums\                     → TaskStatus, Criticality, PerformanceRating
│   │
│   ├── SFManagement.Application\
│   │   ├── Abstractions\              → ICommandHandler, IQueryHandler
│   │   ├── Commands\                  → CreateProjectCommand, etc.
│   │   ├── Queries\                   → GetDashboardTasksQuery, etc.
│   │   └── DTOs\                      → TaskDto, WorkerScoreDto, etc.
│   │
│   ├── SFManagement.Infrastructure\
│   │   ├── Data\                      → INpgsqlConnectionFactory, impl
│   │   ├── Handlers\Commands\         → CreateProjectCommandHandler, etc.
│   │   ├── Handlers\Queries\          → GetDashboardTasksQueryHandler, etc.
│   │   └── Mappers\                   → DataReaderMapper
│   │
│   └── SFManagement.Web\
│       ├── Controllers\               → DashboardController, ProjectController, etc.
│       ├── Views\Shared\              → _Layout.cshtml
│       ├── Views\Dashboard\           → Index.cshtml, partials
│       ├── Views\Project\             → Create.cshtml
│       ├── Views\Task\                → Create.cshtml
│       ├── Views\Worker\              → Detail.cshtml
│       ├── ViewModels\                → DashboardViewModel, etc.
│       └── wwwroot\js\                → kanban.js, modal.js, evaluation.js
│
├── tests\
│   ├── SFManagement.UnitTests\        → SkillVectorTests, XpCalculationTests
│   ├── SFManagement.IntegrationTests\ → ProjectLifecycleTests, etc.
│   └── SFManagement.E2ETests\         → KanbanStateChangeTests, etc.
│
├── SFManagement.sln
├── Dockerfile
├── docker-compose.yml
├── AGENTS.md              (existente)
├── CHANGELOG.md
├── README.md
└── LICENSE                (existente)
```

---

**Total: ~91 tareas** distribuidas en 5 fases.
