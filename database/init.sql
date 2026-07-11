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
    CREATE TYPE task_status AS ENUM ('Queued', 'InProgress', 'Blocked', 'Finish', 'Archived');
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;
DO $$ BEGIN
    CREATE TYPE criticality AS ENUM ('low', 'medium', 'high', 'critical');
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

DO $$ BEGIN
    ALTER TYPE task_status ADD VALUE IF NOT EXISTS 'Archived';
EXCEPTION WHEN duplicate_object THEN NULL;
WHEN SQLSTATE '42704' THEN NULL;
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
    rating            DOUBLE PRECISION NOT NULL,
    criticality       criticality NOT NULL,
    base_points       DOUBLE PRECISION NOT NULL,
    impact            DOUBLE PRECISION NOT NULL,
    previous_level    DOUBLE PRECISION NOT NULL,
    new_level         DOUBLE PRECISION NOT NULL,
    created_at        TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

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
(11, 'MySQL', 10),
(12, 'Web API Desarrollo', 11),
(13, 'DevOps', 12);

-- 3.2 Projects
INSERT INTO projects (id, name, description_md) OVERRIDING SYSTEM VALUE VALUES
(1, 'SFManagement Core Platform', '# Project Specification\nDevelopment of the main corporate resource scheduling application.'),
(2, 'E-Commerce Platform Refactor', '# E-Commerce Architecture\nMigration of the legacy checkout system to a modern web API with optimization audits.');

-- 3.3 Workers (4 profiles with 1024-dimensional vectors, padded with zeros)
-- Worker 1: Pure Frontend Specialist
INSERT INTO workers (id, name, role, skills_vector) OVERRIDING SYSTEM VALUE VALUES
(1, 'Oriol Martinez Lopez', 'Frontend Engineer - Senior', util_pad_vector(ARRAY[10.0, 8.0, 10.0, 8.0, 10.0, 8.0, 2.0, 2.0, 0.0, 0.0, 0.0, 0.0]));
-- Worker 2: Pure Backend Specialist
INSERT INTO workers (id, name, role, skills_vector) OVERRIDING SYSTEM VALUE VALUES
(2, 'Sarah Garcia Torres', 'Backend Engineer - Senior', util_pad_vector(ARRAY[0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 2.0, 2.0, 8.0, 10.0, 10.0, 8.0]));
-- Worker 3: Hybrid / Full-Stack Developer
INSERT INTO workers (id, name, role, skills_vector) OVERRIDING SYSTEM VALUE VALUES
(3, 'Alex Rodriguez Fernandez', 'Full-Stack Developer', util_pad_vector(ARRAY[8.0, 6.0, 8.0, 6.0, 8.0, 4.0, 4.0, 4.0, 8.0, 8.0, 6.0, 8.0]));
-- Worker 4: Junior Auditor Specialist
INSERT INTO workers (id, name, role, skills_vector) OVERRIDING SYSTEM VALUE VALUES
(4, 'John Perez Sanchez', 'Junior Performance Auditor', util_pad_vector(ARRAY[4.0, 4.0, 4.0, 4.0, 2.0, 0.0, 8.0, 8.0, 2.0, 2.0, 2.0, 0.0]));
-- Worker 5: DevOps Engineer (adds DevOps skill at position 12 + MySQL skill)
INSERT INTO workers (id, name, role, skills_vector) OVERRIDING SYSTEM VALUE VALUES
(5, 'Maria Castillo Ruiz', 'DevOps Engineer', util_pad_vector(ARRAY[2.0, 0.0, 2.0, 2.0, 0.0, 0.0, 6.0, 6.0, 4.0, 4.0, 8.0, 4.0, 10.0]));
-- Worker 6: QA Automation Engineer
INSERT INTO workers (id, name, role, skills_vector) OVERRIDING SYSTEM VALUE VALUES
(6, 'David Morales Gomez', 'QA Automation Engineer', util_pad_vector(ARRAY[6.0, 4.0, 6.0, 6.0, 4.0, 0.0, 8.0, 8.0, 2.0, 2.0, 4.0, 2.0, 4.0]));

-- 3.4 Project-Worker Allocations
-- Project 1: All 6 developers
INSERT INTO project_workers (project_id, worker_id) VALUES
(1, 1), (1, 2), (1, 3), (1, 4), (1, 5), (1, 6);
-- Project 2: Full-stack, Performance, DevOps and QA
INSERT INTO project_workers (project_id, worker_id) VALUES
(2, 3), (2, 4), (2, 5), (2, 6);

-- 3.5 Tasks
-- Task 1 (Project 1): Frontend Critical Bug. Requires JavaScript(8) and HTML5(6).
INSERT INTO tasks (id, project_id, title, description, criticality, status, required_skills_vector) OVERRIDING SYSTEM VALUE VALUES
(1, 1, 'Fix Authentication Button Event Handler', 'Login failure in the core application. When the user clicks the submit button, the form does nothing. Event listener seems to be broken or detached in the landing page layout view. The form validation library initialises after the DOMContentLoaded event, but the button click handler is registered using an inline onclick attribute that gets overwritten when the SPA router re-renders the partial view. The root cause appears to be a race condition between the Razor partial rendering pipeline and the custom script loader used for component-specific JavaScript bundles. The fix must ensure the event handler is attached using addEventListener within a MutationObserver that watches the login container element, so the binding survives partial page updates without relying on fragile script loading order.', 'critical', 'Queued', util_pad_vector(ARRAY[8.0, 0.0, 6.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0]));

