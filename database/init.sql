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
    CREATE TYPE task_status AS ENUM ('Queued', 'InProgress', 'InReview', 'Finish', 'Archived');
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

DO $$ BEGIN
    ALTER TYPE task_status RENAME VALUE 'Test' TO 'InReview';
EXCEPTION WHEN OTHERS THEN NULL;
END $$;
DO $$ BEGIN
    CREATE TYPE criticality AS ENUM ('low', 'medium', 'high', 'critical');
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

-- =========================================================================
-- 2. TABLES
-- =========================================================================

-- 2.1 Global Skills Catalogue (immutable dimension index)
CREATE TABLE IF NOT EXISTS skills_catalogue (
    id              INT PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
    name            VARCHAR(100) NOT NULL,
    description     VARCHAR(150) NOT NULL DEFAULT '',
    vector_position INT NOT NULL UNIQUE,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE
);

-- 2.2 Projects
CREATE TABLE IF NOT EXISTS projects (
    id             INT PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
    name           VARCHAR(200) NOT NULL,
    description_md TEXT,
    is_finalized   BOOLEAN NOT NULL DEFAULT FALSE
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
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'task_assignments_task_worker_key'
    ) THEN
        ALTER TABLE task_assignments ADD CONSTRAINT task_assignments_task_worker_key UNIQUE (task_id, worker_id);
    END IF;
END $$;

