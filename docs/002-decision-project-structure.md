# ADR 002: Estructura de Proyectos y Schema de Base de Datos

## Estado
Aceptado

## Contexto
El sistema SFManagement requiere una estructura de solución .NET que refleje los principios de Clean Architecture (Monolito Modular) con CQRS Manual. La base de datos debe soportar operaciones vectoriales mediante pgvector con nombres en inglés estricto.

## Decisión Técnica

### Estructura de Proyectos
Se opta por 4 proyectos separados siguiendo Clean Architecture:
1. **SFManagement.Domain** — Entidades puras, Value Objects, Enums. Sin dependencias externas.
2. **SFManagement.Application** — Casos de uso (Commands/Queries), interfaces CQRS, DTOs. Depende solo de Domain.
3. **SFManagement.Infrastructure** — Implementación ADO.NET (Npgsql + Pgvector), Handlers reales, Mappers. Depende de Application.
4. **SFManagement.Web** — Controladores MVC, Vistas Razor, ViewModels, Vanilla JS. Depende de Application e Infrastructure.

### Testing
3 proyectos de test independientes:
- **UnitTests** — xUnit + FluentAssertions + NSubstitute (aislado, sin BD)
- **IntegrationTests** — WebApplicationFactory + Testcontainers.PostgreSql (BD real Docker)
- **E2ETests** — Playwright (navegador real sobre HTML Razor)

### Husky.Net
Git hooks automatizados para calidad de código:
- **pre-commit**: `dotnet format --verify-no-changes`
- **pre-push**: `dotnet test tests/SFManagement.UnitTests`

### Base de Datos
- **Motor:** PostgreSQL 16 + pgvector
- **Idioma:** Strict English (skills_catalogue, workers, projects, tasks, performance_evaluations)
- **Enums:** task_status (Queued, InProgress, Test, Finish), criticality (low, medium, high, critical), performance_rating (Poor, Average, Good, Excellent)
- **Vector:** 12 dimensiones fijas mapeadas al catálogo de skills
- **Seeding:** 12 skills, 4 workers, 2 projects, 4 tasks

### Docker
- `Dockerfile` multi-stage (SDK 10.0 build → aspnet 10.0 runtime)
- `docker-compose.yml` con PostgreSQL 16 + pgvector e inicialización automática via `init.sql`

## Consecuencias
- **Positivas:** Separación clara de responsabilidades, testing real con pgvector, entorno reproducible via Docker, calidad de código validada en cada commit.
- **Negativas:** Mayor número de proyectos que mantener, los tests de integración requieren Docker en la máquina de desarrollo.
