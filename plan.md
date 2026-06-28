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
| 2.1 | Enum `ProjectTaskStatus` (Queued, InProgress, Blocked, Finish) | `Domain/Enums/ProjectTaskStatus.cs` |
| 2.2 | Enum `Criticality` (low, medium, high, critical) | `Domain/Enums/Criticality.cs` |
| 2.3 | Enum `PerformanceRating` (Poor, Average, Good, Excellent) con método `ToBasePoints()` → -0.5, 0.0, +0.2, +0.5 | `Domain/Enums/PerformanceRating.cs` |
| 2.4 | Value Object `SkillVector` con: constructor desde `float[]`, clamping `[0.0, 10.0]`, método `ApplyImpact(basePoints, criticalityMultiplier)`, indexador `[position]` | `Domain/ValueObjects/SkillVector.cs` |
| 2.5 | Entidad `SkillCatalogue` (Id, Name, VectorPosition, IsActive) | `Domain/Entities/SkillCatalogue.cs` |
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
| 2.20 | Command `UpdateWorkerCommand` (WorkerId, Name, SkillsVector) — SkillsVector opcional, si se envía se actualiza | `Application/Commands/UpdateWorkerCommand.cs` |
| 2.21 | Command `UpdateSkillCatalogueCommand` (SkillId, Name) — vector_position inmutable | `Application/Commands/UpdateSkillCatalogueCommand.cs` |
| 2.22 | Query `GetDashboardTasksQuery` (ProjectId) → List<TaskDto> | `Application/Queries/GetDashboardTasksQuery.cs` |
| 2.23 | Query `GetRecommendedWorkersQuery` (ProjectId, TaskId) → List<WorkerScoreDto> | `Application/Queries/GetRecommendedWorkersQuery.cs` |
| 2.24 | Query `GetWorkerHistoryQuery` (WorkerId) → List<EvaluationHistoryDto> | `Application/Queries/GetWorkerHistoryQuery.cs` |
| 2.25 | Query `GetWorkersByProjectQuery` (ProjectId) → List<WorkerDto> | `Application/Queries/GetWorkersByProjectQuery.cs` |
| 2.26 | Query `GetAllSkillsQuery` → List<SkillDto> | `Application/Queries/GetAllSkillsQuery.cs` |
| 2.27 | DTOs: `TaskDto`, `WorkerScoreDto`, `EvaluationHistoryDto`, `WorkerDto`, `SkillDto` | `Application/DTOs/` |
| 2.28 | Test: `SkillVectorTests` — creación, clamping superior 10.0, clamping inferior 0.0, ApplyImpact (todas las combinaciones criticality × rating) | `tests/SFManagement.UnitTests/SkillVectorTests.cs` |
| 2.29 | Test: `XpCalculationTests` — verificar multiplicadores criticalidad × puntos base | `tests/SFManagement.UnitTests/XpCalculationTests.cs` |
| 2.30 | Test: `DomainValidationTests` — validar estados Kanban, enums, reglas de edición | `tests/SFManagement.UnitTests/DomainValidationTests.cs` |
| 2.31 | ADR 003: documentar diseño de SkillVector y algoritmo XP | `docs/003-decision-skill-vector-xp.md` |
| 2.32 | Actualizar CHANGELOG.md con v0.1.0 | `CHANGELOG.md` |

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

## 🔷 FASE 4 — Frontend: Vistas Razor, Kanban, Vanilla JS, Skill Selector Pills, Skills CRUD y E2E Tests

