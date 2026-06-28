# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.4.0] - 2026-06-28

### Added

- **Fase 4 — Frontend MVC, Skill Selector Pills, Skills CRUD, y mejoras UX**
  - **Skill Pills Selector Component** (`_SkillSelector.cshtml` + `skill-selector.js`):
    - Input de búsqueda que filtra solo skills no seleccionadas por nombre
    - Pills clickeables que al seleccionarse pasan a zona "Seleccionadas" con input numérico "Nivel" (0-10)
    - Las seleccionadas siempre visibles (no afectadas por el filtro)
    - Botón ✕ para deseleccionar
    - Hidden inputs para `skillPositions[]` + `skillLevels[]` generados automáticamente
  - **Reutilizado en**: `Task/Create` (skills requeridas) y `Worker/Edit` (skills del trabajador)
  - **`SkillController`** (ruta `/Skill`): CRUD completo del catálogo de skills:
    - `Index`: tabla con todas las skills (id, nombre, posición, activo/inactivo), botones Edit y Deactivate/Activate
    - `Create`: formulario para añadir skill → llama stored procedure `sp_add_skill`
    - `Edit`: formulario para cambiar nombre (posición inmutable)
    - `ToggleActive` POST: soft-delete/reactivar skill
  - **`WorkerController.Edit`**: nueva página para editar nombre + skills del worker usando Skill Pills Selector pre-cargado con el vector actual
  - **`ProjectController.Detail`**: nueva página de detalle del proyecto
  - **`Project/Create`**: reemplazado textarea por `<input type="file" accept=".md">` con procesamiento server-side
  - **`kanban.js` sin recarga**: todas las operaciones (AssignWorker, ChangeStatus, SubmitEvaluation) manipulan el DOM in-place en vez de `location.reload()`
  - **`modal.js` con focus trap + animaciones**:
    - Focus trap: Tab cíclico dentro del modal, Shift+Tab al revés
    - Guarda/restaura el elemento enfocado al abrir/cerrar
    - Animación CSS de entrada (scale 0.95→1 + opacity) y salida (reverse)
  - **EvaluationPopup** (`DashboardController`): ahora carga skills reales desde `skills_catalogue` via `GetAllSkillsQuery` en vez de hardcodear "Skill #0"..."Skill #11"
  - **E2E Test `SkillsCrudTests`**: crea skill, verifica aparición en índice, desactiva y reactiva via ToggleActive
  - **E2E Test `WorkerEditFlowTests`**: edita nombre de worker desde formulario y verifica cambio en lista de workers

### Fixed (database)

- **`init.sql`**: función `util_pad_vector` usaba `::vector(target_dim)` con un parámetro en lugar de un literal — corregido a `::vector(1024)` para compatibilidad con Npgsql
- **`init.sql`**: reemplazada directiva `\i database/stored_procedures.sql` (solo psql) con el contenido inline para compatibilidad con ejecución via NpgsqlCommand
- **`init.sql`**: `CREATE TYPE` y `CREATE TABLE` ahora usan bloques `DO $$ ... EXCEPTION WHEN duplicate_object` / `IF NOT EXISTS` para ser idempotentes en `ResetDatabaseAsync()`

### Changed

- **Database schema**: `vector(12)` → `vector(1024)` en `workers.skills_vector` y `tasks.required_skills_vector` (soporte para hasta 1024 skills)
- **`skills_catalogue`**: añadida columna `is_active BOOLEAN DEFAULT TRUE` para soft-delete
- **Seed data**: workers y tasks usan `util_pad_vector()` para crear vectores de 1024 dimensiones con ceros en posiciones no usadas
- **`UpdateWorkerCommand`**: añadido campo opcional `SkillsVector` para actualizar vector desde Worker/Edit
- **`UpdateWorkerCommandHandler`**: actualiza `skills_vector` cuando se proporciona

### Added (database)

- **`util_pad_vector(v float[], target_dim)`**: función helper que rellena con ceros hasta la dimensión objetivo (1024)
- **`sp_add_skill(p_name)`**: stored procedure que asigna la siguiente `vector_position` libre e inserta la skill — sin ALTER TABLE gracias a pre-asignación de 1024 slots

### Added (queries/commands)

- **`GetAllSkillsQuery`** + **`IGetAllSkillsQueryHandler`** + handler ADO.NET: SELECT skills activas (o todas si `IncludeInactive=true`)
- **`SkillDto`**: DTO con Id, Name, VectorPosition, IsActive
- **`CreateSkillCommand`** + handler: llama `CALL sp_add_skill`
- **`ToggleSkillActiveCommand`** + handler: `UPDATE skills_catalogue SET is_active = NOT is_active`

## [0.3.0] - 2026-06-28

### Added

