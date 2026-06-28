-- =========================================================================
-- SKILL FORGE MANAGEMENT (SFManagement) - Database Schema
-- PostgreSQL 16 + pgvector
-- Strict English naming convention
-- =========================================================================

-- Enable pgvector extension
CREATE EXTENSION IF NOT EXISTS vector;

-- =========================================================================
-- 1. ENUMS
-- =========================================================================
DO $$ BEGIN
    CREATE TYPE task_status AS ENUM ('Queued', 'InProgress', 'Blocked', 'Finish');
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;
DO $$ BEGIN
    CREATE TYPE criticality AS ENUM ('low', 'medium', 'high', 'critical');
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;
DO $$ BEGIN
    CREATE TYPE performance_rating AS ENUM ('Poor', 'Average', 'Good', 'Excellent');
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

-- =========================================================================
-- 2. TABLES
-- =========================================================================

-- 2.1 Global Skills Catalogue (immutable dimension index)
CREATE TABLE IF NOT EXISTS skills_catalogue (
    id              INT PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
    name            VARCHAR(100) NOT NULL,
    vector_position INT NOT NULL UNIQUE,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE
);

-- 2.2 Projects
CREATE TABLE IF NOT EXISTS projects (
    id             INT PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
    name           VARCHAR(200) NOT NULL,
    description_md TEXT
);

-- 2.3 Workers
CREATE TABLE IF NOT EXISTS workers (
    id            INT PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
    name          VARCHAR(200) NOT NULL,
    role          VARCHAR(100) NOT NULL DEFAULT '',
    skills_vector vector(1024) NOT NULL
);

-- 2.4 Project-Worker Allocation (scope boundary)
CREATE TABLE IF NOT EXISTS project_workers (
    project_id INT NOT NULL REFERENCES projects(id) ON DELETE CASCADE,
    worker_id  INT NOT NULL REFERENCES workers(id) ON DELETE CASCADE,
    PRIMARY KEY (project_id, worker_id)
);

-- 2.5 Tasks (Kanban items)
CREATE TABLE IF NOT EXISTS tasks (
    id                     INT PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
    project_id             INT NOT NULL REFERENCES projects(id) ON DELETE CASCADE,
    title                  VARCHAR(300) NOT NULL,
    description            TEXT,
    criticality            criticality NOT NULL DEFAULT 'medium',
    status                 task_status NOT NULL DEFAULT 'Queued',
    required_skills_vector vector(1024) NOT NULL
);

