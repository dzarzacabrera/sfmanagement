# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.9.0] - 2026-07-16

### Changed

- **Status rename: Blocked → Test** — The `ProjectTaskStatus` enum value was renamed from `Blocked` to `Test`. The Kanban column order changed to **Queued → In Progress → Test → Finish** (Test is now the 3rd column). Updated all views, badge colors, JS status flows, mobile sheets, dropdowns, seed data, and PostgreSQL enum type. Documentation (AGENTS.md, plan.md, decision docs) updated accordingly.

## [0.8.0] - 2026-07-15

### Added

- **Skill description field:**
  - Added `description VARCHAR(150) NOT NULL DEFAULT ''` column to `skills_catalogue`.
  - Updated seed data in `init.sql` with descriptions for all 20 skills (Spanish UI text).
  - Updated `sp_add_skill` to accept `p_description VARCHAR(150)`.
  - Added `Description` property to `SkillDto` record.
  - `CreateSkillCommand` and `UpdateSkillCatalogueCommand` now accept a `Description` field.
  - Create/Edit handlers pass description to SQL/procedure.
  - `SkillController.Create` and `SkillController.Edit` POST actions accept `description` form field.
  - All skill-loading SQL queries now select the `description` column.
- **Views for Skill description:**
  - `Skill/Create.cshtml`: added description textarea (maxlength 150).
  - `Skill/Edit.cshtml`: added description textarea (maxlength 150), pre-filled with current value.
  - `Skill/Detail.cshtml`: added description row (first in the grid, `sm:col-span-2`).
  - `Skill/Index.cshtml` cards: added description line under status with `line-clamp-3 sm:line-clamp-4`.
  - `Skill/Index.cshtml` list view: added Description column (truncated, between Name and Status), adjusted column spans.

## [0.7.0] - 2026-07-11

### Added

- **Worker detail skills display:**
  - Added skills section below worker name in `Views/Worker/Detail.cshtml` matching Index card style (skill bar + name + value) with all skills visible (no "show more").
  - `WorkerController.Detail` GET action now injects `IGetAllSkillsQueryHandler` and passes `ViewBag.AllSkills` and `ViewBag.WorkerSkillsVector`.
- **Skill level decimal input:**
  - `skill-selector.js`: changed `step="0.5"` to `step="any"` for manual decimal entry (up to 2 decimals) while keeping arrow-key stepping at 0.5 via custom `keydown` handler.
  - Input width increased from `w-12` to `w-16` to accommodate wider values.
- **Worker edit evaluation confirmation:**
  - `WorkerController.Edit` GET: passes `ViewBag.EvaluatedSkillPositionsJson` with distinct skill position IDs that have existing evaluations.
  - `Worker.Edit.cshtml`: on form submit, JS checks if any modified skills have evaluations; shows `confirm()` dialog listing affected skill names; on confirmation, resubmits with `confirmedSkillEdit=true`.
  - `WorkerController.Edit` POST: when `confirmedSkillEdit=true` and modified skills have evaluations, fetches old worker vector, creates/gets "Manual Adjustment" project and "User Skill Edit" task, then inserts `performance_evaluations` records with `criticality='low'` for each modified evaluated skill. Rating and base points are derived from the delta (new - old).
- **Active task count now excludes Archived tasks:**
  - `GetAllWorkersQueryHandler`, `GetWorkersByProjectQueryHandler`, `GetWorkersNotInProjectQueryHandler`: changed `t.status <> 'Finish'` to `t.status NOT IN ('Finish', 'Archived')` in the `active_task_count` subquery, matching the Detail page task list filter.

### Changed

- **Worker detail view refactored:** Added `@using System.Linq` and dynamic skills rendering block before Assigned Tasks section.

## [0.6.0] - 2026-06-28

### Added

- **Task creation skill validation:**
  - Server-side validation in `CreateTaskCommandHandler` (`src/SFManagement.Infrastructure/Handlers/Commands/CreateTaskCommandHandler.cs`): throws `InvalidOperationException` if `RequiredSkillsVector` contains no non-zero entries.
  - Client-side validation in `Task/Create.cshtml`: prevents form submission and shows toast if no skill is selected.
- **Dashboard task skills display:**
  - New `TaskSkillDto` record (`SkillName`, `SkillPosition`, `RequiredLevel`) in `TaskDto.cs` (`src/SFManagement.Application/DTOs/TaskDto.cs`).
  - `TaskDto` extended with optional `IReadOnlyList<TaskSkillDto>? Skills` property.
  - `TaskCardDto` (`DashboardViewModel.cs`) extended with optional `Skills` list.
  - `GetDashboardTasksQueryHandler` (`src/SFManagement.Infrastructure/Handlers/Queries/GetDashboardTasksQueryHandler.cs`): loads skills catalogue and decodes `required_skills_vector` into named skill pills.
  - `Dashboard/Index.cshtml`: renders skill pills on each kanban task card (indigo badges with tooltip).