| # | Tarea | Archivo / Ruta | Estado |
|---|-------|----------------|--------|
| 4.1 | Layout principal con Tailwind CSS (CDN v3), meta viewport, fuente sistema | `Web/Views/Shared/_Layout.cshtml` | ✅ |
| 4.2 | `DashboardController` (Index, AssignPopup, EvaluationPopup, POST actions) | `Web/Controllers/DashboardController.cs` | ✅ |
| 4.3 | Vista Dashboard Kanban: 4 columnas responsivas (Queued, In Progress, Blocked, Finish) | `Web/Views/Dashboard/Index.cshtml` | ✅ |
| 4.4 | `ProjectController` — añadir acción `Detail` GET + vista con info del proyecto y tareas vinculadas | `Web/Controllers/ProjectController.cs`, `Web/Views/Project/Detail.cshtml` | ✅ |
| 4.5 | Vista Create Project — reemplazar textarea por `<input type="file" accept=".md">` + procesar contenido en POST | `Web/Views/Project/Create.cshtml`, `ProjectController.Create POST` | ✅ |
| 4.6 | `TaskController` (Create GET/POST, ChangeStatus) | `Web/Controllers/TaskController.cs` | ✅ |
| 4.7 | Vista Create Task — reemplazar inputs numéricos por **Skill Pills Selector** (búsqueda + pills + nivel numérico en zona seleccionadas) | `Web/Views/Task/Create.cshtml`, `Web/Views/Shared/_SkillSelector.cshtml` | ✅ |
| 4.8 | `WorkerController` — añadir acción `Edit` GET/POST + vista con nombre + Skill Pills Selector pre-cargado | `Web/Controllers/WorkerController.cs`, `Web/Views/Worker/Edit.cshtml` | ✅ |
| 4.9 | Vista Worker Detail: tabla cronológica inversa de evaluaciones + enlace "Editar" | `Web/Views/Worker/Detail.cshtml` | ✅ |
| 4.10 | Partial View: Popup asignación worker (glassmorphism backdrop-blur-sm) | `Web/Views/Dashboard/_AssignWorkerModal.cshtml` | ✅ |
| 4.11 | Partial View: Modal evaluación desempeño (rating por skill, carga skills desde BD) | `Web/Views/Dashboard/_EvaluationModal.cshtml` | ✅ |
| 4.12 | ViewModels: `DashboardViewModel`, `AssignWorkerViewModel`, `EvaluationViewModel`, `WorkerHistoryViewModel` | `Web/ViewModels/` | ✅ |
| 4.13 | **kanban.js** — cambiar estado tarea vía fetch + mover card entre columnas **sin recargar** (DOM in-place) | `Web/wwwroot/js/kanban.js` | ✅ |
| 4.14 | **modal.js** — focus trap, animación CSS entrada/salida (opacity/transform), click outside, Escape key | `Web/wwwroot/js/modal.js` | ✅ |
| 4.15 | **skill-selector.js** — componente Vanilla JS: input búsqueda, pills no-seleccionadas filtrables, pills seleccionadas con input nivel (0-10), siempre visibles | `Web/wwwroot/js/skill-selector.js` | ✅ |
| 4.16 | `SkillController` — CRUD skills catálogo (Index, Create, Edit, ToggleActive) | `Web/Controllers/SkillController.cs` | ✅ |
| 4.17 | Vistas Skill: `Index.cshtml` (tabla skills activas/inactivas + toggle), `Create.cshtml` (solo nombre), `Edit.cshtml` (solo nombre) | `Web/Views/Skill/` | ✅ |
| 4.18 | **Query** `GetAllSkillsQueryHandler` — SELECT skills activas desde BD | `Infrastructure/Handlers/Queries/GetAllSkillsQueryHandler.cs` | ✅ |
| 4.19 | **Command** `ToggleSkillActiveCommand` con Handler — soft-delete / reactivar skill | `Application/Commands/ToggleSkillActiveCommand.cs`, `Infrastructure/Handlers/Commands/ToggleSkillActiveCommandHandler.cs` | ✅ |
| 4.20 | **Database**: `vector(12)` → `vector(1024)`, columna `is_active` en skills_catalogue, stored procedure `sp_add_skill`, función `util_pad_vector()` para seed | `database/init.sql`, `database/stored_procedures.sql` | ✅ |
| 4.21 | **E2E Test** — Skills CRUD (crear → listar → desactivar → reactivar) | `tests/SFManagement.E2ETests/SkillsCrudTests.cs` | ✅ |
| 4.22 | **E2E Test** — Worker Edit (editar nombre + skills) | `tests/SFManagement.E2ETests/WorkerEditFlowTests.cs` | ✅ |
| 4.23 | Actualizar CHANGELOG.md con v0.4.0 | `CHANGELOG.md` | ✅ |

