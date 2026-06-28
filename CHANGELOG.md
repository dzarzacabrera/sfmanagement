# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
  - **Commands:** `CreateProjectCommand`, `CreateTaskCommand`, `AssignWorkerToTaskCommand`, `UnassignWorkerFromTaskCommand`, `ChangeTaskStatusCommand`, `CompleteTaskCommand`, `RegisterEvaluationCommand`, `UpdateProjectDetailsCommand`, `UpdateTaskDetailsCommand`.
  - **Queries:** `GetProjectTasksQuery`, `GetRecommendedWorkersQuery` (vector match), `GetWorkerSkillsQuery`, `GetProjectDetailsQuery`.
  - **DTOs:** `TaskDto`, `WorkerDto`, `ProjectDto`, `WorkerSkillDto`.
  - **Tests Unitarios (31 tests):**
    - `SkillVectorTests`: creación, clamping superior/inferior, ApplyImpact (Poor+High, Excellent+Critical, Poor+Critical, Average+Medium), equals.
    - `XpCalculationTests`: multiplicadores de criticalidad, ToBasePoints, impacto combinado.
    - `DomainValidationTests`: UpdateDetails (Queued permite, InProgress bloquea), ChangeStatus (6 transiciones válidas, Finish terminal, inválida).
  - ADR 003 (Skill Vector y Cálculo de XP).

### Changed

- El enum `TaskStatus` fue renombrado a `ProjectTaskStatus` para evitar colisión con `System.Threading.Tasks.TaskStatus`.

### Fixed

- Transiciones de estado Kanban corregidas: `Queued → Finish` directo ya no es válido (debe pasar por InProgress).
