# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.2] - 2026-07-23

### Added

- **Comprehensive `README.md`:** Full project documentation with architecture diagrams, tech stack, matching algorithm explanation, evaluation system, and getting started guide.
- **Seed example for "User Edit" (manual adjustment):** Added a `performance_evaluations` record for David Zarza (worker 7) with `task_id = NULL` and `criticality = 'low'` dated 2026-07-19, demonstrating the manual skill-edit history path in the worker detail view.

---

## [1.0.0] - 2026-07-20

### Added

- **Full deterministic workforce matching engine:** Vector-based skill matching using PostgreSQL pgvector with cosine similarity (Product Scalar operator `<#>`). No external AI/LLM dependency.
- **Kanban Dashboard:** 4-column board (Queued → In Progress → In Review → Finish) with drag-and-drop, async status changes, and mobile bottom sheet for status transitions.
- **Intelligent worker recommendation system:** Three badge types — Most Efficient (≥95% match), Grow Up (75–95%), Fastest to Finish (100%+ with least excess) — with colour-coded compatibility scores and skill-level comparison pills.
- **Evaluation & XP engine:** Per-skill performance evaluation with criticality multipliers (Low 0.5×, Medium 1.0×, High 1.5×, Critical 2.0×), mathematical clamping [0.0–10.0], 0.05 rounding, and real-time recalculation preview.
- **Task archive/restore system:** Archived state for completed tasks with Finish ↔ Archived transitions; Finish + AllWorkersEvaluated hides status controls.
- **Skill Catalogue management:** 22 predefined transversal competencies with immutable vector positions, create/edit/toggle active, and stored procedure `sp_add_skill` for safe insertion.
- **Project lifecycle management:** Create, edit, finalise projects; worker allocation; project-scoped task and team views.
- **Worker profile management:** CRUD with vector-based skill profiles, manual skill editing via Skill Pills Selector (0–10 scale), and evaluation history grouped by task.
- **Task CRUD:** Create, edit (Queued + InProgress only), detail view with assigned workers as Team-style cards, skill requirements with levels.
- **AES-GCM ID encryption:** All IDs in URLs and forms encrypted (12-byte nonce, 16-byte tag, Base64URL no padding) via `IdEncryptionService`.
- **Setup / Demo mode:** One-click database clear (TRUNCATE) and seed data import (22 skills, 3 projects, 13 workers, 17+ tasks) from `/Setup` page.
- **Dark mode:** Class-based theme toggle with `prefers-color-scheme` detection and localStorage persistence.
- **Responsive Mobile-First UI:** Collapsible sidebar (< 1024px), bottom sheets for modals on mobile, auto-switch from list to card view (< 1199px), task card quick-action arrows on desktop, status bottom sheet on mobile.
- **Accessibility (WCAG 2.1 AA):** Strategic `tabindex` on interactive `<div>` elements, `focus-visible` ring, focus trapping in modals, ARIA attributes, keyboard navigation support.
- **Smart tooltips:** `data-tooltip` attribute with truncation detection (`scrollWidth > clientWidth`), auto-positioning above/below, 640px max-width.
- **Toast notification system:** Success/error/info/warning toasts with 3s auto-dismiss and slide animations.
- **Modal system:** Focus trap, CSS scale+opacity animation, bottom sheet fallback on mobile (< 640px), drag-to-dismiss, backdrop click/Escape close.
- **Pagination:** Client-side pagination for long lists (Skills, Workers, Tasks).
- **Landing page:** Minimal layout with hero section, features grid, how-it-works steps, and evaluation mockup.
- **Breadcrumb navigation:** Home / Section partial on all internal pages.
- **Serilog structured logging:** Request logging middleware, rolling file sink (`Logs/log-.txt`), environment/thread enrichers.
- **Error page (`/Home/Error`):** `ResponseCache` disabled, proper exception handler fallback for Production.
- **Health check endpoint:** `/health` with PostgreSQL ping.
- **Docker support:** Multi-stage Dockerfile (SDK 10.0 + ASP.NET 10.0 runtime) and docker-compose.yml (pgvector/pgvector:pg16 + app).
- **CI/CD pipeline:** GitHub Actions on `release/sfmanagement.V*` branches — Unit + Integration tests → Render deploy hook.
- **Testing suite:** 31 unit tests (xUnit + FluentAssertions + NSubstitute), 14 integration tests (Testcontainers + WebApplicationFactory), 8 E2E tests (Playwright + Chromium headless).
- **Comprehensive README.md:** Full project documentation with architecture diagrams, tech stack, getting started guide, matching algorithm, and evaluation system.
- **`plan.md` development roadmap:** 124 tasks across 6 phases with entity editing rules and final project structure.