- **Fase 4: Frontend MVC, Vistas Razor, Vanilla JS y E2E Tests**
  - **ViewModels:** `DashboardViewModel`, `AssignWorkerViewModel`, `EvaluationViewModel`, `WorkerHistoryViewModel` con datos planos para las vistas.
  - **Razor Views:**
    - `_Layout.cshtml` con Tailwind CSS CDN v3.4.17 y estructura base HTML5.
    - `_ViewImports.cshtml` y `_ViewStart.cshtml` para configuración global de vistas.
    - Dashboard Kanban (`Index.cshtml`) con 4 columnas responsivas (Queued, In Progress, Blocked, Finish) y targetas de tarea con botones Assign/Evaluate.
    - `Project/Create.cshtml`: formulario de creación de proyecto.
    - `Task/Create.cshtml`: formulario con 12 inputs numéricos de skill (catálogo inmutable desde ViewBag).
    - `Worker/Detail.cshtml`: detalle de worker con tabla de evaluaciones históricas (orden cronológico inverso).
    - `_AssignWorkerModal.cshtml`: modal de asignación con lista de workers recomendados por el algoritmo `<#>`.
    - `_EvaluationModal.cshtml`: modal de evaluación con dropdown de rating y inputs de skill actualizados.
  - **Controllers:**
    - `HomeController` con redirect a `/Dashboard`.
    - `DashboardController` con acciones Index, AssignPopup, AssignWorker (POST), ChangeStatus (POST), EvaluationPopup, SubmitEvaluation (POST).
    - `ProjectController` Create (GET/POST).
    - `TaskController` Create (GET/POST) con carga de catálogo de skills.
    - `WorkerController` Detail con historial de evaluaciones.
  - **Vanilla JS:**
    - `kanban.js`: drag-and-drop visual (cambia clase en columna destino), fetch POST a `/Dashboard/ChangeStatus` y recarga.
    - `modal.js`: apertura/cierre de modales con backdrop + scroll lock vía `#modal-root`.
    - `evaluation.js`: envío de formulario de evaluación vía fetch.
    - Todas las llamadas fetch incluyen `.catch()` para logging de errores.
    - `openEvaluationModal(taskId, projectId)` recibe `projectId` dinámico desde el atributo `data-project-id`.
  - **E2E Tests (5 tests con Playwright + Kestrel real):**
    - `KanbanStateChangeTests`: mueve tarea Queued→InProgress vía fetch + verifica visualmente columna destino.
    - `WorkerAssignmentFlowTests`: asigna worker a tarea vía fetch + verifica modal sin worker asignado + browser assertion en columna.
    - `PerformanceEvaluationFlowTests`: evalúa tarea Finish vía fetch + verifica skills actualizados en modal de evaluación.
    - Fixture `SfManagementE2eFixture` con `Testcontainers.PostgreSql` + `WebApplication.CreateBuilder` + `UseKestrel()` en puerto libre.
    - `ResetDatabaseAsync()` con `TRUNCATE ... RESTART IDENTITY CASCADE` + re-ejecución de `init.sql` para aislamiento entre tests.
  - **Integration Tests actualizados para usar `ResetDatabaseAsync()`**:
    - Tests de mutación (ProjectLifecycle, WorkerAssignment, PerformanceEvaluation, TaskStatusTransition, TaskEditRestriction) se benefician de la limpieza entre colecciones.
  - ADR 005 (Frontend MVC, manejo de estado y modal system).

### Changed

- `DashboardController` POST actions (`AssignWorker`, `ChangeStatus`, `SubmitEvaluation`): se agregó `[IgnoreAntiforgeryToken]` para compatibilidad con fetch de Vanilla JS.
- E2E fixture `InitializeAsync`: se separó `ResetDatabaseAsync()` de `RunInitSqlAsync()` — el TRUNCATE solo se ejecuta en resets (no en primera inicialización donde las tablas aún no existen).
- E2E tests de mutación: llaman `fixture.ResetDatabaseAsync()` al inicio para garantizar estado limpio.
- E2E tests de solo lectura (`Dashboard_ShowsKanbanColumns`, `AssignPopup_ShowsRecommendedWorkers`): no llaman a `ResetDatabaseAsync()`.
- E2E tests de mutación usan `taskId=1` hardcodeado (desde seed de init.sql) en lugar de parsear HTML dinámico.
- Fixture E2E: cambió de `WebApplicationFactory<Program>` a `WebApplication.CreateBuilder` + `UseKestrel()` porque `TestServer` no expone un socket TCP real necesario para Playwright.
- `Program.cs` del Web project: expuesto via `InternalsVisibleTo` para los proyectos de test.

### Fixed

- `EvaluationPopup` en DashboardController: usaba `projectId=1` hardcodeado; corregido para leer `projectId` desde query string.
- `kanban.js`: todas las llamadas `fetch()` ahora tienen `.catch()` para evitar errores silenciosos.
- `SFManagement.E2ETests.csproj`: se agregó referencia a `Microsoft.Playwright` (eliminada en refactor anterior).

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