- **Evaluation scope reduced to task skills:**
  - `DashboardController.EvaluationPopup` (`src/SFManagement.Web/Controllers/DashboardController.cs`): now uses `task.Skills` (from decoded vector) instead of loading all skills; `IGetAllSkillsQueryHandler` dependency removed.
  - `EvaluateTaskCommandHandler` (`src/SFManagement.Infrastructure/Handlers/Commands/EvaluateTaskCommandHandler.cs`): only processes evaluations for skill positions present in the task's `required_skills_vector`; skips `performance_evaluations` insert if `newLevel == previousLevel` (within 0.001 tolerance); only updates worker vector if at least one skill actually changed.

### Changed

- `MapToCard` in `DashboardController` now passes `t.Skills` to `TaskCardDto`.

### Fixed

- **Worker/Index: "show more skills" visual consistency:**
  - Expanded skill rows now properly toggle the `hidden` class instead of setting inline `display: flex`, avoiding broken block layout for extra skills.
  - Skill row layout restructured to single line: name (truncated) | progress bar | score (bold, right-aligned) — all on the same row.
- **Worker/Edit: skills not loading for editing:**
  - `WorkerController.Edit` GET now computes active skills from `worker.SkillsVector`, serializes them to JSON, and passes via `ViewBag.WorkerSkillsJson`.
  - `Edit.cshtml` sets `window.__workerSkills` from the ViewBag and dispatches `change` events so the skill-selector JS properly stores the actual level values in its internal map.
- **Skill selector pills not rendering:**
  - `_SkillSelector.cshtml`: JSON serialization now uses explicit camelCase property names (`id`, `name`, `vectorPosition`, `isActive`) to match JS expectations, fixing the empty pills display in Task/Create and Worker/Edit forms.

### Added

- **Multiple workers per task:**
  - Database: removed `UNIQUE (task_id)` constraint from `task_assignments` to allow multiple workers per task.
  - New `AssignedWorkerDto` record (`WorkerId`, `WorkerName`) in `TaskDto.cs`.
  - `TaskDto` and `TaskCardDto` now use `IReadOnlyList<AssignedWorkerDto>? AssignedWorkers` (replacing single `AssignedWorkerId`/`AssignedWorkerName`).
  - New `RemoveWorkerFromTaskCommand` + `RemoveWorkerFromTaskCommandHandler` (`DELETE FROM task_assignments WHERE task_id = $1 AND worker_id = $2`).
  - New `GetWorkerTasksQuery` + `GetWorkerTasksQueryHandler`: returns tasks assigned to a worker with project names.
  - `AssignWorkerCommandHandler`: changed from `ON CONFLICT ... DO UPDATE` to simple `INSERT` (no conflict clause).
  - `GetDashboardTasksQueryHandler` and `GetAllTasksQueryHandler`: now aggregate multiple assigned workers per task via a second query with `= ANY($1)`.
  - `GetRecommendedWorkersQueryHandler`: excludes workers already assigned to the task (`NOT IN (SELECT worker_id FROM task_assignments WHERE task_id = $3)`).
  - `EvaluateTaskCommand` now includes `WorkerId`; handler filters assignment by both `taskId` and `workerId`.
- **Dashboard: worker pills with remove button:**
  - `Dashboard/Index.cshtml`: shows each assigned worker as an indigo pill with an `×` button (hidden for finished tasks).
  - `kanban.js`: new `removeWorker(taskId, workerId, btn)` function calls `/Dashboard/RemoveWorker` and removes the pill from the card.
  - `DashboardController`: new `RemoveWorker` POST action.
- **Evaluation modal: worker selector:**
  - `EvaluationPopup` passes `AssignedWorkers` to the view model.
  - `_EvaluationModal.cshtml`: shows a `<select>` dropdown when multiple workers are assigned; otherwise a hidden input with the single worker's ID.
- **Worker detail page: assigned tasks grid:**
  - `WorkerController.Detail` loads assigned tasks via `IGetWorkerTasksQueryHandler`.
  - `WorkerHistoryViewModel` extended with optional `AssignedTasks` list.
  - `Worker/Detail.cshtml`: renders assigned tasks as a responsive card grid with project name and task title, linking to the dashboard board.

## [0.5.0] - 2026-06-28

### Added