### Changed

- **Test → InReview enum rename:** `ProjectTaskStatus.Test` renamed to `InReview` across enum, PostgreSQL ALTER TYPE migration, SQL queries, JavaScript STATUS_FLOW/STATUS_LABELS, Razor views, and all test files.
- **Sidebar simplified:** Removed MAIN section label and Dashboard link; navigation via Project/Index card/row clicks to Dashboard.
- **Card titles standardised at 16px:** All admin list views (Project, Task, Skill, Worker Detail) use `text-[16px]` instead of `text-lg`.
- **Modal popup width responsive:** 80% default → 76% at ≥ 1500px → 70% at ≥ 1850px via CSS media queries.
- **All status/priority pills unified:** Consistent `bg-*-50 text-*-700 border border-*-300 font-semibold` style across all views.
- **Pull-to-refresh disabled:** `overscroll-behavior-y: contain` on body + `modal-open` class toggled by modal lifecycle.
- **Task cards reorder:** Status + Priority first, Project below (Task/Index and Worker/Detail Active Tasks).
- **Project/Index navigation:** Card/row clicks → Dashboard; "Detail" button → Project/Detail; row Dashboard icon removed.
- **Evaluation popup labels:** Date and Project labels use `text-gray-700 font-medium` matching Priority/Status style.
- **Worker skills gap tightened:** `space-y-1.5`, removed `py-2` from toggle in Worker/Index.
- **`RecyclableNpgsqlDataSource`:** Wraps NpgsqlDataSource singleton; `Recycle()` rebuilds after schema-modifying operations to fix stale PostgreSQL type cache (`DataTypeName '-.-'`).
- **ClearDatabase uses TRUNCATE:** `TRUNCATE TABLE ... RESTART IDENTITY CASCADE` keeps schema intact; app functional immediately after clear without importing.
- **idempotent init.sql:** All seed INSERTs use `ON CONFLICT DO NOTHING`; constraint creation guarded via `pg_constraint` existence check.

### Fixed

- **`DataTypeName '-.-'` error on import:** RecyclableNpgsqlDataSource.Recycle() invalidates stale NpgsqlDataSource type cache after TRUNCATE + reimport cycle.
- **Pull-to-refresh interfering with modals:** `overscroll-behavior-y: contain` prevents browser overscroll from triggering page reload on real mobile devices.
- **Pagination not re-rendering after search:** `pagination.js` `filteredItems()` ancestor `hidden` class check fixed with `max-sm:hidden` CSS-only visibility.
- **Select focus border not visible:** `-ml-px` overlap replaces `border-l-0` so blue focus border is visible on all sides.
- **`GetEnumOrNull<TEnum>` missing from `DataReaderMapper`:** Now handles nullable enum columns from PostgreSQL.
- **Sidebar localStorage persistence removed:** Dashboard link no longer saved as active page in localStorage.

## [0.13.0] - 2026-07-20

### Added

- **8 new archived tasks with full evaluation seed data (tasks 18-25):**
  - Added 8 fully evaluated archived tasks spanning 2 months (2026-05-20 to 2026-07-18) with 1-4 workers each and 3-6 skills per task.
  - Task 20: Critical Auth Integration (critical, 3 workers: Alex, Sarah, David Z — 4 skills each)
  - Task 21: Dashboard Visualisation (low, 2 workers: Oriol, John — 4 skills each)
  - Task 22: Security Audit (critical, 2 workers: Sarah, Maria — 3-4 skills each)
  - Task 18: Landing Page Redesign (high, 2 workers: Alex, John — 4 skills each)
  - Task 19: Backend API Endpoints (medium, 3 workers: Alex, Maria, Carlos — 4 skills each)
  - Task 24: Data Migration (medium, 2 workers: David M, Carlos — 3-4 skills each)
  - Task 23: UI Component Library (medium, 2 workers: Oriol, John — 4 skills each)
  - Task 25: Performance Optimisation (critical, 2 workers: Oriol, David Z — 5 skills each)
  - All evaluations chain correctly through worker skill vectors with proper clamping (0.0-10.0) and rounding (0.05).
  - Total new evaluation records: 71. Grand total across all seeds: 82 evaluations.
