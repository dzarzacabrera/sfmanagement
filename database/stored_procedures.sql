-- =========================================================================
-- SKILL FORGE MANAGEMENT - Stored Procedures
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