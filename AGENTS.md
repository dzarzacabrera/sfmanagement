# agents.md

# 🤖 Reglas de Desarrollo y Directrices del Asistente IA (SFManagement)

Este documento establece las reglas arquitectónicas, principios de diseño, restricciones técnicas y estándares de documentación obligatorios para el desarrollo de **Skill Forge Management (SFManagement)**. Cualquier sugerencia de código, refactorización o nueva funcionalidad debe alinearse estrictamente con estas directrices.

---

## 👥 Índice de Agentes Especializados

Para el desarrollo de tareas específicas, se deben invocar los siguientes sub-agentes según el contexto del ticket:
- **[Frontend Expert](./.github/agents/frontend.md)**: Usabilidad, accesibilidad WCAG y rendimiento nativo en interfaces Razor.
- **[Security Expert](./.github/agents/security.md)**: Auditoría de código bajo OWASP Top 10 y protección de datos.
- **[Architecture Architect](./.github/agents/architecture.md)**: Monolito Modular, Clean Architecture, CQRS Manual, DRY y YAGNI.
- **[Database Specialist](./.github/agents/database.md)**: ADO.NET Puro, Npgsql, pgvector y operaciones matemáticas en PostgreSQL.
- **[Testing Automation](./.github/agents/testing.md)**: xUnit, FluentAssertions, Testcontainers y Playwright E2E.


## 🏗️ 1. Arquitectura y Estilo de Diseño

### Monolito Modular con Clean Architecture
El sistema se construirá como un Monolito Modular. Cada módulo encapsula su propia lógica de negocio y se organiza internamente siguiendo los principios de **Clean Architecture**:
*   **Domain (Núcleo):** Entidades, lógica de negocio pura, value objects, excepciones de dominio y contratos (interfaces) básicos. Cero dependencias externas.
*   **Application:** Casos de uso, orquestación de flujos, comandos, consultas y manejadores (Handlers). Depende únicamente del Dominio.
*   **Infrastructure:** Implementación de persistencia de bajo nivel mediante ADO.NET Puro, servicios externos, adaptadores y configuración de la base de datos con PostgreSQL.
*   **Presentation (Web MVC):** Controladores, Vistas, ViewModels y Dashboards de usuario.