- **6 earlier archived tasks for skill evolution continuity (tasks 26-31):**
  - Tasks dated 3-4 months ago (2026-03-20 to 2026-05-28) to fill the skill evolution timeline.
  - Task 26: jQuery Cleanup (low, 1 worker: John — 2 skills)
  - Task 27: Connection Audit (high, 2 workers: Sarah, Carlos — 3 skills each)
  - Task 28: Responsive Audit (medium, 2 workers: Oriol, John — 3 skills each)
  - Task 29: Backend Security Audit (high, 2 workers: Sarah, Carlos — 3 skills each)
  - Task 30: CSS Accessibility Audit (low, 2 workers: Oriol, John — 3 skills each)
  - Task 31: DB Performance Optimisation (critical, 2 workers: Carlos, Maria — 3-4 skills each)
  - Added `created_at` timestamps to existing evaluations for tasks 3, 4, 14, and 16.
  - 45 additional evaluation records. Grand total across all seeds: 127 evaluations.
  - Added `Serilog.AspNetCore`, `Serilog.Sinks.Console`, `Serilog.Sinks.File`, `Serilog.Enrichers.Environment`, `Serilog.Enrichers.Thread` NuGet packages.
  - Static `Log.Logger` initialized from `appsettings.json` Serilog section via `ReadFrom.Configuration`.
  - `builder.Host.UseSerilog()` for full host integration.
  - `app.UseSerilogRequestLogging()` middleware logs every HTTP request with method, path, status code and elapsed time.
  - Rolling file sink: `Logs/log-.txt` with daily rolling and 15-day retention.
  - Enrichers: `FromLogContext`, `WithMachineName`, `WithThreadId`.
  - Production minimum level: `Information`; Development: `Debug`.
- **Exception logging in controllers:**
  - `TaskController.POST Edit`: `Log.Warning` on `InvalidOperationException`.
  - `ProjectController.POST FinalizeProject`: `Log.Warning` on `InvalidOperationException`.
  - `DashboardController.POST FinalizeProject`: `Log.Warning` on `InvalidOperationException`.
  - `SetupController.POST ClearDatabase`: `Log.Warning` on `Exception`.
  - `SetupController.POST ImportSeedData`: `Log.Warning` on `Exception`.
- **Error page (`/Home/Error`):**
  - New `HomeController.Error()` action with `[ResponseCache(Duration = 0, NoStore = true)]`.
  - New `Views/Home/Error.cshtml` view with error message and link to home.
  - Fixes broken `app.UseExceptionHandler("/Home/Error")` in Production (previously pointed to non-existent action).
- **Comprehensive README.md:**
  - Complete rewrite with detailed project description, architecture overview, and feature documentation.
  - Project origin and development phases (Gemma 4 → Gemini → OpenCode → Big Pickle).
  - Full tech stack table (Backend, Frontend, Database, Testing, DevOps).
  - Clean Architecture diagram and CQRS Manual pattern explanation.
  - Complete project structure tree with all layers and key files.
  - Getting Started guide with 3 options: Docker Compose, Local Development, and Tests.
  - Database schema overview, custom types, and stored procedures.
  - Demo & Seed Data section (22 skills, 3 projects, 13 workers, 17 tasks).
  - CI/CD Pipeline documentation (GitHub Actions → Render).
  - Kanban lifecycle and task editing rules table.
  - Matching Algorithm explanation with score formula and badge criteria.
  - Evaluation & XP System with criticality multipliers and clamping.
  - UI/UX Features (dark mode, responsive, accessibility, tooltips).
  - Testing Strategy pyramid (Unit, Integration, E2E) with test counts.
  - Development Phases summary (5 phases from concept to deployment).

### Changed

- **`.gitignore`:** added `Logs/` directory exclusion for Serilog file output.

## [0.12.0] - 2026-07-19

### Added

- **Evaluation History grouped by task:**
  - New `EvaluationHistoryGroupDto` record with `TaskId`, `AvgScore`, `TotalImpact`, `ApprovedSkills`, `TotalSkills`, `Criticality`, `Status` fields.
  - New `GetWorkerHistoryGroupQuery` + handler: GROUP BY query aggregating evaluations per task with criticality from latest evaluation and task status.
  - `WorkerHistoryViewModel` extended with `GroupedEvaluations` list.
  - `WorkerController.Detail` fetches grouped evaluations with encrypted task IDs.
