# ADR 003: Skill Vector y Cálculo de XP en el Dominio

## Estado
Aceptado

## Contexto
El núcleo del motor de SFManagement requiere un sistema de evaluación de habilidades representado como vectores numéricos de dimensión fija (12 posiciones). Cada posición corresponde a una skill del catálogo inmutable. El sistema necesita:

1. **Clamping estricto** (0.0–10.0) para mantener la integridad del vector tras operaciones.
2. **Fórmula de impacto** que combine la criticidad de una tarea y el rating de desempeño del trabajador.
3. **Validación de transiciones** de estado Kanban para garantizar el ciclo de vida correcto de las tareas.

## Decisión Técnica

### SkillVector (Value Object)
- Se implementa como un `readonly record struct` que envuelve un `float[]`.
- El constructor aplica `Math.Clamp` (0.0, 10.0) a cada valor de entrada.
- Método `ApplyImpact(index, basePoints, criticalityMultiplier)`:
  - Calcula `newValue = Values[index] + (basePoints * criticalityMultiplier)`
  - Aplica clamping al resultado
  - Retorna un nuevo `SkillVector` (inmutable)
- Método `CalculateCriticalityMultiplier(Criticality)`:
  - Low → 0.5, Medium → 1.0, High → 1.5, Critical → 2.0

### PerformanceRating.ToBasePoints()
- Poor → -0.5, Average → 0.0, Good → +0.2, Excellent → +0.5

### ProjectTask.ChangeStatus()
Solo se permiten las siguientes transiciones:
- `Queued → InProgress`
- `InProgress → Blocked`, `InProgress → Finish`
- `Blocked → Queued`, `Blocked → InProgress`
- Cualquier transición hacia/desde `Finish` está prohibida (estado terminal).
- `Queued → Finish` directo está prohibido.

### ProjectTask.UpdateDetails()
- Solo permitido cuando `Status == Queued`.
- Lanza `InvalidOperationException` en cualquier otro estado.

## Consecuencias
- **Positivas:** Reglas de negocio centralizadas y testeadas unitariamente (31 tests). Transiciones Kanban forzadas por el dominio, no por la UI. Clamping garantiza que ningún error de redondeo o cálculo manual rompa la escala 0–10.
- **Negativas:** Toda operación de modificación de vector requiere crear una nueva instancia (inmutabilidad). El dominio no tiene acceso a la BD, por lo que la validación de existencia de workers/proyectos/skills se delega a los Handlers de Aplicación.

## Archivos Afectados
- `src/SFManagement.Domain/ValueObjects/SkillVector.cs` — Lógica de vector, clamping, ApplyImpact
- `src/SFManagement.Domain/Enums/PerformanceRating.cs` — ToBasePoints()
- `src/SFManagement.Domain/Enums/Criticality.cs` — Enumeración de criticidad
- `src/SFManagement.Domain/Entities/ProjectTask.cs` — ChangeStatus(), UpdateDetails()
- `tests/SFManagement.UnitTests/SkillVectorTests.cs` — 12 tests de SkillVector
- `tests/SFManagement.UnitTests/XpCalculationTests.cs` — 8 tests de multiplicadores e impacto
- `tests/SFManagement.UnitTests/DomainValidationTests.cs` — 11 tests de validación de dominio
