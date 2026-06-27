# ADR 001: Adopción de ADO.NET Puro (Npgsql) y Rechazo de ORMs

## Estado
Aceptado

## Contexto
El sistema **Skill Forge Management (SFManagement)** requiere un control milimétrico sobre operaciones de álgebra lineal y emparejamiento vectorial a través de la extensión `pgvector` en PostgreSQL. Tradicionalmente, se recurre a Object-Relational Mappers (ORMs) como Entity Framework Core o micro-ORMs como Dapper para gestionar la persistencia en C#.

## Decisión Técnica
Se rechaza explícitamente el uso de Entity Framework Core y Dapper, adoptando en su lugar **ADO.NET Puro** mediante el proveedor nativo **`Npgsql`** y su extensión oficial **`Pgvector`**.

Esta decisión se fundamenta en los siguientes pilares de diseño:
1. **Principio YAGNI (You Ain't Gonna Need It):** No se requieren abstracciones de mapeo complejas, sistemas de seguimiento de estado (*change tracking*) ni generadores automáticos de consultas SQL que oculten las operaciones lógicas del sistema.
2. **Control del Tipo de Dato `vector`:** `Npgsql` permite registrar el tipo `vector` de forma nativa en el `NpgsqlDataSource`, permitiendo mapear arrays flotantes (`float[]`) de C# directamente a la base de datos sin necesidad de configurar intermediarios o hacks de conversión en un ORM.
3. **CQRS Manual Eficiente:** Al escribir los comandos y consultas con un flujo manual, las clases Handlers abren, parametrizan y leen los flujos de datos a través de un `NpgsqlDataReader`, garantizando la máxima velocidad de ejecución y un consumo de memoria mínimo (Cercano a cero asignaciones).

## Consecuencias
* **Positivas:** Rendimiento de lectura y escritura drásticamente superior a cualquier ORM; total transparencia entre las consultas matemáticas explicadas en la memoria del TFC y el código fuente; cumplimiento estricto de los principios de diseño.
* **Negativas:** Se debe escribir de forma manual todo el mapeo de hidratación de objetos desde los índices del DataReader, incrementando el volumen de líneas de código en la capa de Infraestructura.