- **Evaluation detail popup / bottom sheet:**
  - New `EvaluationDetailViewModel` record with task title, project, criticality, status, date, summary stats, and per-skill evaluation data.
  - New `WorkerController.EvaluationDetailPopup` action fetching task status and computing summary stats.
  - New `_EvaluationDetailPopup.cshtml` partial: header with task title, priority/status badges, date/project, stat boxes (Total Impact, Improved, Decreased, Unchanged), and skills table with bar visualization.
  - Card click opens popup (desktop) or bottom sheet (mobile) via `openEvaluationDetailPopup()` in `kanban.js`.
- **Evaluation history search and project filter:**
  - Search input filters evaluations by task title.
  - Project select dropdown filters evaluations by project name.
  - Both filters work simultaneously on both mobile and desktop.
- **View toggle (card/table) for desktop evaluation history:**
  - Toggle buttons for card view and table view, only visible at `xl:` (1280px+).
  - Below 1280px forced to card view via `matchMedia` listener.
  - View mode persisted in `localStorage`.
- **Worker skills responsive grid:**
  - Worker detail skills section uses 2-column grid by default, 3 columns at 1600px+ (`min-[1600px]:grid-cols-3`).

### Changed

- **Evaluation history card layout:**
  - Cards show Priority, Status badges followed by Date on the same line.
  - Project line below with colored dot indicator.
  - Stats grid: Avg Score, Impact, Skills.
  - All non-title card text now 14px (`text-sm`).
  - Card title remains 16px (`text-base`).
- **Evaluation history cards responsive columns:**
  - 1 column below 750px, 2 columns 750px–1279px, 3 columns 1280px+.
- **Evaluation history mobile layout:**
  - Mobile-only search bar + project select: both share equal width 50/50 (`flex-1`).
  - Desktop search bar + project select: same joined visual treatment.
  - Select elements now have full border (using `-ml-px` instead of `border-l-0`) so blue focus border is visible on all sides.
- **Evaluation detail popup responsive:**
  - On small screens (<750px), Date and Project stack vertically; on sm+ they sit side by side.
  - Popup max-width 1000px on desktop, full width on mobile via bottom sheet.
  - Skills table scrolls horizontally on small screens.
- **Section visibility handling:**
  - Replaced `hidden sm:block` wrapper with `max-sm:hidden` on individual elements to fix `pagination.js` `filteredItems()` ancestor `hidden` class check.

### Fixed

- **Pagination not re-rendering after search:** `pagination.js` `filteredItems()` walks ancestors checking `classList.contains('hidden')` — a literal `hidden` class in DOM filtered out all items even when CSS `sm:block` made them visible. Fixed by using `max-sm:hidden` (CSS-only visibility).
- **Select focus border not visible:** Selects previously used `border-l-0` to join with search input, causing the left border to be invisible on focus. Fixed with `-ml-px` overlap and `focus-visible:border-blue-500`.
- **`GetEnumOrNull<TEnum>` added to `DataReaderMapper`:** handles nullable enum columns (task status) from PostgreSQL.

## [0.11.0] - 2026-07-18

### Added

- **Detail cards open a popup / bottom sheet on click:**
  - New shared partials `Views/Shared/_TaskDetailPopup.cshtml` and `Views/Shared/_WorkerDetailPopup.cshtml` render a compact detail panel.
  - New controller actions returning the partials: `ProjectController.TaskDetailPopup` / `WorkerDetailPopup`, `TaskController.WorkerDetailPopup`, `WorkerController.TaskDetailPopup`.
  - `openTaskDetailPopup` and `openWorkerDetailPopup` JS helpers in `kanban.js` fetch the partial and call `openModal` (popup on desktop, bottom sheet on mobile).
  - Project Detail task cards and team cards, Task Detail assigned-worker cards, and Worker Detail active-task cards now open the detail popup instead of navigating away.
  - Popups include a footer with a "Close" button and an "Open Detail" button that navigates to the full detail page.
- **Edit button on Project Detail:** added the `Edit` action button in the `ActionButtons` section of `Project/Detail.cshtml` (hidden when the project is closed).

### Changed