-- Task 2 (Project 1): Backend Secure Feature. Requires C#(8), MVC(8), SQL(6).
INSERT INTO tasks (id, project_id, title, description, criticality, status, required_skills_vector) OVERRIDING SYSTEM VALUE VALUES
(2, 1, 'Implement Secure Password Hashing Command', 'Develop the manual command handler to intercept user creation requests and securely hash credentials before storing them in PostgreSQL. The current implementation passes plaintext passwords from the controller to the ADO.NET command handler without any hashing layer, which poses a critical security risk if the database backup is compromised. The new handler must use PBKDF2 with a per-user salt generated from a cryptographically secure random number generator, enforce a minimum work factor of 600000 iterations, and store the hash in a dedicated column separate from other user metadata. The existing user creation query in the Infrastructure layer must be refactored to include the hashing step before the INSERT statement executes against the workers table.', 'high', 'InProgress', util_pad_vector(ARRAY[0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 8.0, 8.0, 6.0, 0.0]));

-- Task 3 (Project 2): Performance Optimization. Requires PageSpeed(8) and Lighthouse(8).
INSERT INTO tasks (id, project_id, title, description, criticality, status, required_skills_vector) OVERRIDING SYSTEM VALUE VALUES
(3, 2, 'Optimize Checkout Core Web Vitals', 'The current mobile LCP metrics during checkout are above 3.5 seconds. Implement speculative preloading scripts to drop loading times and secure the conversion funnel. The main bottleneck is the checkout summary endpoint which makes four sequential database queries without batching, causing the server response time to spike to 800ms on mobile connections. Preload hints for the critical CSS and the payment form bundle must be injected into the page head using the Link HTTP header. The PostgreSQL query in the GetCheckoutSummaryQueryHandler must be refactored to use a single CTE that joins orders, line_items, shipping_address and payment_methods in one round trip instead of four separate async queries.', 'medium', 'Blocked', util_pad_vector(ARRAY[0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 8.0, 8.0, 0.0, 0.0, 0.0, 0.0]));

-- Task 4 (Project 2): UI Refactor. Requires CSS3(4) and Tailwind(4).
INSERT INTO tasks (id, project_id, title, description, criticality, status, required_skills_vector) OVERRIDING SYSTEM VALUE VALUES
(4, 2, 'Migrate Legacy Storefront Footer to Tailwind CSS', 'Replace old custom layout stylesheets in the storefront footer view with utility-first classes to guarantee proper mobile responsiveness and a minimum tactile target of 44x44px. The current footer uses a table-based layout with inline font-size declarations that fail WCAG 2.1 AA minimum contrast ratios on the social media links. The refactored version must use Tailwind utility classes exclusively, implement a responsive three-column grid that collapses to a single column below 640px breakpoint, and include a dark mode variant using the dark: prefix. All brand colors in the footer must be mapped to the CSS custom properties defined in the root stylesheet so theme switching works without a full page reload.', 'low', 'Finish', util_pad_vector(ARRAY[0.0, 0.0, 0.0, 4.0, 4.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0]));

-- Task 5 (Project 1): Session redirect bug
INSERT INTO tasks (id, project_id, title, description, criticality, status, required_skills_vector) OVERRIDING SYSTEM VALUE VALUES
(5, 1, 'User Session Timeout Not Redirecting to Login Page', 'When the authentication token expires after 20 minutes of inactivity, the application fails to redirect the user to the login page. Instead, a blank white screen is rendered with no error message in the browser console. The session middleware timeout handler in the custom authentication pipeline appears to swallow the redirect exception. The fix requires updating the session validation logic in the custom middleware to catch expiration events and issue a 302 redirect to /Auth/Login preserving the return URL as a query parameter so the user lands back on their intended page after re-authentication.', 'high', 'InProgress', util_pad_vector(ARRAY[8.0, 0.0, 6.0, 4.0, 0.0, 0.0, 0.0, 0.0, 6.0, 8.0, 4.0, 4.0]));

-- Task 6 (Project 1): Safari rendering bug
INSERT INTO tasks (id, project_id, title, description, criticality, status, required_skills_vector) OVERRIDING SYSTEM VALUE VALUES
(6, 1, 'Dashboard Stat Charts Not Rendering in Safari Browser', 'Users on Safari 17+ report that the main dashboard stat charts render as empty containers with no visible content. The SVG-based chart components use modern CSS grid and subgrid features that Safari does not fully support. The fallback rendering path is not triggered because the feature detection check uses an unsupported @supports syntax. Additionally, the chart animation library relies on WebKit-prefixed transform properties that are deprecated. The solution involves switching to a canvas-based fallback renderer for Safari, adding proper @supports feature detection for CSS subgrid, and applying the -webkit- prefixed transform animations only when the WebKit user agent is detected via the navigator object.', 'medium', 'Queued', util_pad_vector(ARRAY[6.0, 6.0, 8.0, 6.0, 8.0, 4.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0]));