### Detalle Skill Pills Selector (tareas 4.7, 4.8, 4.15)

**Comportamiento:**
1. Carga skills desde ViewBag (lista `SkillDto` con id, name, vector_position)
2. `<input type="text" placeholder="🔍 Filtrar skills...">` filtra por nombre solo las pills **no seleccionadas**
3. Pills no seleccionadas se muestran como `<span class="pill">` clickeables
4. Click en pill → se mueve a zona "Seleccionadas" + aparece `<input type="number" min="0" max="10" step="0.5">` con label "Nivel"
5. Click en ✕ → vuelve a zona no seleccionada, se limpia nivel y posición
6. Las seleccionadas **siempre visibles** (el filtro no las afecta)
7. POST envía: `skillPositions[]` (vector_position) + `skillLevels[]` (nivel numérico)
8. El handler construye un `float[1024]` con ceros y setea las posiciones indicadas

**Partial View:** `_SkillSelector.cshtml`
**JS:** `skill-selector.js`

---

## 🔷 FASE 5 — UI/UX Polish y Frontend Security

| # | Tarea | Archivo / Ruta |
|---|-------|----------------|
| 5.1 | **UI/UX Polish**: aplicar estilos consistentes a todas las vistas (Tailwind) — card shadows, button hover states, form focus rings, responsive gaps, empty-state illustrations, status badge uniformity, animations sutiles, tipografía consistente | `Web/Views/*/*.cshtml`, `Web/wwwroot/css/` |
| 5.2 | **ID Encryption**: encriptar/empaquetar todos los IDs expuestos en URLs y formularios del frontend (`workerId`, `projectId`, `taskId`, `skillId`) usando un cifrado simétrico server-side (e.g. AES con clave en configuración); los controladores descifran automáticamente los IDs entrantes vía model binder o action filter | `Web/Infrastructure/EncryptedIdHandler.cs`, `Web/Controllers/*.cs` |
| 5.3 | **Responsive Mobile-First**: probar y ajustar todas las vistas en viewports <768px — menú colapsable, tablas con scroll horizontal, cards apiladas, formularios de ancho completo, kanban vertical en móvil | `Web/Views/*/*.cshtml`, `Web/wwwroot/css/responsive.css` |
| 5.4 | **Dark Mode / Theme Toggle**: implementar modo oscuro con Tailwind (`dark:` variant), persistencia en `localStorage`, toggle en la sidebar, transición suave entre temas | `Web/Views/Shared/_Layout.cshtml`, `Web/wwwroot/js/theme.js` |
| 5.5 | **Loading Skeletons / Spinners**: mostrar indicadores de carga durante operaciones fetch (Assign, ChangeStatus, SubmitEvaluation) — spinners en botones, skeleton cards en kanban, estado "Cargando..." en modales | `Web/wwwroot/js/kanban.js`, `Web/wwwroot/js/modal.js`, `Web/wwwroot/css/loaders.css` |
| 5.6 | **Toast Notifications**: sistema de notificaciones no-bloqueantes para feedback de acciones (guardado exitoso, error, skill añadida) — auto-dismiss después de 3s, stack de toasts, animación slide-in/out | `Web/wwwroot/js/toast.js`, `Web/Views/Shared/_Layout.cshtml` |
| 5.7 | **Breadcrumb Navigation**: migas de pan en todas las páginas mostrando la jerarquía (ej: Workers > Edit Worker #1), generadas desde el controlador o routing | `Web/Views/Shared/_Breadcrumb.cshtml`, `Web/Controllers/*.cs` |
| 5.8 | **Paginación en Listas Largas**: añadir paginación server-side con `OFFSET`/`LIMIT` en queries de Workers, Tasks y Skills — número de página, total de páginas, navegación | `Web/Controllers/*.cs`, `Infrastructure/Handlers/Queries/*.cs`, `Web/Views/*/Index.cshtml` |
| 5.9 | **Tooltips / Hover Cards**: tooltips en elementos del Dashboard (nombre de skill al hover sobre badge, score de recomendación en cards de asignación, vista previa de descripción en tareas) | `Web/wwwroot/js/tooltips.js`, `Web/Views/Dashboard/*.cshtml` |
| 5.10 | **Developer Profiles Grid**: reemplazar tabla de Workers por grid de tarjetas con avatar, rol, búsqueda en tiempo real, skills colapsables con barra de progreso, contador de evaluaciones | `Web/Views/Worker/Index.cshtml`, `Web/wwwroot/js/worker-grid.js`, `Application/DTOs/WorkerProfileDto.cs`, `Infrastructure/Handlers/Queries/GetAllWorkersQueryHandler.cs` |

---

## 🔷 FASE 6 — Validación Extrema de XP, Auditoría OWASP y Cierre

| # | Tarea | Archivo / Ruta |
|---|-------|----------------|
| 6.1 | Test extremo: worker con skill en 6.0, recibe evaluación "mal" con criticidad "high" (×1.5), debe bajar a 5.25 (6.0 + (-0.5 × 1.5) = 5.25) | `tests/Unit/SkillVectorEdgeCaseTests.cs` |
| 6.2 | Test extremo: worker con skill en 0.0, recibe evaluación "mal" con criticidad "critical" (×2.0), debe quedar en 0.0 (clamping inferior) | `tests/Unit/SkillVectorEdgeCaseTests.cs` |
| 6.3 | Test extremo: worker con skill en 9.5, recibe "muy_bien" con criticidad "critical" (×2.0), debe quedar en 10.0 (clamping superior: 9.5 + 0.5×2.0 = 10.5 → clamp a 10.0) | `tests/Unit/SkillVectorEdgeCaseTests.cs` |
| 6.4 | ADR 005: documentar algoritmo de cálculo de XP con fórmula, multiplicadores y clamping | `docs/005-decision-xp-calculation-algorithm.md` |
| 6.5 | Auditoría OWASP: revisar inyección SQL (parametrización), XSS en Razor (Html.Encode), saneamiento subida .md, cabeceras HTTP security | `docs/006-owasp-security-audit.md` |
| 6.6 | Actualizar `CHANGELOG.md` con v1.0.0 (Added: todas las fases, Fixed: clamping edge cases, Security: OWASP mitigaciones, UI Polish, ID Encryption) | `CHANGELOG.md` |
| 6.7 | Actualizar `README.md` con descripción técnica, instrucciones de despliegue (docker-compose up), y enlaces a ADRs | `README.md` |

---

## 📋 Reglas de Edición por Entidad

### SkillsCatalogue
- Editable solo `name`. `vector_position` es inmutable (cambiarlo rompe vectores existentes).

### Project
- Editables `name` y `description_md`.
- `project_workers` (alcance) se pueden añadir/remover libremente.

### Worker
- Editables `name` y `skills_vector` (desde la página `Worker/Edit` usando el Skill Pills Selector).
- `skills_vector` también se recalcula automáticamente vía evaluaciones de tareas.

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
│   ├── init.sql                       → Schema + seeding
│   └── stored_procedures.sql          → sp_add_skill
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
│       ├── Controllers\               → DashboardController, ProjectController, SkillController, WorkerController, TaskController
│       ├── Views\Shared\              → _Layout.cshtml, _SkillSelector.cshtml
│       ├── Views\Dashboard\           → Index.cshtml, _AssignWorkerModal.cshtml, _EvaluationModal.cshtml
│       ├── Views\Project\             → Create.cshtml, Detail.cshtml
│       ├── Views\Task\                → Create.cshtml
│       ├── Views\Worker\              → Detail.cshtml, Edit.cshtml
│       ├── Views\Skill\               → Index.cshtml, Create.cshtml, Edit.cshtml
│       ├── ViewModels\                → DashboardViewModel, AssignWorkerViewModel, EvaluationViewModel, WorkerHistoryViewModel
│       └── wwwroot\js\                → kanban.js, modal.js, evaluation.js, skill-selector.js
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

**Total: ~110 tareas** distribuidas en 6 fases.
