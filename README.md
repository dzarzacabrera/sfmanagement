# 🔨 Skill Forge Management

### Enterprise Workforce Intelligence & Skill Matching Platform

**Optimize your teams by pairing the right talent with the right tasks using a deterministic, vector-based skill matching engine.**

---

![Version](https://img.shields.io/badge/Version-1.0.4-blue?style=flat-square)
![License](https://img.shields.io/badge/License-Proprietary-green?style=flat-square)
![.NET](https://img.shields.io/badge/.NET-10-purple?style=flat-square)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791?style=flat-square)
![pgvector](https://img.shields.io/badge/pgvector-Extension-orange?style=flat-square)
![Tailwind](https://img.shields.io/badge/Tailwind_CSS-3.4-06B6D4?style=flat-square)

[**Live Demo →**](https://sfmanagement.onrender.com) · [Presentation](https://drive.google.com/file/d/1iViZv66iGDdNr0ufBvdyD41-_6p7cbdB/view?usp=sharing) · [Video](https://drive.google.com/file/d/1Gx9im59YOv39nMMep5ll-7P5430oFzRf/view?usp=sharing)

---

## 📖 Table of Contents

- [About](#-about)
- [Key Features](#-key-features)
- [How It Works](#-how-it-works)
- [Tech Stack](#-tech-stack)
- [Architecture](#-architecture)
- [Project Structure](#-project-structure)
- [Getting Started](#-getting-started)
  - [Prerequisites](#prerequisites)
  - [Option 1 — Docker Compose (Recommended)](#option-1--docker-compose-recommended)
  - [Option 2 — Local Development](#option-2--local-development)
  - [Option 3 — Run Tests](#option-3--run-tests)
- [Database](#-database)
- [Demo & Seed Data](#-demo--seed-data)
- [CI/CD Pipeline](#-cicd-pipeline)
- [Project Lifecycle](#-project-lifecycle)
- [The Matching Algorithm](#-the-matching-algorithm)
- [Evaluation & XP System](#-evaluation--xp-system)
- [UI/UX Features](#-uiux-features)
- [Testing Strategy](#-testing-strategy)
- [Development Phases](#-development-phases)
- [Future Improvements](#-future-improvements)
- [Contributing](#-contributing)
- [License](#-license)
- [Contact](#-contact)

---

## 🔍 About

**Skill Forge Management (SFManagement)** is a modular monolithic web application designed to optimize team allocation in software development projects. The platform uses a **deterministic, vector-based skill matching engine** powered by PostgreSQL's pgvector extension to pair developers with tasks based on their competency profiles, eliminating the need for external AI services and ensuring cost-free, reproducible results.

### Origin & Process

The project was born through a rigorous multi-phase methodology combining local and cloud AI models:

1. **Phase 1 — Conceptualization:** The MVP concept was defined and validated locally using Gemma 4 via voice notes processed through LocalWhispers and OpenClaw. The entire structural basis was then migrated to Google Gemini for deeper technical refinement.
2. **Phase 2 — Architecture Design:** All technical objectives were consolidated into a structured roadmap within Gemini, including the deterministic vector engine, the architectural manifesto (`agents.md`), and the CQRS manual pattern.
3. **Phase 3 — Action Plan:** The full architectural synthesis was extracted from Gemini and imported into OpenCode, where the definitive `plan.md` was created.
4. **Phase 4 — Development:** Iterative coding guided by OpenCode + Big Pickle model, adhering strictly to `agents.md` rules, with rapid UI prototyping via Vercel v0.
5. **Phase 5 — Deployment:** Automated CI/CD with GitHub Actions → Render (free-tier, cold start ~40s–1min), triggered by `release/sfmanagement.V*` branch pushes.

---

## ✨ Key Features

### 📋 Project & Task Management
- **Kanban Dashboard** with drag-and-drop status columns (Queued → In Progress → In Review → Finish → Archived)
- Create, edit, and archive tasks with skill requirements and criticality levels
- Project lifecycle management with worker allocation and finalization

### 🎯 Intelligent Skill Matching
- **Deterministic vector-based engine** using PostgreSQL pgvector (Product Scalar operator `<#>`)
- Skill catalog with indexed by immutable vector positions
- Three recommendation badges: **Most Efficient** (≥95% all required skill), **Grow Up** (75–95%), **Fastest to Finish** (excess capacity, ≥100% all required skills but most closer to 100% for don't over qualified)

### ⚡ Evaluation & XP System
- Per-skill performance evaluations with criticality multipliers (0.5×–2.0×)
- Mathematical clamping to keep skill vectors in [0.0, 10.0]
- Evaluation history grouped by task with average scores, total impact, and skill progress bars

### 🌙 Responsive & Accessible UI
- **Dark mode** with system preference detection and localStorage persistence
- **Mobile-first** responsive design with collapsible sidebar, bottom sheets, and touch-optimized controls
- **WCAG 2.1 AA** compliance, focus management, and ARIA attributes

### 🛡️ Security
- **AES-GCM encrypted IDs** in all URLs and form submissions (Base64URL encoding, no padding)
- Database health check endpoint at `/health`

### 🔄 Demo & Setup
- **One-click demo mode**: Clear database and import seed data from the `/Setup` page
- Pre-loaded with 22 skills, 3 projects, 13 workers, and 17 tasks for immediate exploration

---

## ⚙️ How It Works

```
┌──────────────────────────────────────────────────────────────────┐
│                        USER INTERFACE                            │
│  ASP.NET Core MVC · Tailwind CSS · Vanilla JS · Dark Mode        │
├──────────────────────────────────────────────────────────────────┤
│                       APPLICATION LAYER                          │
│  CQRS Manual · 20 Commands · 13 Queries · Handlers               │
├──────────────────────────────────────────────────────────────────┤
│                     INFRASTRUCTURE LAYER                         │
│  ADO.NET (Npgsql) · pgvector · AES-GCM ID Encryption             │
├──────────────────────────────────────────────────────────────────┤
│                        DOMAIN LAYER                              │
│  Entities · Value Objects · Business Rules · Enums               │
├──────────────────────────────────────────────────────────────────┤
│                      PostgreSQL + pgvector                       │
│  Vector(1024) columns · Stored Procedures · Triggers             │
└──────────────────────────────────────────────────────────────────┘
```

### Workflow

1. **Create a Project** → Define name and description
2. **Add Workers** → Each worker has a vectorized skill profile (0–10 scale)
3. **Create Tasks** → Each task requires specific skills at specific levels
4. **Assign Workers** → The engine recommends the best matches with compatibility badges
5. **Execute & Evaluate** → As tasks complete, evaluate worker performance per skill
6. **Watch Skills Evolve** → Worker skill vectors update automatically via XP system

---

## 🛠️ Tech Stack

### Backend
| Technology | Purpose |
|------------|---------|
| **C# / .NET 10** | Core language and runtime |
| **ASP.NET Core MVC** | Web framework (server-side rendered) |
| **ADO.NET (Npgsql 10.0.3)** | Database access (pure, no ORM) |
| **Pgvector 0.3.2** | Vector similarity search in PostgreSQL |
| **Serilog** | Structured logging (file sink) |
| **AES-GCM** | ID encryption for URLs |

### Frontend
| Technology | Purpose |
|------------|---------|
| **Tailwind CSS 3.4** | Utility-first styling |
| **Vanilla JavaScript** | Interactivity (Kanban, modals, dark mode, tooltips) |
| **HTML5 / Razor Views** | Server-side templating |

### Database
| Technology | Purpose |
|------------|---------|
| **PostgreSQL 16** | Primary database |
| **pgvector** | Vector operations and similarity search |
| **Stored Procedures** | Skill management, vector padding |

### Testing
| Technology | Purpose |
|------------|---------|
| **xUnit 2.9.3** | Unit & integration testing |
| **FluentAssertions 8.10** | Readable assertions |
| **NSubstitute 5.3** | Mocking (unit tests) |
| **Testcontainers 4.12** | Ephemeral PostgreSQL containers |
| **Playwright 1.61** | End-to-end browser testing |
| **WebApplicationFactory** | Integration test server |

### DevOps
| Technology | Purpose |
|------------|---------|
| **Docker** | Multi-stage builds |
| **Docker Compose** | Local orchestration (app + PostgreSQL) |
| **GitHub Actions** | CI/CD pipeline |
| **Render** | Production hosting (free tier) |

---

## 🏗️ Architecture

### Clean Architecture (Monolithic Modular)

The project follows **Clean Architecture** principles within a **modular monolith** pattern, enforcing strict dependency rules:

```
src/
├── SFManagement.Domain        ← Core (zero external dependencies)
├── SFManagement.Application   ← Use cases (depends only on Domain)
├── SFManagement.Infrastructure ← Persistence (ADO.NET, Npgsql, pgvector)
└── SFManagement.Web           ← MVC controllers, views, static assets
```

### CQRS Manual (No MediatR)

Read and write operations are separated explicitly using hand-rolled interfaces:

```csharp
// Commands (write operations)
ICommandHandler<TCommand>
ICommandHandler<TCommand, TResult>

// Queries (read operations)
IQueryHandler<TQuery, TResult>
```

**20 Commands** handle state changes (create, update, evaluate, archive, etc.)
**13 Queries** handle data retrieval (dashboard, recommendations, history, etc.)

### Key Architectural Decisions

| Decision | Rationale |
|----------|-----------|
| **No ORM (ADO.NET only)** | Full SQL control, pgvector compatibility, zero abstraction overhead |
| **No MediatR** | CQRS manual pattern for explicit dependency tracking |
| **C# 10 + .NET 10** | Modern language features, latest LTS |
| **100% English code** | Universal readability |
| **DRY + YAGNI** | No unnecessary abstractions or future-proofing |

---

## 📁 Project Structure

```
sfmanagement/
├── src/
│   ├── SFManagement.Domain/                  # Core business logic
│   │   ├── Entities/                         # Project, Worker, Task, Skill, etc.
│   │   ├── Enums/                            # TaskStatus, Criticality, etc.
│   │   ├── Exceptions/                       # Domain validation exceptions
│   │   └── ValueObjects/                     # SkillVector, SkillPosition, etc.
│   │
│   ├── SFManagement.Application/             # Use cases & DTOs
│   │   ├── Commands/                         # 20 CQRS commands
│   │   ├── Commands/Handlers/                # 20 command handlers
│   │   ├── Queries/                          # 13 CQRS queries
│   │   ├── Queries/Handlers/                 # 13 query handlers
│   │   ├── DTOs/                             # Data transfer objects
│   │   └── Interfaces/                       # Service contracts
│   │
│   ├── SFManagement.Infrastructure/          # Data access & external services
│   │   ├── Data/                             # NpgsqlConnectionFactory, RecyclableDataSource
│   │   ├── DependencyInjection.cs            # DI registration
│   │   ├── Handlers/Commands/                # ADO.NET implementations
│   │   ├── Handlers/Queries/                 # ADO.NET implementations
│   │   ├── Mappers/                          # DataReaderMapper (manual hydration)
│   │   ├── Security/                         # IdEncryptionService (AES-GCM)
│   │   ├── Health/                           # DatabaseHealthCheck
│   │   └── Seed/                             # Embedded init.sql
│   │
│   └── SFManagement.Web/                     # ASP.NET Core MVC
│       ├── Controllers/                      # 7 controllers
│       ├── ViewModels/                       # View models
│       ├── Views/                            # Razor views
│       │   ├── Shared/                       # Layout, partials, modals
│       │   ├── Dashboard/                    # Kanban board
│       │   ├── Project/                      # Project CRUD
│       │   ├── Task/                         # Task CRUD
│       │   ├── Worker/                       # Worker CRUD
│       │   ├── Skill/                        # Skill catalog
│       │   ├── Setup/                        # Demo mode
│       │   └── Home/                         # Landing page
│       └── wwwroot/
│           ├── js/                           # kanban, modal, toast, theme, etc.
│           └── css/                          # Tailwind output, loaders
│
├── tests/
│   ├── SFManagement.UnitTests/               # 31 tests (domain logic)
│   ├── SFManagement.IntegrationTests/        # 14 tests (DB operations)
│   └── SFManagement.E2ETests/               # 8 tests (Playwright browser)
│
├── database/
│   └── init.sql                              # Full schema + seed data
│
├── .github/workflows/
│   └── deploy.yml                            # CI/CD pipeline
│
├── Dockerfile                                # Multi-stage Docker build
├── docker-compose.yml                        # App + PostgreSQL orchestration
├── AGENTS.md                                 # AI assistant development rules
├── plan.md                                   # Development roadmap (125+ tasks)
├── CHANGELOG.md                              # Version history (Keep a Changelog)
├── LICENSE                                   # ***REMOVED***
└── .env.example                              # Environment variable template
```

---

## 🚀 Getting Started

### Prerequisites

- **.NET 10 SDK** — [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Docker & Docker Compose** — [Download](https://docs.docker.com/get-docker/) (recommended for database)
- **Node.js** (optional, for Tailwind CSS rebuild) — [Download](https://nodejs.org/)

### Option 1 — Docker Compose (Recommended)

The fastest way to get running with a pre-seeded database:

```bash
# Clone the repository
git clone https://github.com/dzarzacabrera/sfmanagement.git
cd sfmanagement

# Start everything (app + PostgreSQL with pgvector)
docker compose up --build

# Access the application
open http://localhost:8080
```

> The first boot takes ~1 minute. The database is automatically seeded with demo data.

### Option 2 — Local Development

**1. Start PostgreSQL with pgvector:**

```bash
# Using Docker for the database only
docker run -d \
  --name sf-postgres \
  -e POSTGRES_USER=sf_user \
  -e POSTGRES_PASSWORD=sf_password \
  -e POSTGRES_DB=sfmanagement \
  -p 5432:5432 \
  pgvector/pgvector:pg16
```

**2. Create the `.env` file:**

```bash
cp .env.example .env
# Edit .env with your PostgreSQL connection string
```

**3. Build and run:**

```bash
# Install frontend dependencies (if modifying Tailwind)
npm install

# Build Tailwind CSS
npm run build:css

# Run database migrations (init.sql is applied via Docker entrypoint or manually)
psql -h localhost -U sf_user -d sfmanagement -f database/init.sql

# Start the application
dotnet run --project src/SFManagement.Web
```

**4. Access:**

```
http://localhost:5000 (HTTP)
https://localhost:5001 (HTTPS)
```

### Option 3 — Run Tests

```bash
# Unit tests (no database required)
dotnet test tests/SFManagement.UnitTests/

# Integration tests (requires Docker for Testcontainers)
dotnet test tests/SFManagement.IntegrationTests/

# E2E tests (requires Docker + Playwright browsers)
dotnet test tests/SFManagement.E2ETests/

# All tests
dotnet test
```

---

## 🗄️ Database

### Schema Overview

PostgreSQL 16 with the **pgvector** extension for vector operations:

| Table | Description |
|-------|-------------|
| `skills_catalogue` | 22 predefined skills with immutable vector positions |
| `projects` | Projects with name, description, and finalization status |
| `workers` | Workers with name, role, and `vector(1024)` skill profile |
| `project_workers` | Many-to-many: workers assigned to projects |
| `tasks` | Tasks with criticality, status, and `vector(1024)` requirements |
| `task_assignments` | Workers assigned to specific tasks |
| `performance_evaluations` | Per-skill evaluation records with XP impact |

### Custom Types

- **`task_status`**: `Queued`, `InProgress`, `InReview`, `Finish`, `Archived`
- **`criticality`**: `low`, `medium`, `high`, `critical`

### Stored Procedures

- **`sp_add_skill(name, description, OUT new_id)`** — Auto-positions new skills in the vector
- **`util_pad_vector(float[], target_dim)`** — Pads skill arrays to vector(1024)

### Connection

```bash
# Default connection string
Host=localhost;Port=5432;Database=sfmanagement;Username=sf_user;Password=sf_password
```

---

## 🎮 Demo & Seed Data

### Using the Setup Page

Navigate to **Administration → Setup** (`/Setup`) to:

- **🗑️ Clear Database** — Truncates all tables (keeps schema intact)
- **📥 Import Seed Data** — Re-imports the full demo dataset

### Seed Data Contents

| Entity | Count | Details |
|--------|-------|---------|
| **Skills** | 22 | JavaScript, TypeScript, React, Angular, Node.js, .NET, Python, SQL, Docker, AWS, Git, REST API, GraphQL, CI/CD, TDD, Scrum, Communication, Leadership, Problem Solving, Time Management, English, Virtual Selling |
| **Projects** | 3 | SFManagement Core Platform, E-Commerce Platform Refactor, Digital Hunters |
| **Workers** | 13 | Full-stack, backend, and frontend profiles with calibrated skill vectors |
| **Tasks** | 17 | Feature development, bug fixes, and infrastructure tasks |
| **Evaluations** | Multiple | Performance evaluations with XP impact records |

---

## 🔄 CI/CD Pipeline

### GitHub Actions Workflow

Triggered on push to `release/sfmanagement.V*` branches:

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  Push to        │     │   Run Tests     │     │   Deploy to     │
│  release/       │────▶│   (Unit +       │────▶│   Render        │
│  sfmanagement.V*│     │   Integration)  │     │   (via webhook) │
└─────────────────┘     └─────────────────┘     └─────────────────┘
```

**Job 1 — Test:**
- .NET 10 SDK setup
- Restore, Build (Release)
- Run Unit Tests + Integration Tests
- Testcontainers with PostgreSQL pgvector

**Job 2 — Deploy:**
- Sends `curl` to Render deploy hook
- Auto-deploys on success

### Render Hosting

- **URL:** [sfmanagement.onrender.com](https://sfmanagement.onrender.com)
- **Note:** First access triggers cold start (40s–1min wake-up)
- **Auto-deploy:** Triggered by GitHub Actions via deploy hook

---

## 📊 Project Lifecycle

### Kanban Board States

```
┌──────────┐    ┌──────────────┐    ┌────────────┐    ┌────────┐    ┌───────────┐
│  Queued  │───▶│  In Progress │───▶│  In Review │───▶│ Finish │───▶│ Archived  │
│          │◀───│              │◀───│            │◀───│        │◀───│           │
└──────────┘    └──────────────┘    └────────────┘    └────────┘    └───────────┘
```

- **Free transitions** between Queued, In Progress, In Review, and Finish
- **Finish ↔ Archived** allowed (Archived can only go back to Finish)
- **Finish + All Evaluated** → Controls hidden, only Archive available

### Task Editing Rules

| Status | Can Edit? |
|--------|-----------|
| Queued | ✅ Yes |
| In Progress | ✅ Yes |
| In Review | ❌ No |
| Finish | ❌ No |
| Archived | ❌ No |

---

## 🎯 The Matching Algorithm

### Vector-Based Skill Matching

All skills are mapped to positions in a 1024-dimensional vector. Worker skills and task requirements are stored as `vector(1024)` columns.

### Compatibility Score Calculation

```
Score = Σ min(worker_skill[i], required_skill[i]) / Σ required_skill[i]
         (for all positions where required_skill[i] > 0)
```

### Recommendation Badges

| Badge | Criteria | Color |
|-------|----------|-------|
| **🏆 Most Efficient** | Score ≥ 95%, no excess capacity | Green |
| **📈 Grow Up** | Score 75–95%, closest to 87% target | Blue |
| **⚡ Fastest to Finish** | Score ≥ 100% (excess), least total excess | Orange |

### Example

```
Task requires: [JS:8, React:6, SQL:4]
Worker A has:  [JS:9, React:7, SQL:3]  → Score = (8+6+3)/(8+6+4) = 94.4%  → 🏆 Most Efficient
Worker B has:  [JS:6, React:5, SQL:8]  → Score = (6+5+4)/(8+6+4) = 83.3%  → 📈 Grow Up
Worker C has:  [JS:10, React:8, SQL:6] → Score = (8+6+4)/(8+6+4) = 100%   → ⚡ Fastest to Finish
```

---

## ⚡ Evaluation & XP System

### How It Works

When a task is marked as **Finish**, the project manager evaluates each assigned worker's performance per skill:

1. **Select Skills** — Only skills required by the task are available for evaluation
2. **Rate Performance** — 0 (Poor) to 10 (Excellent) per skill
3. **Calculate Impact** — `Impact = basePoints × criticalityMultiplier`
4. **Apply to Vector** — `newLevel = oldLevel + impact`
5. **Clamp Result** — `Math.Clamp(newLevel, 0.0, 10.0)` (hard bounds)
6. **Round** — Nearest 0.05 increment

### Criticality Multipliers

| Level | Multiplier | Description |
|-------|------------|-------------|
| **Low** | 0.5× | Minor tasks, documentation |
| **Medium** | 1.0× | Standard development work |
| **High** | 1.5× | Important features, complex bugs |
| **Critical** | 2.0× | Production incidents, core features |

### Manual Skill Editing

Workers can also have their skills manually adjusted (outside task evaluation). These edits:
- Are stored as evaluation records with `criticality='low'`
- Are attached to a "Manual Adjustment" task
- Follow the same clamping rules

---

## 🎨 UI/UX Features

### Responsive Design

- **Mobile-first** approach with Tailwind CSS breakpoints
- Collapsible sidebar (slide-in overlay on screens < 1024px)
- Bottom sheets for mobile modals and status changes
- Task cards with quick-action arrows on small screens

### Dark Mode

- Class-based dark mode with `dark:` Tailwind variants
- System preference detection via `prefers-color-scheme`
- localStorage persistence
- Toggle in sidebar header (moon/sun icon)

### Interactive Elements

- **Kanban Drag & Drop** — Reorder tasks within columns
- **Modal System** — Focus trap, CSS animations, bottom sheet fallback
- **Toast Notifications** — Success/error/info/warning with auto-dismiss
- **Tooltips** — Auto-positioning on truncated text (`data-tooltip`)
- **Skill Pills** — Searchable selector with level input (0–10)
- **Loading Skeletons** — Shimmer effect during data fetch
- **Pulse Animation** — Highlights "Add Worker" when no workers assigned

### Accessibility (WCAG 2.1 AA)

- Focus management in modals
- ARIA attributes on interactive elements
- Keyboard navigation support
- Color contrast compliance
- Screen reader friendly labels

---

## 🧪 Testing Strategy

### Three-Level Pyramid

```
         ┌───────────────────┐
         │   E2E (Playwright)│  ← 8 tests: Full browser workflows
         ├───────────────────┤
         │  Integration      │  ← 14 tests: DB + HTTP operations
         ├───────────────────┤
         │  Unit             │  ← 31 tests: Domain logic, clamping, XP
         └───────────────────┘
```

### Unit Tests (31 tests)

| File | Coverage |
|------|----------|
| `SkillVectorTests.cs` | Vector clamping, `ApplyImpact`, rounding |
| `XpCalculationTests.cs` | Criticality multipliers, impact calculation |
| `DomainValidationTests.cs` | Task status transitions, edit restrictions |

### Integration Tests (14 tests)

| File | Coverage |
|------|----------|
| `ProjectLifecycleTests.cs` | Create/update projects via DB |
| `WorkerAssignmentTests.cs` | Worker allocation, pgvector recommendations |
| `PerformanceEvaluationTests.cs` | Full evaluation with skill updates |
| `TaskStatusTransitionTests.cs` | Valid/invalid status transitions |
| `TaskEditRestrictionTests.cs` | Edit permissions by status |

### E2E Tests (8 tests)

| File | Coverage |
|------|----------|
| `KanbanStateChangeTests.cs` | Dashboard columns, status changes |
| `WorkerAssignmentFlowTests.cs` | Assign worker via UI |
| `PerformanceEvaluationFlowTests.cs` | Evaluate task end-to-end |
| `SkillsCrudTests.cs` | Skill create/deactivate/reactivate |
| `WorkerEditFlowTests.cs` | Edit worker and verify |

### Test Infrastructure

- **Testcontainers.PostgreSql** — Ephemeral PostgreSQL containers with pgvector
- **WebApplicationFactory** — In-memory test server for integration tests
- **Playwright** — Chromium headless browser for E2E
- **NSubstitute** — Mocking framework for unit tests
- **FluentAssertions** — Readable assertion syntax

---

## 📅 Development Phases

### Phase 1: Conceptualization & Initial Planning
- MVP definition using Gemma 4 (local, via voice notes + LocalWhispers + OpenClaw)
- Migration to Google Gemini for detailed technical refinement

### Phase 2: Methodological Design & Architecture (Gemini)
- Vector matching engine design (pgvector, scalar product inverse)
- Architectural manifesto (`agents.md`): Clean Architecture, CQRS Manual, ADO.NET only
- Frontend: Tailwind CSS, Vanilla JS, Mobile-First, WCAG 2.1 AA
- Evaluation Kanban: 4-column immutable flow, XP with clamping
- Testing: xUnit, FluentAssertions, NSubstitute, Testcontainers, Playwright

### Phase 3: Technical Refinement & Action Plan (OpenCode)
- Synthesis extraction from Gemini
- Import into OpenCode for planning
- `plan.md` creation: 125+ tasks across 7 phases

### Phase 4: Software Development & Guided Implementation
- Iterative coding following `plan.md`
- OpenCode + Big Pickle model synergy
- Rapid UI prototyping with Vercel v0
- Continuous testing and refinement

### Phase 5: Deployment & Configuration
- GitHub Actions CI/CD on `release/sfmanagement.V*` branches
- Automated test → build → deploy pipeline
- Render hosting (free tier, cold start ~40s–1min)
- Loging: Serilog

---

## 🚀 Future Improvements

Planned enhancements for upcoming iterations of SFManagement:

- **Multi-User Login System** — Add authentication and authorization to support multiple users, each managing their own projects and teams securely.

- **Review Incidents & Evaluation Multipliers** — When a task is in the *In Review* state, allow logging incidents classified as *minor*, *medium*, or *severe*. These incidents would impact the score multiplier applied during the final worker evaluation, penalizing performance according to incident severity.

- **Worker Evaluation Charts & Tracking** — Add graphical dashboards detailing each worker's evaluations to enable finer skill tracking. This would let managers act proactively when a skill drops significantly — e.g. providing training or discussing personal circumstances (family situations, etc.) with the worker.

- **Richer Task Metadata** — Add more information to tasks (e.g. estimated duration in hours) to give longer tasks greater weight in evaluations.

- **Local AI for Task & Skill Creation** — Integrate a local AI to assist in creating tasks and skills when a project is created, and to automatically manage and assign workers on the Kanban board.

- **Slack (or similar) Webhook Integration** — Create a webhook (e.g. Slack) to receive messages for creating tasks or other actions. The AI would receive the message and act as it deems appropriate.

- **AI Workers (Autonomous Agents)** — Create AI-powered workers (autonomous agents) backed by different LLMs, routing each task to the most suitable agent. An orchestrator would manage the application and assign jobs to the right agent.

---

## 🤝 Contributing

Contributions are welcome! Please follow these guidelines:

1. **Read `AGENTS.md`** — All architectural rules and coding standards are defined there
2. **Follow Clean Architecture** — Domain → Application → Infrastructure → Web
3. **No ORMs** — Use ADO.NET (Npgsql) for all database operations
4. **CQRS Manual** — Write operations as Commands, read operations as Queries
5. **100% English code** — Spanish only in UI text for end users
6. **Test your changes** — Write unit tests for business logic at minimum

### Development Setup

```bash
# Fork and clone
git clone https://github.com/YOUR_USERNAME/sfmanagement.git

# Create a feature branch
git checkout -b feature/my-feature

# Make changes following AGENTS.md rules

# Run tests
dotnet test

# Push and create PR
git push origin feature/my-feature
```

---

## 📄 License

This project is licensed under a Proprietary License — see the [LICENSE](LICENSE) file for details.

```
Proprietary License - Skill Forge Management. All rights reserved.
```

---

## 📬 Contact

- **GitHub:** [dzarzacabrera](https://github.com/dzarzacabrera)
- **Repository:** [sfmanagement](https://github.com/dzarzacabrera/sfmanagement)
- **Issues:** [Report a bug](https://github.com/dzarzacabrera/sfmanagement/issues)

---

<div align="center">

**Built with determination and strict architectural rules.**

*No AI services in production. No ORMs. No shortcuts.*

</div>