- **Fase 5 — UI/UX Polish + Frontend Security**
  - **5.6 Toast Notifications** (`wwwroot/js/toast.js`):
    - `ToastManager` singleton con soporte para `success`, `error`, `info`, `warning`
    - Auto-dismiss tras 3s con animación slide-in/out
    - Integrado en `_Layout.cshtml` via `#toast-container` y función global `window.showToast()`
    - Activado en todas las acciones asíncronas del kanban (assign, change status, evaluate)
  - **5.7 Breadcrumb Navigation** (`_Breadcrumb.cshtml`):
    - Partial compartido renderizado en todas las páginas
    - `ViewBag.Breadcrumbs` como `List<KeyValuePair<string, string>>` (label, url)
    - Último item sin url indica página actual
    - Estilos responsivos con Tailwind
  - **5.9 Tooltips/Hover Cards** (`wwwroot/js/tooltips.js`):
    - Tooltips CSS vía atributo `data-tooltip` en elementos
    - Posicionamiento automático (evita bordes de viewport)
    - Añadido a skill-pills, botones de asignar/evaluar, y theme toggle
  - **5.5 Loading Skeletons/Spinners** (`wwwroot/css/loaders.css`):
    - Animación shimmer para skeletons (`@keyframes shimmer`)
    - Spinner circular para botones en estado loading
    - Kanban.js: skeleton placeholder mientras carga AssignPopup/EvaluationPopup
  - **5.4 Dark Mode** (`wwwroot/js/theme.js`):
    - Persistencia en `localStorage('sfm-theme')`
    - Detecta `prefers-color-scheme` en primera visita
    - Toggle via botón en sidebar con icono luna/sol SVG
    - Clase `dark` en `<html>` + Tailwind `dark:` variants en todas las vistas
  - **5.3 Responsive Mobile-First**:
    - Kanban: `sm:grid-cols-1` → vertical stack en móvil
    - Tablas: `overflow-x-auto` con scroll horizontal
    - Formularios: `w-full` inputs, paddings responsivos `p-4 sm:p-6`
    - Sidebar: ancho fijo 56 con diseño compacto
  - **5.1 UI/UX Polish**:
    - Sombras consistentes: `shadow-sm` en cards, `shadow-md` en hover
    - Hover states: `hover:bg-gray-50` en filas de tabla, `hover:shadow-md` en cards
    - Focus rings: `focus:ring-2 focus:ring-indigo-500` en todos los inputs
    - Badges uniformes: `px-2 py-0.5 rounded-full text-xs font-medium`
    - Empty states con iconos y CTA
    - Transiciones suaves en modales (scale+opacity), sidebar links, skill pills
  - **Nuevos archivos creados**:
    - `wwwroot/js/toast.js`, `wwwroot/js/tooltips.js`, `wwwroot/js/theme.js`
    - `wwwroot/css/loaders.css`
    - `Views/Shared/_Breadcrumb.cshtml`
  - **Todos los controladores actualizados**: `ViewBag.PageTitle` y `ViewBag.Breadcrumbs` en cada acción GET
  - **Todas las vistas actualizadas**: dark mode classes, tooltips, breadcrumbs, consistencia visual
  - **Build**: 0 errores, 0 warnings
  - **Tests**: 53/53 verdes (31 unit + 14 integration + 8 E2E)
  - **5.11 Sidebar colapsable en mobile**:
    - Sidebar cambia a `fixed` overlay con slide-in/out en móvil (<1024px), se mantiene `static` en desktop
    - Botón hamburguesa en `<header>` con SVG icono, oculto en desktop (`lg:hidden`)
    - Backdrop semitransparente al abrir sidebar en mobile, cierra al hacer clic
    - Tecla Escape cierra la sidebar
    - `overflow: hidden` en body mientras sidebar abierta en mobile

### Fixed

- **Static files empty in browser**: Reemplazado `MapStaticAssets()` + `.WithStaticAssets()` por `UseStaticFiles()` para evitar los warnings `StaticFileMiddleware[16]` (WebRootPath no encontrado) y `StaticAssetsInvoker[17]` (Static Web Assets no habilitados en development). `UseStaticFiles()` sirve archivos directamente desde el wwwroot físico sin depender del manifest.
- **HTTPS redirect warning**: Movido `UseHttpsRedirection()` dentro del bloque `if (!app.Environment.IsDevelopment())` para eliminar el warning `HttpsRedirectionMiddleware[3]` (puerto HTTPS no determinado) al usar el perfil HTTP.

### Changed

- **Tailwind CSS**: Reestructurada la integración local:
  - `input.css` movido de `wwwroot/css/input.css` a `styles/input.css` (fuera de wwwroot) para evitar que el SDK lo trate como activo estático servible
  - `package.json`: scripts `build:css`/`watch:css` apuntan a `./styles/input.css` → `./wwwroot/css/tailwind.css`
  - `tailwind.config.js`: `content` ampliado con `./Pages/**/*.cshtml` y `./**/*.html`
  - `SFManagement.Web.csproj`:
    - Target `BuildTailwindCss` con `MakeDir` que crea `wwwroot` + `wwwroot\css` si no existen, antes de ejecutar `npm run build:css`
    - Nuevo target `CopyWwwrootToOutput` (after Build) que copia `wwwroot\**` completo a `$(OutputPath)wwwroot` con `SkipUnchangedFiles=true`, garantizando que los archivos están disponibles incluso si el content root apunta al directorio de salida
  - `Program.cs`: `WebApplicationOptions` con `ContentRootPath` y `WebRootPath` explícitos para evitar el warning `StaticFileMiddleware[16]` (WebRootPath no encontrado)
  - Eliminado `wwwroot/css/input.css` (source) del directorio servido
  - `wwwroot/css/tailwind.css` (generado) permanece en `.gitignore`

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