### Principios Fundamentales de Código
*   **DRY (Don't Repeat Yourself):** Evitar la duplicación de lógica de negocio y cálculos matemáticos (especialmente el algoritmo de match vectorial y de XP).
*   **YAGNI (You Ain't Gonna Need It):** No añadas código, generalizaciones o capas de abstracción innecesarias para necesidades futuras hipotéticas. Programa exclusivamente lo requerido para cumplir las reglas actuales de SFManagement.

### CQRS Manual (Command Query Responsibility Segregation)
No se permite el uso de librerías de mediación externas (como MediatR). La separación de lectura y escritura se gestionará de manera explícita y manual:
*   **Commands:** Clases inmutables que representan intenciones de cambio de estado (ej: `RegistrarEvaluacionCommand`). Tienen su propio Handler dedicado (`ICommandHandler<TCommand>`).
*   **Queries:** Clases inmutables que representan consultas de datos (ej: `ObtenerDesarrolladoresRecomendadosQuery`). Tienen su propio Handler dedicado (`IQueryHandler<TQuery, TResult>`).
*   Los Controladores MVC inyectarán directamente los Handlers específicos necesarios para cada acción.

---

## 🛠️ 2. Stack Tecnológico Estricto e Innegociable

### Prohibición Expresa de ORMs
*   **Queda totalmente prohibido el uso de Entity Framework Core, Dapper o cualquier otro ORM/Micro-ORM.**
*   Toda la persistencia y lectura de datos debe realizarse exclusivamente mediante **ADO.NET Puro** utilizando el proveedor nativo **`Npgsql`** y su extensión **`Pgvector`**.
*   Las consultas se escribirán en SQL crudo parametrizado dentro de los Handlers de la infraestructura.
*   La lectura de datos se procesará de forma secuencial y eficiente utilizando `NpgsqlDataReader`.
*   El mapeo e hidratación de objetos desde la base de datos hacia las entidades o DTOs debe ser manual.

### Especificaciones Técnicas
*   **Lenguaje:** C# Net 10.
*   **Tipo de Proyecto:** ASP.NET Core Web App (MVC).
*   **Idioma del Código (Strict English Only):** Todo el código fuente sin excepción debe escribirse en inglés. Esto incluye: nombres de proyectos, namespaces, nombres de clases (`Project`, `Task`, `Worker`, `SkillEvaluation`), variables, propiedades, métodos, tablas de la base de datos, columnas, comentarios en el código, comandos, consultas y mensajes de log. El español queda relegado única y exclusivamente a los textos visuales de cara al usuario final en las vistas Razor del Frontend (UI).
*   **Base de Datos:** PostgreSQL con la extensión `pgvector` habilitada.
*   **Mapeo de Vectores:** Uso obligatorio de `NpgsqlDataSourceBuilder.UseVector()` en el inicio de la aplicación para enlazar la estructura `Vector` de C# con el tipo de dato nativo `vector` de Postgres. Los vectores en memoria se manejan como arrays de flotantes (`float[]`) en la escala acordada.
*   **Dockerización (Opcional/Soporte):** Se debe incluir un archivo `Dockerfile` multi-stage y un archivo `docker-compose.yml` en la raíz para permitir levantar el entorno de la aplicación MVC y la base de datos relacional con `pgvector` de forma unificada mediante un único comando si se requiere.
---

## 📝 3. Documentación y Trazabilidad (Obligatorio)

### 1. Documentación en `docs/`
*   Toda nueva funcionalidad, decisión técnica o explicación de diseño debe ser documentada por el asistente IA en archivos `.md` independientes dentro de la carpeta `docs/` en la raíz del proyecto.
*   Cada archivo debe incluir: Contexto del problema, Decisión adoptada, Estructura del código afectado y Consecuencias técnicas.

### 2. Gestión de Cambios (CHANGELOG.md)
*   Cada cambio, mejora o corrección debe quedar registrado obligatoriamente en el archivo `CHANGELOG.md` situado en la raíz del proyecto.
*   Se debe seguir estrictamente el estándar de [Keep a Changelog](https://keepachangelog.com).
*   **Formato de Agrupación:** Clasificar las modificaciones exclusivamente bajo las etiquetas: `Added`, `Changed`, `Deprecated`, `Removed`, `Fixed`, `Security`.
*   **Ordenación:** Cronológica inversa (el bloque de cambios más reciente debe situarse en la parte superior).
*   **Versionado:** Las versiones aplicadas deben respetar escrupulosamente el Versionamiento Semántico (SemVer).

---

## ⚙️ 4. Reglas del Motor de SFManagement a Respetar

Cuando programes o refactorices lógica asociada a las competencias, debes forzar matemáticamente estas condiciones:
1.  **Escala Estricta (0-10):** Los valores de los vectores deben representarse con números de este rango (0=No tiene, 2=Básico, 4=Básico-Medio, 6=Medio, 8=Medio-Avanzado, 10=Avanzado).
2.  **Operaciones en BD:** La búsqueda de perfiles compatibles debe resolverse mediante una consulta SQL cruda parametrizada utilizando el operador de producto escalar `<#>` de `pgvector` multiplicado por `-1`, restringiendo los trabajadores al id del proyecto en cuestión.
3.  **Dropdowns No Libres:** No generes entradas de texto para habilidades. Las habilidades provienen de un catálogo inmutable indexado por una posición en el vector.
4.  **Cálculo de Desempeño (Clamping):** Al procesar un comando de finalización de tarea, aplica la fórmula de impacto neto (Puntos Base $\times$ Multiplicador de Criticidad) e implementa un *clamp* matemático que impida de forma absoluta que el valor de una skill sea inferior a `0.0` o superior a `10.0`.
5.  **Estados de las Tareas (Kanban Core):** El sistema debe manejar de forma obligatoria y estricta el ciclo de vida de las tareas reflejado en 4 estados en la base de datos: `Queued`, `In Progress`, `Test` y `Finish`. El Dashboard renderizará estos estados en 4 columnas responsivas con el orden natural: **Queued → In Progress → Test → Finish**. Las transiciones entre Queued, InProgress, Test y Finish son totalmente libres (cualquier estado a cualquier otro). Una tarea en estado Finish con todos los workers evaluados (AllWorkersEvaluated) solo se puede archivar o permanecer en Finish — los controles de cambio de estado (flechas y botón "Change Status") se ocultan automáticamente. Las acciones para actualizar el estado o abrir el modal de asignación de un `Worker` deben ejecutarse mediante Vanilla JS enviando peticiones asíncronas hacia los Command Handlers del Backend.
6.  **Controles Móviles de Estado:** En pantallas pequeñas (`sm:`), cada tarjeta de tarea incluye:
    *   **Flechas de acción rápida**: `←` (retrocede al estado anterior) y `→` (avanza al siguiente estado) en la jerarquía natural. Test puede ir a InProgress (`←`) o a Finish (`→`). Las flechas siempre se muestran; si la transición no es válida para el estado actual, aparecen deshabilitadas (`pointer-events-none`, `text-gray-300`).
    *   **Selector de estado (Bottom Sheet)**: Al tocar la etiqueta del estado actual, se abre un panel deslizante desde abajo con los 4 estados disponibles. Al seleccionar uno, se envía `POST /Dashboard/ChangeStatus`.

## 🧪 5. Pirámide de Pruebas Obligatoria

Toda sugerencia de desarrollo debe ir acompañada de su correspondiente suite de pruebas bajo el estándar de `xUnit` y `FluentAssertions`, estructurada en tres niveles lógicos:

### 1. Pruebas Unitarias (Imprescindibles)
*   **Propósito:** Probar la lógica de negocio pura de forma aislada, sin tocar la base de datos ni servicios externos.
*   **Qué probar:** Métodos de cálculo de XP por criticidad, el truncamiento matemático (*clamping*) del vector de 0 a 10, validaciones de formularios y mapeadores manuales.
*   **Aislamiento:** Uso estricto de `NSubstitute` para simular (*mockear*) el comportamiento de servicios o repositorios y aislar la lógica bajo prueba.

### 2. Pruebas de Integración (Altamente Recomendadas)
*   **Propósito:** Verificar que los controladores de la arquitectura MVC se comuniquen correctamente con la base de datos real y los componentes intermedios de la infraestructura.
*   **Estrategia:** Uso nativo de `WebApplicationFactory` para levantar toda la aplicación MVC en un servidor de prueba en memoria durante el test.
*   **Base de datos para test:** **Queda prohibido el uso de bases de datos EF In-Memory o SQLite.** Para garantizar la compatibilidad con el operador `<#>` de `pgvector` y las consultas ADO.NET puras, los tests deben orquestar un contenedor Docker efímero y real utilizando la librería `Testcontainers.PostgreSql`. Se probará que las acciones del Controlador realicen escrituras y lecturas consistentes.

### 3. Pruebas End-to-End (E2E) con Playwright
*   **Propósito:** Validar que el flujo completo de la interfaz de usuario interactúe correctamente con el HTML real renderizado en el servidor por el motor de vistas Razor.
*   **Qué probar:** El camino crítico del usuario en el Dashboard (ej: El flujo completo desde que el usuario navega al formulario de creación de tareas, selecciona una skill desde el dropdown controlado, asigne un desarrollador recomendado por el algoritmo, y el navegador redirija de forma exitosa visualizando los cambios).

