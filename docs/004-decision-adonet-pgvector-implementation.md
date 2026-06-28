# ADR 004: Implementación ADO.NET Puro con Npgsql y Pgvector

## Estado
Aceptado

## Contexto
La Fase 3 de SFManagement requiere implementar la capa de Infraestructura que conecta los comandos y consultas CQRS con PostgreSQL. Las restricciones arquitectónicas impiden el uso de ORMs (EF Core, Dapper). El tipo `vector` de pgvector debe ser soportado para las operaciones de matching y actualización de skills.

## Decisión Técnica

### Connection Factory
Se define `INpgsqlConnectionFactory` como interfaz que expone `GetOpenConnectionAsync()`. Su implementación `NpgsqlConnectionFactory` recibe un `NpgsqlDataSource` (singleton) y crea conexiones abiertas bajo demanda. El `DataSource` se configura en `Program.cs` con `UseVector()` para habilitar el mapeo automático del tipo `vector`.

### Mapeo del Tipo Vector
- **Lectura:** `NpgsqlDataReader.GetFieldValue<Pgvector.Vector>(ordinal)` devuelve un objeto `Vector`. Se convierte a `float[]` via `.ToArray()`.
- **Escritura:** Los parámetros SQL se construyen con `new Pgvector.Vector(floatArray)` para que Npgsql serialice correctamente al formato nativo de pgvector.

### Handlers de Comandos
Cada command handler sigue el patrón:
1. Obtener conexión del factory
2. Crear `NpgsqlCommand` con SQL parametrizado (placeholders `$1`, `$2`, etc.)
3. Ejecutar `ExecuteNonQueryAsync()`, `ExecuteScalarAsync()` o `ExecuteReaderAsync()`
4. Para comandos que devuelven ID, usar `RETURNING id` en el SQL

Los handlers `ChangeTaskStatusCommandHandler` y `UpdateTaskCommandHandler` cargan la entidad `ProjectTask` desde la BD, ejecutan validación de dominio (`ChangeStatus()` / `UpdateDetails()`), y luego persisten los cambios. Esto garantiza que las reglas de negocio (transiciones Kanban, edición solo en Queued) no sean eludidas.

### Handlers de Consultas
Usan `INpgsqlConnectionFactory` para ejecutar SELECTs y mapear manualmente cada fila mediante `DataReaderMapper`. El handler `GetRecommendedWorkersQueryHandler` ejecuta el producto escalar `<#>` de pgvector y lo multiplica por -1 para obtener la similitud coseno ascendente.

### DataReaderMapper
Clase helper interna que envuelve un `NpgsqlDataReader` y proporciona métodos tipados (`GetInt32`, `GetString`, `GetVector`, `GetEnum`, etc.) para hidratar DTOs. Usa `Enum.Parse<T>(ignoreCase: true)` para mapear los valores en minúscula de la BD (`'low'`, `'medium'`, etc.) a los enums PascalCase de C#.

### DI Registration
Método de extensión `AddInfrastructure()` en `SFManagement.Infrastructure.DependencyInjection` que registra:
- `NpgsqlDataSource` como singleton con `UseVector()`
- `INpgsqlConnectionFactory` como singleton
- Todos los command/query handlers como transient

## Consecuencias
- **Positivas:** Control total sobre las consultas SQL; las operaciones vectoriales se ejecutan directamente en PostgreSQL sin capas intermedias; las reglas de negocio se validan en el dominio antes de persistir.
- **Negativas:** Mayor volumen de código boilerplate (cada handler repite el patrón conexión → comando → ejecución); el mapeo manual de DataReader es propenso a errores si cambia el schema.

## Archivos Afectados
- `src/SFManagement.Infrastructure/Data/INpgsqlConnectionFactory.cs`
- `src/SFManagement.Infrastructure/Data/NpgsqlConnectionFactory.cs`
- `src/SFManagement.Infrastructure/Mappers/DataReaderMapper.cs`
- `src/SFManagement.Infrastructure/Handlers/Commands/` (9 handlers)
- `src/SFManagement.Infrastructure/Handlers/Queries/` (4 handlers)
- `src/SFManagement.Infrastructure/DependencyInjection.cs`
- `src/SFManagement.Web/Program.cs`
- `tests/SFManagement.IntegrationTests/` (6 archivos de test)