-- Task 7 (Project 1): DB connection pool exhaustion
INSERT INTO tasks (id, project_id, title, description, criticality, status, required_skills_vector) OVERRIDING SYSTEM VALUE VALUES
(7, 1, 'Database Connection Pool Exhaustion Under Heavy Traffic Load', 'During peak usage hours the application becomes unresponsive and logs show Npgsql connection pool exhaustion errors. The current pool size is set to the default 100 connections, but long-running reporting queries hold connections open for several seconds without releasing them back to the pool. Several ADO.NET command handlers do not wrap connections in using blocks, causing connections to be released only when the garbage collector runs. The fix requires auditing all command and query handlers in the Infrastructure layer to ensure every NpgsqlConnection is wrapped in a using statement or try-finally block, increasing the MaxPoolSize to 200 in the connection string, and adding a custom retry policy for transient connection failures.', 'critical', 'InProgress', util_pad_vector(ARRAY[0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 8.0, 6.0, 8.0, 6.0]));

-- Task 8 (Project 2): Cart sync failure
INSERT INTO tasks (id, project_id, title, description, criticality, status, required_skills_vector) OVERRIDING SYSTEM VALUE VALUES
(8, 2, 'Shopping Cart Items Disappear After Successful Payment Confirmation', 'Customers report that after completing a payment and being redirected to the order confirmation page, all items in their shopping cart vanish from the order summary. The payment gateway webhook fires correctly and the order is created in the database, but the cart-to-order item migration query in the CheckoutCommandHandler does not join on the correct session identifier. The basket service clears the Redis cache before the migration query completes due to a race condition in the async command pipeline. The fix must ensure the cache is cleared only after a successful database commit, add a distributed lock around the cart migration process, and log the full cart state at each step of the checkout flow for future debugging.', 'critical', 'Queued', util_pad_vector(ARRAY[6.0, 0.0, 4.0, 0.0, 0.0, 4.0, 0.0, 0.0, 8.0, 6.0, 6.0, 8.0]));

-- Task 9 (Project 2): Search duplicates
INSERT INTO tasks (id, project_id, title, description, criticality, status, required_skills_vector) OVERRIDING SYSTEM VALUE VALUES
(9, 2, 'Product Search Returns Duplicate Results When Category Filter Is Active', 'The product search endpoint returns duplicate product entries when the user applies a category filter in combination with a keyword search. The underlying SQL query joins the products table with product_categories using a LEFT JOIN, but the WHERE clause filters on category_id without a DISTINCT or a proper GROUP BY clause. As a result, products assigned to multiple subcategories under the same parent category appear multiple times in the result set. The fix requires rewriting the search query to use a subquery with EXISTS instead of a LEFT JOIN for the category filter, adding a DISTINCT ON (products.id) clause as a safety net, and updating the integration tests to cover multi-category product scenarios.', 'high', 'InProgress', util_pad_vector(ARRAY[0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 6.0, 4.0, 8.0, 4.0]));

-- Task 10 (Project 2): Email template broken
INSERT INTO tasks (id, project_id, title, description, criticality, status, required_skills_vector) OVERRIDING SYSTEM VALUE VALUES
(10, 2, 'Order Confirmation Email Template Shows Broken Layout in Gmail', 'The HTML email template for order confirmations renders with broken tables and misaligned text when viewed in Gmail webmail and the Gmail mobile app. The template uses CSS flexbox and modern CSS grid properties that Gmail strips out during rendering, causing the product listing table to collapse into a single column and the order total section to overflow its container. The email template must be refactored to use only table-based layouts with inline styles, following the MJML-compatible patterns used by the marketing team. All CSS must be inlined using the PreMailer.NET library before sending, and the template should be tested with Litmus or Email on Acid for Gmail, Outlook, and Apple Mail compatibility before deployment.', 'low', 'Queued', util_pad_vector(ARRAY[4.0, 4.0, 6.0, 6.0, 4.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0]));

-- 3.6 Task Assignments (workers assigned by skill affinity)
INSERT INTO task_assignments (task_id, worker_id) VALUES
(2, 1),
(3, 4),
(4, 3),
(5, 3),
(6, 1),
(7, 2),
(8, 3),
(9, 3),
(10, 4);

-- Reset sequences to avoid conflicts with identity columns
ALTER TABLE skills_catalogue ALTER COLUMN id RESTART WITH 14;
ALTER TABLE projects ALTER COLUMN id RESTART WITH 3;
ALTER TABLE workers ALTER COLUMN id RESTART WITH 7;
ALTER TABLE tasks ALTER COLUMN id RESTART WITH 11;

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
