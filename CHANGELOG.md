# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.0] - 2026-06-28

### Added

- **Fase 3: Infraestructura ADO.NET, Handlers PostgreSQL e Integration Tests**
  - **Connection Factory:** `INpgsqlConnectionFactory` / `NpgsqlConnectionFactory` con `NpgsqlDataSource` + `UseVector()` para pgvector.
  - **DataReaderMapper:** Helper interno con métodos tipados (`GetInt32`, `GetString`, `GetVector`, `GetEnum`) para hidratación manual desde `NpgsqlDataReader`.
  - **9 Command Handlers ADO.NET:**
    - `CreateProjectCommandHandler` (INSERT RETURNING id)
    - `CreateTaskCommandHandler` (INSERT con vector)
    - `AssignWorkerCommandHandler` (INSERT con ON CONFLICT DO UPDATE)
    - `ChangeTaskStatusCommandHandler` (carga entidad, valida transición vía dominio, UPDATE)
    - `EvaluateTaskCommandHandler` (INSERT evaluations + UPDATE skills_vector con clamping)
    - `UpdateProjectCommandHandler` (UPDATE projects)
    - `UpdateTaskCommandHandler` (carga entidad, valida solo Queued, UPDATE)
    - `UpdateWorkerCommandHandler` (UPDATE workers)
    - `UpdateSkillCatalogueCommandHandler` (UPDATE skills_catalogue)
  - **4 Query Handlers ADO.NET:**
    - `GetDashboardTasksQueryHandler` (SELECT con LEFT JOIN task_assignments + workers)
    - `GetRecommendedWorkersQueryHandler` (SELECT con producto escalar `<#>` de pgvector, restringido por project_workers)
    - `GetWorkerHistoryQueryHandler` (SELECT performance_evaluations JOIN tasks)
    - `GetWorkersByProjectQueryHandler` (SELECT workers JOIN project_workers)
  - **DI Registration:** Método de extensión `AddInfrastructure()` que registra DataSource, ConnectionFactory, y todos los handlers.
  - **Integration Tests (14 tests):**
    - `ProjectLifecycleTests`: crear y actualizar proyecto.
    - `WorkerAssignmentTests`: asignar worker, recomendados por `<#>` ordenados, workers por proyecto.
    - `PerformanceEvaluationTests`: evaluación completa con actualización de skill, evaluar tarea no-finalizada lanza error.
    - `TaskStatusTransitionTests`: Queued→InProgress→Finish, Queued→Finish inválido, Finish→Any inválido, Blocked→Queued.
    - `TaskEditRestrictionTests`: editar Queued OK, editar InProgress/Finish lanza error.
  - ADR 004 (Implementación ADO.NET Puro con Npgsql y Pgvector).

### Fixed

- Database `init.sql`: se agregó `OVERRIDING SYSTEM VALUE` y `ALTER TABLE ... RESTART WITH` para compatibilidad con `GENERATED ALWAYS AS IDENTITY` en tests de integración.
- `DataReaderMapper.GetEnum`: cambiado a `ignoreCase: true` para mapear valores en minúscula de la BD (`'low'`, `'medium'`) a enums PascalCase de C#.
- Lectura/escritura del tipo `vector`: corregido el mapeo mediante `Pgvector.Vector` (no `float[]` directo).

## [0.1.0] - 2026-06-28

### Added

- **Fase 1: Inicialización del proyecto**
  - Solución .NET 10 con 4 proyectos (Domain, Application, Infrastructure, Web MVC) en formato clásico `.sln`.
  - 3 proyectos de test (UnitTests, IntegrationTests, E2ETests) con paquetes NuGet: xUnit, FluentAssertions, NSubstitute, WebApplicationFactory, Testcontainers.PostgreSql, Playwright.
  - Husky.Net hooks: pre-commit (`dotnet format --verify-no-changes`) y pre-push (`dotnet test tests/SFManagement.UnitTests`).
  - Esquema completo de base de datos en `database/init.sql` con tablas, enums, índices y seeding (12 skills, 4 workers, 2 proyectos, 4 tareas).
  - Dockerfile multi-stage y docker-compose.yml con PostgreSQL 16 + pgvector.
  - `.gitignore` y `AGENTS.md` con directrices de desarrollo.
  - ADR 001 (persistencia ADO.NET puro) y ADR 002 (estructura de proyectos y schema BD).

- **Fase 2: Dominio, Aplicación y Tests Unitarios**
  - **Enums:** `ProjectTaskStatus` (Queued, InProgress, Blocked, Finish), `Criticality` (Low, Medium, High, Critical), `PerformanceRating` (Poor, Average, Good, Excellent) con método `ToBasePoints()`.
  - **Value Object:** `SkillVector` con clamping 0–10, `ApplyImpact()`, `CalculateCriticalityMultiplier()`.
  - **Entidades:** `SkillCatalogue`, `Worker`, `Project`, `ProjectTask` (con `ChangeStatus()` y `UpdateDetails()`), `TaskAssignment`, `PerformanceEvaluation`.
  - **CQRS Manual:** Interfaces `ICommandHandler<T>` y `IQueryHandler<T,R>`.
  - **Commands:** `CreateProjectCommand`, `CreateTaskCommand`, `AssignWorkerCommand`, `ChangeTaskStatusCommand`, `EvaluateTaskCommand`, `UpdateProjectCommand`, `UpdateTaskCommand`, `UpdateWorkerCommand`, `UpdateSkillCatalogueCommand`.
  - **Queries:** `GetDashboardTasksQuery`, `GetRecommendedWorkersQuery` (vector match `<#>`), `GetWorkerHistoryQuery`, `GetWorkersByProjectQuery`.
  - **DTOs:** `TaskDto`, `WorkerDto`, `WorkerScoreDto`, `EvaluationHistoryDto`.
  - **Tests Unitarios (31 tests):**
    - `SkillVectorTests`: creación, clamping superior/inferior, ApplyImpact (Poor+High, Excellent+Critical, Poor+Critical, Average+Medium), equals.
    - `XpCalculationTests`: multiplicadores de criticalidad, ToBasePoints, impacto combinado.
    - `DomainValidationTests`: UpdateDetails (Queued permite, InProgress bloquea), ChangeStatus (6 transiciones válidas, Finish terminal, inválida).
  - ADR 003 (Skill Vector y Cálculo de XP).

### Changed

- El enum `TaskStatus` fue renombrado a `ProjectTaskStatus` para evitar colisión con `System.Threading.Tasks.TaskStatus`.

### Fixed

- Transiciones de estado Kanban corregidas: `Queued → Finish` directo ya no es válido (debe pasar por InProgress).