- **Popup / bottom sheet presentation:**
  - Detail popups now open as a normal centered popup on desktop (no longer forced into a bottom-sheet style) and as a bottom sheet on mobile devices.
  - Bottom sheet now uses the full available width with no left margin and no horizontal scroll (`applyMobileSheet` forces `width:100%`, `margin:0` and `overflow-x:hidden`).
  - The close button is no longer auto-focused when a popup opens (excluded from the initial focus target in `modal.js`).
  - Long titles inside the bottom sheet are truncated with an ellipsis (added `min-w-0 flex-1` to the header container in both detail popups).
- **Section heading styling:** in Task Detail (page and popup) and Worker Detail popup, section titles (`Required Skills`, `Assigned Workers`, `Skills`, `Project`) are now black, 16px (`text-base font-semibold`), without uppercase or turquoise color.
- **Field label styling:** in Task Detail, the `Status`, `Priority` and `Project` labels now use darker gray (`text-gray-700`) with a trailing colon, matching the rest of the form.
- **Role / Project text color:** any displayed `Role` or `Project`/`ProjectName` text now uses `text-gray-500` instead of `text-gray-400` across detail pages and popups.
- **Assigned Worker cards in Task Detail popup:** rendered as standard cards (name on top, role below) instead of a compact row, and clickable to open the worker detail popup.

### Fixed

- **Worker not found from Task Detail:** the `TaskController.WorkerDetailPopup` action now decrypts the worker id first and matches by raw `WorkerId` (the handler's `AssignedWorkers` did not populate `WorkerIdEncrypted`, causing a false "Worker not found").
- **Missing worker ids in task popups:** `ProjectController.TaskDetailPopup` and `WorkerController.TaskDetailPopup` now populate `WorkerIdEncrypted` on each assigned worker before rendering the partial, so the assigned-worker cards open correctly.

## [0.10.0] - 2026-07-18

### Added

- **Setup / "Modo demo" page:**
  - New `Setup` nav link under Administration in `_Layout.cshtml` linking to `/Setup/Index`.
  - `SetupController` with `Index` (GET) and two independent POST actions: `ClearDatabase` and `ImportSeedData` (`[IgnoreAntiforgeryToken]`, returning `Json({ success, message })`).
  - `ClearDatabaseCommand`/`Handler` (TRUNCATE all tables `RESTART IDENTITY CASCADE`) and `ImportSeedDataCommand`/`Handler` (runs embedded `init.sql`).
  - `SeedScriptProvider` reads `init.sql` as an embedded resource; registered in `DependencyInjection.cs`.
  - `Setup/Index.cshtml` with two action cards (destructive red "Borrar Base de Datos", constructive green "Importar Datos Iniciales"), confirmation modals, spinner loading state, and success/error toast feedback.
  - Build target `CopySeedScript` syncs `database/init.sql` into the embedded resource on each build (single source of truth).

### Changed

- **`init.sql` idempotency:** made seed script safe to run repeatedly — unique constraint creation guarded via `pg_constraint` existence check, and all seed `INSERT`s now use `ON CONFLICT DO NOTHING`. Fixes `42P07: relation "task_assignments_task_worker_key" already exists` on repeated imports.
- **Empty Directory/Skills lists:** the search box (and view-toggle/filter bar) is now hidden in Projects, Tasks, Workers and Skills catalogue views when there are no items (`@if (Model.Count > 0)`); only the "No ... yet" empty state shows.
- **Seed data updates:**
  - Task `Optimize Checkout Core Web Vitals` (E-Commerce Platform Refactor) status changed from `Test` to `Finish`.
  - Task `Implement Secure Password Hashing Command` (SFManagement Core Platform) reassigned from Oriol Martinez to **Alex Rodriguez Fernandez** and **Maria Castillo Ruiz**.
  - Task `Refactor Notification System to Use Server-Sent Events Instead of Polling` assigned to **Carlos Moreno Ruiz** and **David Zarza Cabrera**.

### Fixed

- **Mobile status controls:** on small/very small screens the previous/next status arrows (`<` / `>`) are now hidden (`hidden sm:inline-flex`) in `Dashboard/_TaskCard.cshtml`; only the "Status" button (which opens the bottom-sheet selector) remains.
- **Status bottom sheet height:** the mobile "Change Status" sheet now auto-sizes to its content (small) instead of filling `85vh`, while other modals keep their full height.

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
