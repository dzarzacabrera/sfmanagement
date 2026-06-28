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
CREATE TYPE task_status AS ENUM ('Queued', 'InProgress', 'Blocked', 'Finish');
CREATE TYPE criticality AS ENUM ('low', 'medium', 'high', 'critical');
CREATE TYPE performance_rating AS ENUM ('Poor', 'Average', 'Good', 'Excellent');

-- =========================================================================
-- 2. TABLES
-- =========================================================================

-- 2.1 Global Skills Catalogue (immutable dimension index)
CREATE TABLE skills_catalogue (
    id              INT PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
    name            VARCHAR(100) NOT NULL,
    vector_position INT NOT NULL UNIQUE
);

-- 2.2 Projects
CREATE TABLE projects (
    id             INT PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
    name           VARCHAR(200) NOT NULL,
    description_md TEXT
);

-- 2.3 Workers
CREATE TABLE workers (
    id            INT PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
    name          VARCHAR(200) NOT NULL,
    skills_vector vector(12) NOT NULL
);

-- 2.4 Project-Worker Allocation (scope boundary)
CREATE TABLE project_workers (
    project_id INT NOT NULL REFERENCES projects(id) ON DELETE CASCADE,
    worker_id  INT NOT NULL REFERENCES workers(id) ON DELETE CASCADE,
    PRIMARY KEY (project_id, worker_id)
);

-- 2.5 Tasks (Kanban items)
CREATE TABLE tasks (
    id                     INT PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
    project_id             INT NOT NULL REFERENCES projects(id) ON DELETE CASCADE,
    title                  VARCHAR(300) NOT NULL,
    description            TEXT,
    criticality            criticality NOT NULL DEFAULT 'medium',
    status                 task_status NOT NULL DEFAULT 'Queued',
    required_skills_vector vector(12) NOT NULL
);

-- 2.6 Task Assignments (one worker per task at a time)
CREATE TABLE task_assignments (
    id          INT PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
    task_id     INT NOT NULL REFERENCES tasks(id) ON DELETE CASCADE,
    worker_id   INT NOT NULL REFERENCES workers(id) ON DELETE CASCADE,
    assigned_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (task_id)
);

-- 2.7 Performance Evaluations (immutable audit trail)
CREATE TABLE performance_evaluations (
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

-- 3.3 Workers (4 profiles with 12-dimensional vectors)
-- Worker 1: Pure Frontend Specialist
INSERT INTO workers (id, name, skills_vector) OVERRIDING SYSTEM VALUE VALUES
(1, 'Oriol Frontend', '[10.0, 8.0, 10.0, 8.0, 10.0, 8.0, 2.0, 2.0, 0.0, 0.0, 0.0, 0.0]');
-- Worker 2: Pure Backend Specialist
INSERT INTO workers (id, name, skills_vector) OVERRIDING SYSTEM VALUE VALUES
(2, 'Sarah Backend', '[0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 2.0, 2.0, 8.0, 10.0, 10.0, 8.0]');
-- Worker 3: Hybrid / Full-Stack Developer
INSERT INTO workers (id, name, skills_vector) OVERRIDING SYSTEM VALUE VALUES
(3, 'Alex FullStack', '[8.0, 6.0, 8.0, 6.0, 8.0, 4.0, 4.0, 4.0, 8.0, 8.0, 6.0, 8.0]');
-- Worker 4: Junior Auditor Specialist
INSERT INTO workers (id, name, skills_vector) OVERRIDING SYSTEM VALUE VALUES
(4, 'John Performance', '[4.0, 4.0, 4.0, 4.0, 2.0, 0.0, 8.0, 8.0, 2.0, 2.0, 2.0, 0.0]');

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
(1, 1, 'Fix Authentication Button Event Handler', 'Login failure in the core application. When the user clicks the submit button, the form does nothing. Event listener seems to be broken or detached in the landing page layout view.', 'critical', 'Queued', '[8.0, 0.0, 6.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0]');

-- Task 2 (Project 1): Backend Secure Feature. Requires C#(8), MVC(8), SQL(6).
INSERT INTO tasks (id, project_id, title, description, criticality, status, required_skills_vector) OVERRIDING SYSTEM VALUE VALUES
(2, 1, 'Implement Secure Password Hashing Command', 'Develop the manual command handler to intercept user creation requests and securely hash credentials before storing them in PostgreSQL.', 'high', 'InProgress', '[0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 8.0, 8.0, 6.0, 0.0]');

-- Task 3 (Project 2): Performance Optimization. Requires PageSpeed(8) and Lighthouse(8).
INSERT INTO tasks (id, project_id, title, description, criticality, status, required_skills_vector) OVERRIDING SYSTEM VALUE VALUES
(3, 2, 'Optimize Checkout Core Web Vitals', 'The current mobile LCP metrics during checkout are above 3.5 seconds. Implement speculative preloading scripts to drop loading times and secure the conversion funnel.', 'medium', 'Blocked', '[0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 8.0, 8.0, 0.0, 0.0, 0.0, 0.0]');

-- Task 4 (Project 2): UI Refactor. Requires CSS3(4) and Tailwind(4).
INSERT INTO tasks (id, project_id, title, description, criticality, status, required_skills_vector) OVERRIDING SYSTEM VALUE VALUES
(4, 2, 'Migrate Legacy Storefront Footer to Tailwind CSS', 'Replace old custom layout stylesheets in the storefront footer view with utility-first classes to guarantee proper mobile responsiveness and a minimum tactile target of 44x44px.', 'low', 'Finish', '[0.0, 0.0, 0.0, 4.0, 4.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0]');

-- Reset sequences to avoid conflicts with identity columns
ALTER TABLE skills_catalogue ALTER COLUMN id RESTART WITH 13;
ALTER TABLE projects ALTER COLUMN id RESTART WITH 3;
ALTER TABLE workers ALTER COLUMN id RESTART WITH 5;
ALTER TABLE tasks ALTER COLUMN id RESTART WITH 5;