-- 2.7 Performance Evaluations (immutable audit trail)
CREATE TABLE IF NOT EXISTS performance_evaluations (
    id                INT PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
    task_id           INT REFERENCES tasks(id) ON DELETE SET NULL,
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
INSERT INTO skills_catalogue (id, name, description, vector_position) OVERRIDING SYSTEM VALUE VALUES
(1,  'JavaScript', 'Client-side scripting language for web applications', 0),
(2,  'jQuery', 'Legacy JavaScript library for DOM manipulation', 1),
(3,  'HTML5', 'Markup language for structuring web content', 2),
(4,  'CSS3', 'Stylesheet language for web presentation', 3),
(5,  'Tailwind CSS', 'Utility-first CSS framework for rapid UI development', 4),
(6,  'React', 'Component-based JavaScript library for building user interfaces', 5),
(7,  'PageSpeed Insights', 'Tool for analyzing web page performance metrics', 6),
(8,  'Lighthouse CI', 'Automated auditing tool for performance and accessibility', 7),
(9,  'C# OOP', 'Object-oriented programming with C# language', 8),
(10, 'ASP.NET Core MVC', 'Model-View-Controller framework for web applications', 9),
(11, 'MySQL', 'Relational database management system', 10),
(12, 'Web API Desarrollo', 'Building and designing RESTful web APIs', 11),
(13, 'DevOps', 'CI/CD pipelines and infrastructure automation', 12),
(14, 'Interpersonal Skills', 'Building rapport and trust with clients and stakeholders', 13),
(15, 'Punctuality', 'Respect for client time and meeting deadlines', 14),
(16, 'Active Listening', 'Understanding real client needs through attentive listening', 15),
(17, 'Resilience', 'Handling daily rejection and setbacks in sales', 16),
(18, 'Persuasiveness', 'Closing commercial deals through effective persuasion', 17),
(19, 'Tech-savviness', 'Understanding and explaining web app technology to clients', 18),
(20, 'Product Demo Mastery', 'Delivering engaging live product demonstrations', 19),
(21, 'Data-driven Selling', 'Using metrics and data to convince prospects', 20),
(22, 'Virtual Selling', 'Mastering video-call sales (Zoom, Google Meet)', 21)
ON CONFLICT (id) DO NOTHING;

-- 3.2 Projects
INSERT INTO projects (id, name, description_md) OVERRIDING SYSTEM VALUE VALUES
(1, 'SFManagement Core Platform', E'This project represents the central pillar of our internal operations — a full-featured corporate resource scheduling and workforce management platform. The application handles everything from project creation and worker profile management to real-time task assignment using a vector-based skill matching engine. Built on a modular Clean Architecture with ADO.NET and PostgreSQL (including the pgvector extension for cosine similarity searches), the platform features a Kanban-style dashboard where tasks flow through Queued, In Progress, Test, and Finish states. Performance evaluations feed back into worker skill vectors, creating a continuous improvement loop that refines future recommendations. The system also includes an encrypted ID system for URLs, a complete setup wizard for database initialisation via seed scripts, and a responsive dark-mode UI styled with Tailwind CSS.'),
(2, 'E-Commerce Platform Refactor', E'A comprehensive modernisation initiative aimed at migrating a legacy monolithic checkout system to a contemporary web API architecture with integrated performance optimisation audits. The project encompasses rewriting the entire payment processing pipeline — from cart management and session handling through to order confirmation and email templating — using ASP.NET Core MVC backed by raw ADO.NET queries against PostgreSQL. Key deliverables include speculative preloading scripts to improve Core Web Vitals, a secure password hashing command handler using PBKDF2 with per-user salts, a refactored product search endpoint that eliminates duplicate results through proper EXISTS subqueries, and a complete storefront footer migration from table-based layouts to utility-first Tailwind CSS classes that meet WCAG 2.1 AA contrast ratios. The checkout flow includes a distributed lock mechanism to prevent race conditions between Redis cache clearing and database commits.'),
(3, 'Digital Hunters', E'A strategic B2B sales acceleration platform designed to manage pipeline workflows, track client relationships, and deliver persuasive product demonstrations that convert enterprise prospects into long-term customers. The platform combines a full-stack development team with specialised sales consultants who use a dedicated set of soft-skill competencies — including Virtual Selling, Product Demo Mastery, Persuasiveness, Active Listening, and Data-driven Selling — to engage leads across multiple channels. The project includes a live video call demonstration system with real-time PostgreSQL data aggregation, a structured follow-up engine for re-engaging stalled leads after budget negotiation impasses, and a comprehensive quarterly sales conversion dashboard that visualises pipeline metrics by industry vertical, deal size, and region using interactive filters powered by raw SQL analytical queries.')
ON CONFLICT (id) DO NOTHING;

-- 3.3 Workers (4 profiles with 1024-dimensional vectors, padded with zeros)
-- Worker 1: Pure Frontend Specialist (decimals added for granularity)
INSERT INTO workers (id, name, role, skills_vector) OVERRIDING SYSTEM VALUE VALUES
(1, 'Oriol Martinez Lopez', 'Frontend Engineer - Senior', util_pad_vector(ARRAY[10.0, 8.5, 10.0, 8.0, 9.5, 8.5, 2.0, 2.0, 0.0, 0.0, 0.0, 0.0]))
ON CONFLICT (id) DO NOTHING;
-- Worker 2: Pure Backend Specialist
INSERT INTO workers (id, name, role, skills_vector) OVERRIDING SYSTEM VALUE VALUES
(2, 'Sarah Garcia Torres', 'Backend Engineer - Senior', util_pad_vector(ARRAY[0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 2.0, 2.0, 8.0, 10.0, 10.0, 8.0]))
ON CONFLICT (id) DO NOTHING;
-- Worker 3: Hybrid / Full-Stack Developer (decimals refined)
INSERT INTO workers (id, name, role, skills_vector) OVERRIDING SYSTEM VALUE VALUES
(3, 'Alex Rodriguez Fernandez', 'Full-Stack Developer', util_pad_vector(ARRAY[8.0, 6.0, 8.0, 6.5, 8.0, 4.5, 4.0, 4.0, 8.5, 8.0, 6.5, 8.0]))
ON CONFLICT (id) DO NOTHING;
-- Worker 4: Junior Auditor Specialist
INSERT INTO workers (id, name, role, skills_vector) OVERRIDING SYSTEM VALUE VALUES
(4, 'John Perez Sanchez', 'Junior Performance Auditor', util_pad_vector(ARRAY[4.0, 4.0, 4.0, 4.0, 2.5, 0.0, 8.0, 8.0, 2.0, 2.0, 2.0, 0.0]))
ON CONFLICT (id) DO NOTHING;
-- Worker 5: DevOps Engineer (adds DevOps skill at position 12 + MySQL skill)
INSERT INTO workers (id, name, role, skills_vector) OVERRIDING SYSTEM VALUE VALUES
(5, 'Maria Castillo Ruiz', 'DevOps Engineer', util_pad_vector(ARRAY[2.5, 0.0, 2.0, 2.0, 0.0, 0.0, 6.5, 6.0, 4.5, 4.0, 8.0, 4.0, 10.0]))
ON CONFLICT (id) DO NOTHING;
-- Worker 6: QA Automation Engineer (decimals added)
INSERT INTO workers (id, name, role, skills_vector) OVERRIDING SYSTEM VALUE VALUES
(6, 'David Morales Gomez', 'QA Automation Engineer', util_pad_vector(ARRAY[6.5, 4.0, 6.0, 6.0, 4.5, 0.0, 8.0, 8.5, 2.5, 2.0, 4.0, 3.0, 4.5]))
ON CONFLICT (id) DO NOTHING;
-- Worker 7: David Zarza Cabrera - Full-Stack Developer (strong frontend 8-10, backend 7.5-9)
INSERT INTO workers (id, name, role, skills_vector) OVERRIDING SYSTEM VALUE VALUES
(7, 'David Zarza Cabrera', 'Full-Stack Developer - Senior', util_pad_vector(ARRAY[9.5, 10.0, 9.0, 8.5, 9.5, 8.0, 9.0, 9.5, 8.5, 9.0, 8.0, 7.5]))
ON CONFLICT (id) DO NOTHING;
-- Worker 8: Carlos Moreno Ruiz - Full-Stack Developer (backend-leaning)
INSERT INTO workers (id, name, role, skills_vector) OVERRIDING SYSTEM VALUE VALUES
(8, 'Carlos Moreno Ruiz', 'Full-Stack Developer', util_pad_vector(ARRAY[6.0, 5.5, 6.0, 5.5, 6.0, 3.0, 5.0, 5.0, 9.5, 9.0, 8.5, 9.0, 8.0]))
ON CONFLICT (id) DO NOTHING;

-- Worker 9: Laura Fernandez Paredes - Sales Account Executive (commercial profile)
INSERT INTO workers (id, name, role, skills_vector) OVERRIDING SYSTEM VALUE VALUES
(9, 'Laura Fernandez Paredes', 'Sales Account Executive', util_pad_vector(ARRAY[0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 9.0, 8.5, 9.5, 8.0, 9.0, 7.0, 8.5, 8.0, 9.0]))
ON CONFLICT (id) DO NOTHING;
-- Worker 10: Marta Gonzalez Ruiz - Senior Sales Consultant (commercial profile, reduced by 1 from original)
INSERT INTO workers (id, name, role, skills_vector) OVERRIDING SYSTEM VALUE VALUES
(10, 'Marta Gonzalez Ruiz', 'Senior Sales Consultant', util_pad_vector(ARRAY[0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 7.0, 8.0, 7.0, 8.5, 7.5, 7.0, 8.0, 7.5, 7.5]))
ON CONFLICT (id) DO NOTHING;
-- Worker 11: Sofia Ramirez Lopez - Junior Sales Representative (shines in Virtual Selling and Interpersonal)
INSERT INTO workers (id, name, role, skills_vector) OVERRIDING SYSTEM VALUE VALUES
(11, 'Sofia Ramirez Lopez', 'Junior Sales Representative', util_pad_vector(ARRAY[0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 8.0, 7.0, 6.0, 3.0, 4.0, 4.0, 3.0, 2.0, 8.0]))
ON CONFLICT (id) DO NOTHING;
-- Worker 12: Pablo Hernandez Torres - Mid Sales Consultant (balanced scores 5.5-7)
INSERT INTO workers (id, name, role, skills_vector) OVERRIDING SYSTEM VALUE VALUES
(12, 'Pablo Hernandez Torres', 'Sales Consultant', util_pad_vector(ARRAY[0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 6.5, 7.0, 6.0, 5.5, 6.5, 5.5, 6.0, 5.5, 7.0]))
ON CONFLICT (id) DO NOTHING;
-- Worker 13: Elena Torres Martin - Senior Sales Advisor (uneven profile: 3 weak areas, 2 strong)
INSERT INTO workers (id, name, role, skills_vector) OVERRIDING SYSTEM VALUE VALUES
(13, 'Elena Torres Martin', 'Senior Sales Advisor', util_pad_vector(ARRAY[0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 6.0, 3.5, 2.5, 2.0, 5.5, 4.0, 5.0, 4.5, 5.0]))
ON CONFLICT (id) DO NOTHING;

-- 3.4 Project-Worker Allocations
-- Project 1: All 6 original developers + 2 new full-stack
INSERT INTO project_workers (project_id, worker_id) VALUES
(1, 1), (1, 2), (1, 3), (1, 4), (1, 5), (1, 6), (1, 7), (1, 8)
ON CONFLICT (project_id, worker_id) DO NOTHING;
-- Project 2: Original team + new full-stack
INSERT INTO project_workers (project_id, worker_id) VALUES
(2, 3), (2, 4), (2, 5), (2, 6), (2, 7), (2, 8);
-- Project 3: Commercial sales team (no full-stack developers)
INSERT INTO project_workers (project_id, worker_id) VALUES
(3, 9), (3, 10), (3, 11), (3, 12), (3, 13);

-- 3.5 Tasks
-- Task 1 (Project 1): Frontend Critical Bug. Requires JavaScript(8.75) and HTML5(6.5).
INSERT INTO tasks (id, project_id, title, description, criticality, status, required_skills_vector) OVERRIDING SYSTEM VALUE VALUES
(1, 1, 'Fix Authentication Button Event Handler', 'Login failure in the core application. When the user clicks the submit button, the form does nothing. Event listener seems to be broken or detached in the landing page layout view. The form validation library initialises after the DOMContentLoaded event, but the button click handler is registered using an inline onclick attribute that gets overwritten when the SPA router re-renders the partial view. The root cause appears to be a race condition between the Razor partial rendering pipeline and the custom script loader used for component-specific JavaScript bundles. The fix must ensure the event handler is attached using addEventListener within a MutationObserver that watches the login container element, so the binding survives partial page updates without relying on fragile script loading order.', 'critical', 'Queued', util_pad_vector(ARRAY[8.75, 0.0, 6.5, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0]));

-- Task 2 (Project 1): Backend Secure Feature. Requires C#(8), MVC(8), SQL(6).
INSERT INTO tasks (id, project_id, title, description, criticality, status, required_skills_vector) OVERRIDING SYSTEM VALUE VALUES
(2, 1, 'Implement Secure Password Hashing Command', 'Develop the manual command handler to intercept user creation requests and securely hash credentials before storing them in PostgreSQL. The current implementation passes plaintext passwords from the controller to the ADO.NET command handler without any hashing layer, which poses a critical security risk if the database backup is compromised. The new handler must use PBKDF2 with a per-user salt generated from a cryptographically secure random number generator, enforce a minimum work factor of 600000 iterations, and store the hash in a dedicated column separate from other user metadata. The existing user creation query in the Infrastructure layer must be refactored to include the hashing step before the INSERT statement executes against the workers table.', 'high', 'InProgress', util_pad_vector(ARRAY[0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 8.0, 8.0, 6.0, 0.0]));

-- Task 3 (Project 2): Performance Optimization. Requires PageSpeed(8.5) and Lighthouse(7.75).
INSERT INTO tasks (id, project_id, title, description, criticality, status, required_skills_vector) OVERRIDING SYSTEM VALUE VALUES
(3, 2, 'Optimize Checkout Core Web Vitals', 'The current mobile LCP metrics during checkout are above 3.5 seconds. Implement speculative preloading scripts to drop loading times and secure the conversion funnel. The main bottleneck is the checkout summary endpoint which makes four sequential database queries without batching, causing the server response time to spike to 800ms on mobile connections. Preload hints for the critical CSS and the payment form bundle must be injected into the page head using the Link HTTP header. The PostgreSQL query in the GetCheckoutSummaryQueryHandler must be refactored to use a single CTE that joins orders, line_items, shipping_address and payment_methods in one round trip instead of four separate async queries.', 'medium', 'Finish', util_pad_vector(ARRAY[0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 8.5, 7.75, 0.0, 0.0, 0.0, 0.0]));

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
(7, 1, 'Database Connection Pool Exhaustion Under Heavy Traffic Load', 'During peak usage hours the application becomes unresponsive and logs show Npgsql connection pool exhaustion errors. The current pool size is set to the default 100 connections, but long-running reporting queries hold connections open for several seconds without releasing them back to the pool. Several ADO.NET command handlers do not wrap connections in using blocks, causing connections to be released only when the garbage collector runs. The fix requires auditing all command and query handlers in the Infrastructure layer to ensure every NpgsqlConnection is wrapped in a using statement or try-finally block, increasing the MaxPoolSize to 200 in the connection string, and adding a custom retry policy for transient connection failures.', 'critical', 'InProgress', util_pad_vector(ARRAY[0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 8.5, 6.0, 8.0, 5.5]));

-- Task 8 (Project 2): Cart sync failure
INSERT INTO tasks (id, project_id, title, description, criticality, status, required_skills_vector) OVERRIDING SYSTEM VALUE VALUES
(8, 2, 'Shopping Cart Items Disappear After Successful Payment Confirmation', 'Customers report that after completing a payment and being redirected to the order confirmation page, all items in their shopping cart vanish from the order summary. The payment gateway webhook fires correctly and the order is created in the database, but the cart-to-order item migration query in the CheckoutCommandHandler does not join on the correct session identifier. The basket service clears the Redis cache before the migration query completes due to a race condition in the async command pipeline. The fix must ensure the cache is cleared only after a successful database commit, add a distributed lock around the cart migration process, and log the full cart state at each step of the checkout flow for future debugging.', 'critical', 'Queued', util_pad_vector(ARRAY[6.0, 0.0, 4.0, 0.0, 0.0, 4.0, 0.0, 0.0, 8.0, 6.0, 6.0, 8.0]));

-- Task 9 (Project 2): Search duplicates
INSERT INTO tasks (id, project_id, title, description, criticality, status, required_skills_vector) OVERRIDING SYSTEM VALUE VALUES
(9, 2, 'Product Search Returns Duplicate Results When Category Filter Is Active', 'The product search endpoint returns duplicate product entries when the user applies a category filter in combination with a keyword search. The underlying SQL query joins the products table with product_categories using a LEFT JOIN, but the WHERE clause filters on category_id without a DISTINCT or a proper GROUP BY clause. As a result, products assigned to multiple subcategories under the same parent category appear multiple times in the result set. The fix requires rewriting the search query to use a subquery with EXISTS instead of a LEFT JOIN for the category filter, adding a DISTINCT ON (products.id) clause as a safety net, and updating the integration tests to cover multi-category product scenarios.', 'high', 'InProgress', util_pad_vector(ARRAY[0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 6.0, 4.0, 8.0, 4.0]));

-- Task 10 (Project 2): Email template broken
INSERT INTO tasks (id, project_id, title, description, criticality, status, required_skills_vector) OVERRIDING SYSTEM VALUE VALUES
(10, 2, 'Order Confirmation Email Template Shows Broken Layout in Gmail', 'The HTML email template for order confirmations renders with broken tables and misaligned text when viewed in Gmail webmail and the Gmail mobile app. The template uses CSS flexbox and modern CSS grid properties that Gmail strips out during rendering, causing the product listing table to collapse into a single column and the order total section to overflow its container. The email template must be refactored to use only table-based layouts with inline styles, following the MJML-compatible patterns used by the marketing team. All CSS must be inlined using the PreMailer.NET library before sending, and the template should be tested with Litmus or Email on Acid for Gmail, Outlook, and Apple Mail compatibility before deployment.', 'low', 'Queued', util_pad_vector(ARRAY[4.0, 4.0, 6.0, 5.5, 4.5, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0]));

-- Task 11 (Project 3): Client pitch via Meet - requires Virtual Selling, Product Demo Mastery, Persuasiveness
INSERT INTO tasks (id, project_id, title, description, criticality, status, required_skills_vector) OVERRIDING SYSTEM VALUE VALUES
(11, 3, 'Deliver Persuasive Product Demo to Enterprise Prospect via Video Call', 'The Digital Hunters team must pitch the platform to a high-value enterprise prospect in the logistics sector. The prospect has requested a live video call demonstration covering the dashboard analytics, pipeline management features, and custom report generation. The sales engineer must showcase Tech-savviness by explaining how the PostgreSQL backend handles real-time data aggregation, while the account executive must apply Virtual Selling techniques to keep the remote audience engaged. The deal is worth $120k ARR and the prospect has indicated they are evaluating two competitors simultaneously. The demo must close with a tailored proposal that uses Data-driven Selling to benchmark the prospect current metrics against projected improvements.', 'critical', 'InProgress', util_pad_vector(ARRAY[0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 7.5, 0.0, 0.0, 0.0, 9.0, 7.0, 9.0, 7.0, 8.5]));
-- Task 12 (Project 3): Follow-up after negotiation stalemate - requires Resilience, Persuasiveness, Active listening
INSERT INTO tasks (id, project_id, title, description, criticality, status, required_skills_vector) OVERRIDING SYSTEM VALUE VALUES
(12, 3, 'Re-engage Stalled Lead After Contract Budget Negotiation Impasse', 'A promising lead from the fintech sector went cold after three rounds of budget negotiations. The client expressed interest but cited pricing concerns. The Digital Hunters team must devise a follow-up strategy that applies Active Listening to address unspoken objections, Resilience to handle potential rejection, and Persuasiveness to reframe the ROI narrative. A tailored proposal with flexible payment terms and a 30-day performance guarantee must be prepared. The approach will be rehearsed via role-play before the actual call.', 'high', 'InReview', util_pad_vector(ARRAY[0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 6.0, 8.0, 9.0, 7.5, 0.0, 0.0, 0.0, 0.0]));
-- Task 13 (Project 3): Data-driven sales report - requires Data-driven Selling, Tech-savviness
INSERT INTO tasks (id, project_id, title, description, criticality, status, required_skills_vector) OVERRIDING SYSTEM VALUE VALUES
(13, 3, 'Build Quarterly Sales Conversion Dashboard From Pipeline Data', 'The sales director needs a comprehensive Power BI-style dashboard that visualises the entire Digital Hunters pipeline: lead sources, conversion rates by stage, average deal size, and win/loss ratios broken down by industry vertical. The dashboard must pull real-time data from the PostgreSQL database using raw SQL queries and display metrics with interactive filters for region and deal size. Laura and Marta will collaborate with the backend team to ensure the data warehouse query extracts accurate historical trends. Punctuality is critical as the board presentation is scheduled for the first Monday of next month.', 'medium', 'Queued', util_pad_vector(ARRAY[0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 6.0, 4.0, 0.0, 0.0, 0.0, 0.0, 8.0, 0.0, 0.0, 0.0, 6.0, 5.0, 8.5, 0.0]));
-- Task 14 (Project 1, Finish): Avatar upload feature. Requires JavaScript(7), HTML5(6.5), Tailwind(6), C#(6), ASP.NET(6).
INSERT INTO tasks (id, project_id, title, description, criticality, status, required_skills_vector) OVERRIDING SYSTEM VALUE VALUES
(14, 1, 'Implement User Avatar Upload with Client-Side Cropping and CDN Distribution', 'Users need the ability to upload a profile avatar from their local machine, crop it to a square aspect ratio using a custom JavaScript cropping widget, and have the final image served via the CDN. The frontend must validate file size (max 5MB), enforce image MIME types (JPEG, PNG, WebP), and preview the selected image before upload. The cropping widget requires a 1:1 aspect ratio lock, a draggable selection area, and a zoom slider. On the backend, the C# command handler must resize the image to 256x256 pixels using the SkiaSharp library, generate a unique filename with a GUID prefix, upload it to Azure Blob Storage, and persist the URL in the users table. The full-stack implementation spans the Razor view, the JavaScript cropper module, the ADO.NET command handler, and the blob storage service.', 'high', 'Finish', util_pad_vector(ARRAY[7.0, 0.0, 6.5, 0.0, 6.0, 0.0, 0.0, 0.0, 6.0, 6.0, 0.0, 0.0]));
-- Task 15 (Project 1, InReview): SSE notification system. Requires JavaScript(7), C#(7.5), Web API(6.5).
INSERT INTO tasks (id, project_id, title, description, criticality, status, required_skills_vector) OVERRIDING SYSTEM VALUE VALUES
(15, 1, 'Refactor Notification System to Use Server-Sent Events Instead of Polling', 'The current notification system polls the server every 15 seconds via a setInterval AJAX call, which generates unnecessary database load and introduces a noticeable delay in delivering real-time alerts to dashboard users. The solution must replace polling with the Server-Sent Events (SSE) standard, establishing a long-lived HTTP connection from the browser to a dedicated SSE endpoint. The backend C# handler must use a Channel<T> to broadcast notification events to all connected clients, with a heartbeat mechanism every 30 seconds to keep the connection alive. The JavaScript EventSource API must reconnect automatically on connection loss with exponential backoff. The migration affects the notification query handler, the layout view script bundle, and the global notification counter component.', 'medium', 'InReview', util_pad_vector(ARRAY[7.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 7.5, 0.0, 0.0, 6.5]));
-- Task 16 (Project 1, Archived): CD pipeline automation. Requires C#(8), ASP.NET(7), Web API(7.5), MySQL(7), DevOps(9).
INSERT INTO tasks (id, project_id, title, description, criticality, status, required_skills_vector) OVERRIDING SYSTEM VALUE VALUES
(16, 1, 'Integrate Continuous Deployment Pipeline with Automated Database Migration and Smoke Tests', 'The current deployment process requires manual SQL script execution and manual smoke testing after each release, causing frequent production incidents when the migration order is incorrect or when a breaking API change is deployed without corresponding frontend updates. The CD pipeline must be fully automated using GitHub Actions, triggered on the main branch push. The pipeline must: (1) run the full test suite including integration tests, (2) build the Docker image and push it to the container registry, (3) execute Flyway-style database migrations against the staging environment using raw SQL scripts sourced from the Infrastructure project, (4) deploy the new container to the staging Kubernetes pod, (5) run a smoke test suite that hits critical API endpoints and verifies the dashboard renders with status 200, and (6) promote the deployment to production only after a manual approval gate. The DevOps engineer must configure the GitHub Actions workflow with matrix builds, while the backend team ensures all database schema changes are reversible with a rollback script and that the smoke tests cover the authentication, project listing, and task assignment flows.', 'high', 'Archived', util_pad_vector(ARRAY[0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 7.0, 6.5, 8.0, 7.0, 7.5, 7.0, 9.0]));

-- Task 17 (Project 1, Finish, pending evaluation): Collaborative project description editor.
INSERT INTO tasks (id, project_id, title, description, criticality, status, required_skills_vector) OVERRIDING SYSTEM VALUE VALUES
(17, 1, 'Implement Real-Time Collaborative Editing for Project Descriptions Using WebSockets and Operational Transform', 'Project managers and team leads need the ability to collaboratively edit project descriptions in real time, similar to Google Docs, without overwriting each other''s changes. The current textarea-based approach causes data loss when two users save simultaneously because the last write wins without any conflict resolution. The solution must use a WebSocket connection managed by a dedicated C# background service that maintains a shared document state per project and applies operational transformation (OT) to merge concurrent edits. The frontend must replace the static textarea with a contenteditable div that captures local insertions and deletions as operations (with position, length, and text payload) and sends them over the WebSocket. Each operation must be timestamped and sequenced with a version counter to detect and resolve conflicts on the server. The backend must persist the final document state to PostgreSQL every 30 seconds and broadcast the confirmed version to all connected clients. The implementation touches the JavaScript editor module, the WebSocket handler in the ASP.NET middleware pipeline, the OT algorithm service in the Application layer, and the ADO.NET document persistence query in Infrastructure.', 'medium', 'Finish', util_pad_vector(ARRAY[9.0, 6.0, 7.0, 0.0, 6.0, 4.0, 0.0, 0.0, 8.0, 7.5, 0.0, 7.0]));

-- 3.6 Task Assignments (workers assigned by skill affinity)
INSERT INTO task_assignments (task_id, worker_id) VALUES
(2, 3),
(2, 5),
(3, 4),
(4, 3),
(5, 3),
(6, 1),
(7, 2),
(8, 3),
(9, 3),
(10, 4),
(11, 9),
(11, 10),
(12, 10),
(13, 9),
(14, 1),
(14, 7),
(15, 8),
(15, 7),
(16, 2),
(16, 3),
(16, 5),
(17, 1),
(17, 3),
(17, 7)
ON CONFLICT (task_id, worker_id) DO NOTHING;

-- 3.7 Performance Evaluations (for finished task 14 - avatar upload)
INSERT INTO performance_evaluations (task_id, worker_id, skill_position, rating, criticality, base_points, impact, previous_level, new_level)
VALUES
-- Oriol (worker 1): Tailwind CSS (pos 4), Good → impact +0.4
(14, 1, 4, 7.5, 'high', 0.25, 0.4, 9.5, 9.9),
-- Oriol (worker 1): CSS3 (pos 3), Good → impact +0.4
(14, 1, 3, 7.5, 'high', 0.25, 0.4, 8.0, 8.4),
-- David Zarza (worker 7): JavaScript (pos 0), Good → impact +0.4
(14, 7, 0, 7.5, 'high', 0.25, 0.4, 9.5, 9.9),
-- David Zarza (worker 7): ASP.NET Core MVC (pos 9), Good → impact +0.4
(14, 7, 9, 7.5, 'high', 0.25, 0.4, 9.0, 9.4),
-- Sarah (worker 2): C# (pos 8), Good → impact +0.4
(16, 2, 8, 7.5, 'high', 0.25, 0.4, 8.0, 8.4),
-- Sarah (worker 2): Web API (pos 11), Good → impact +0.4
(16, 2, 11, 7.5, 'high', 0.25, 0.4, 8.0, 8.4),
-- Alex (worker 3): ASP.NET Core MVC (pos 9), Good → impact +0.4
(16, 3, 9, 7.5, 'high', 0.25, 0.4, 8.0, 8.4),
-- Alex (worker 3): PageSpeed (pos 6), Good → impact +0.4
(16, 3, 6, 7.5, 'high', 0.25, 0.4, 4.0, 4.4),
-- Maria (worker 5): DevOps (pos 12), Excellent → clamped at 10.0
(16, 5, 12, 10.0, 'high', 0.5, 0.0, 10.0, 10.0),
-- Maria (worker 5): MySQL (pos 10), Good → impact +0.4
(16, 5, 10, 7.5, 'high', 0.25, 0.4, 8.0, 8.4);

-- Reset sequences to avoid conflicts with identity columns
ALTER TABLE skills_catalogue ALTER COLUMN id RESTART WITH 23;
ALTER TABLE projects ALTER COLUMN id RESTART WITH 4;
ALTER TABLE workers ALTER COLUMN id RESTART WITH 14;
ALTER TABLE tasks ALTER COLUMN id RESTART WITH 18;

-- =========================================================================
-- 4. STORED PROCEDURES
-- =========================================================================
-- Adds a new skill to the catalogue at the next available vector position.
-- Existing worker/task vectors are already pre-allocated to 1024 dimensions
-- with zeros in all unused positions, so no ALTER TABLE is needed.
CREATE OR REPLACE PROCEDURE sp_add_skill(p_name VARCHAR(100), p_description VARCHAR(150), OUT new_id INT)
LANGUAGE plpgsql
AS $$
DECLARE
    v_next_pos INT;
BEGIN
    SELECT COALESCE(MAX(vector_position), -1) + 1 INTO v_next_pos FROM skills_catalogue;

    IF v_next_pos >= 1024 THEN
        RAISE EXCEPTION 'Vector dimension limit reached (1024). Cannot add more skills.';
    END IF;

    INSERT INTO skills_catalogue (name, description, vector_position)
    VALUES (p_name, p_description, v_next_pos)
    RETURNING id INTO new_id;
END;
$$;