-- 2.6 Task Assignments (multiple workers per task, unique per worker)
CREATE TABLE IF NOT EXISTS task_assignments (
    id          INT PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
    task_id     INT NOT NULL REFERENCES tasks(id) ON DELETE CASCADE,
    worker_id   INT NOT NULL REFERENCES workers(id) ON DELETE CASCADE,
    assigned_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE task_assignments DROP CONSTRAINT IF EXISTS task_assignments_task_id_key;
DO $$ BEGIN
    ALTER TABLE task_assignments ADD CONSTRAINT task_assignments_task_worker_key UNIQUE (task_id, worker_id);
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

-- 2.7 Performance Evaluations (immutable audit trail)
CREATE TABLE IF NOT EXISTS performance_evaluations (
    id                INT PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
    task_id           INT NOT NULL REFERENCES tasks(id) ON DELETE CASCADE,
    worker_id         INT NOT NULL REFERENCES workers(id) ON DELETE CASCADE,
    skill_position    INT NOT NULL,
    rating            performance_rating NOT NULL,
    criticality       criticality NOT NULL,
    base_points       NUMERIC(3,1) NOT NULL,
    impact            NUMERIC(4,2) NOT NULL,
    previous_level    NUMERIC(4,1) NOT NULL,
    new_level         NUMERIC(4,1) NOT NULL,
    created_at        TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- =========================================================================
-- 2.8 Helper function to pad short vectors to 1024 dimensions
-- =========================================================================
CREATE OR REPLACE FUNCTION util_pad_vector(v float[], target_dim INT DEFAULT 1024)
RETURNS vector
LANGUAGE sql IMMUTABLE PARALLEL SAFE
AS $$
  SELECT (v || array_fill(0.0::float, ARRAY[target_dim - array_length(v, 1)]))::vector(1024)
$$;

-- =========================================================================
-- 3. SEEDING DATA
-- =========================================================================

-- 3.1 Skills Catalogue (12 fixed positions)
INSERT INTO skills_catalogue (id, name, vector_position) OVERRIDING SYSTEM VALUE VALUES
(1,  'JavaScript', 0),
(2,  'jQuery', 1),
(3,  'HTML5', 2),
(4,  'CSS3', 3),
(5,  'Tailwind CSS', 4),
(6,  'React', 5),
(7,  'PageSpeed Insights', 6),
(8,  'Lighthouse CI', 7),
(9,  'C# OOP', 8),
(10, 'ASP.NET Core MVC', 9),
(11, 'SQL Server / Postgres', 10),
(12, 'Web API Desarrollo', 11);

-- 3.2 Projects
INSERT INTO projects (id, name, description_md) OVERRIDING SYSTEM VALUE VALUES
(1, 'SFManagement Core Platform', '# Project Specification\nDevelopment of the main corporate resource scheduling application.'),
(2, 'E-Commerce Platform Refactor', '# E-Commerce Architecture\nMigration of the legacy checkout system to a modern web API with optimization audits.');

-- 3.3 Workers (4 profiles with 1024-dimensional vectors, padded with zeros)
-- Worker 1: Pure Frontend Specialist
INSERT INTO workers (id, name, role, skills_vector) OVERRIDING SYSTEM VALUE VALUES
(1, 'Oriol Frontend', 'Frontend Engineer - Senior', util_pad_vector(ARRAY[10.0, 8.0, 10.0, 8.0, 10.0, 8.0, 2.0, 2.0, 0.0, 0.0, 0.0, 0.0]));
-- Worker 2: Pure Backend Specialist
INSERT INTO workers (id, name, role, skills_vector) OVERRIDING SYSTEM VALUE VALUES
(2, 'Sarah Backend', 'Backend Engineer - Senior', util_pad_vector(ARRAY[0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 2.0, 2.0, 8.0, 10.0, 10.0, 8.0]));
-- Worker 3: Hybrid / Full-Stack Developer
INSERT INTO workers (id, name, role, skills_vector) OVERRIDING SYSTEM VALUE VALUES
(3, 'Alex FullStack', 'Full-Stack Developer', util_pad_vector(ARRAY[8.0, 6.0, 8.0, 6.0, 8.0, 4.0, 4.0, 4.0, 8.0, 8.0, 6.0, 8.0]));
-- Worker 4: Junior Auditor Specialist
INSERT INTO workers (id, name, role, skills_vector) OVERRIDING SYSTEM VALUE VALUES
(4, 'John Performance', 'Junior Performance Auditor', util_pad_vector(ARRAY[4.0, 4.0, 4.0, 4.0, 2.0, 0.0, 8.0, 8.0, 2.0, 2.0, 2.0, 0.0]));

-- 3.4 Project-Worker Allocations
-- Project 1: All 4 developers
INSERT INTO project_workers (project_id, worker_id) VALUES
(1, 1), (1, 2), (1, 3), (1, 4);
-- Project 2: Only Alex and John
INSERT INTO project_workers (project_id, worker_id) VALUES
(2, 3), (2, 4);

-- 3.5 Tasks
-- Task 1 (Project 1): Frontend Critical Bug. Requires JavaScript(8) and HTML5(6).
INSERT INTO tasks (id, project_id, title, description, criticality, status, required_skills_vector) OVERRIDING SYSTEM VALUE VALUES
(1, 1, 'Fix Authentication Button Event Handler', 'Login failure in the core application. When the user clicks the submit button, the form does nothing. Event listener seems to be broken or detached in the landing page layout view.', 'critical', 'Queued', util_pad_vector(ARRAY[8.0, 0.0, 6.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0]));

-- Task 2 (Project 1): Backend Secure Feature. Requires C#(8), MVC(8), SQL(6).
INSERT INTO tasks (id, project_id, title, description, criticality, status, required_skills_vector) OVERRIDING SYSTEM VALUE VALUES
(2, 1, 'Implement Secure Password Hashing Command', 'Develop the manual command handler to intercept user creation requests and securely hash credentials before storing them in PostgreSQL.', 'high', 'InProgress', util_pad_vector(ARRAY[0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 8.0, 8.0, 6.0, 0.0]));

-- Task 3 (Project 2): Performance Optimization. Requires PageSpeed(8) and Lighthouse(8).
INSERT INTO tasks (id, project_id, title, description, criticality, status, required_skills_vector) OVERRIDING SYSTEM VALUE VALUES
(3, 2, 'Optimize Checkout Core Web Vitals', 'The current mobile LCP metrics during checkout are above 3.5 seconds. Implement speculative preloading scripts to drop loading times and secure the conversion funnel.', 'medium', 'Blocked', util_pad_vector(ARRAY[0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 8.0, 8.0, 0.0, 0.0, 0.0, 0.0]));

-- Task 4 (Project 2): UI Refactor. Requires CSS3(4) and Tailwind(4).
INSERT INTO tasks (id, project_id, title, description, criticality, status, required_skills_vector) OVERRIDING SYSTEM VALUE VALUES
(4, 2, 'Migrate Legacy Storefront Footer to Tailwind CSS', 'Replace old custom layout stylesheets in the storefront footer view with utility-first classes to guarantee proper mobile responsiveness and a minimum tactile target of 44x44px.', 'low', 'Finish', util_pad_vector(ARRAY[0.0, 0.0, 0.0, 4.0, 4.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0]));

-- Reset sequences to avoid conflicts with identity columns
ALTER TABLE skills_catalogue ALTER COLUMN id RESTART WITH 13;
ALTER TABLE projects ALTER COLUMN id RESTART WITH 3;
ALTER TABLE workers ALTER COLUMN id RESTART WITH 5;
ALTER TABLE tasks ALTER COLUMN id RESTART WITH 5;

-- =========================================================================
-- 4. STORED PROCEDURES
-- =========================================================================
-- Adds a new skill to the catalogue at the next available vector position.
-- Existing worker/task vectors are already pre-allocated to 1024 dimensions
-- with zeros in all unused positions, so no ALTER TABLE is needed.
CREATE OR REPLACE PROCEDURE sp_add_skill(p_name VARCHAR(100), OUT new_id INT)
LANGUAGE plpgsql
AS $$
DECLARE
    v_next_pos INT;
BEGIN
    SELECT COALESCE(MAX(vector_position), -1) + 1 INTO v_next_pos FROM skills_catalogue;

    IF v_next_pos >= 1024 THEN
        RAISE EXCEPTION 'Vector dimension limit reached (1024). Cannot add more skills.';
    END IF;

    INSERT INTO skills_catalogue (name, vector_position)
    VALUES (p_name, v_next_pos)
    RETURNING id INTO new_id;
END;
$$;
