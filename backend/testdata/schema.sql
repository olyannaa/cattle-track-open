--
-- PostgreSQL database dump
--

\restrict dt0XcqbFtIseFV8GRvBhDGfeImd6lxrBjIf0RjRdEbdl5g0QaeTga9t1dqBbz9O

-- Dumped from database version 14.23 (Ubuntu 14.23-0ubuntu0.22.04.1)
-- Dumped by pg_dump version 15.18

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: public; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA IF NOT EXISTS public;


--
-- Name: SCHEMA public; Type: COMMENT; Schema: -; Owner: -
--

COMMENT ON SCHEMA public IS 'standard public schema';


--
-- Name: add_group(uuid, text, uuid, text, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.add_group(p_organization_id uuid, p_name text, p_group_type_id uuid, p_description text, p_location text) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO groups (id, organization_id, name, type_id, description, location, created_at)
    VALUES (gen_random_uuid(), p_organization_id, p_name, p_group_type_id, p_description, p_location, NOW());
END;
$$;


--
-- Name: add_group_type(text, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.add_group_type(p_name text, p_organization_id uuid) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO group_types (id, name, created_at, organization_id)
    VALUES (gen_random_uuid(), p_name, NOW(), p_organization_id);
END;
$$;


--
-- Name: add_identification_field(text, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.add_identification_field(p_field_name text, p_organization_id uuid) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO identification_fields (id, field_name, organization_id)
    VALUES (gen_random_uuid(), p_field_name, p_organization_id);
END;
$$;


--
-- Name: add_standard_components(text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.add_standard_components(org_name text) RETURNS void
    LANGUAGE plpgsql
    AS $$
DECLARE
    org_id UUID;
BEGIN
    -- Получаем ID организации по имени
    SELECT id INTO org_id FROM organizations WHERE name = org_name;
    
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Организация с именем "%" не найдена', org_name;
    END IF;

    -- Добавляем стандартные компоненты
    INSERT INTO components (
        organization_id,
        name,
        "NE Maintenance",
        "NE Gain",
        "Crude Protein",
        "Degradable Protein",
        "Crude Fat",
        "ByProduct, % DM",
        "Roughage, % DM",
        "NDF",
        "Forage NDF",
        "Starch",
        "Calcium",
        "Phosphorus",
        "Salt",
        "Potassium",
        "Sulfur",
        "cost"
    ) VALUES
    -- Кукуруза
    (
        org_id,
        'Кукуруза',
        2.15,
        1.45,
        9.4,
        6.8,
        4.7,
        0,
        0,
        12.5,
        0,
        68.2,
        0.03,
        0.28,
        0.02,
        0.37,
        0.1,
        25.50
    ),
    -- Пшеница
    (
        org_id,
        'Пшеница',
        2.05,
        1.38,
        11.8,
        8.5,
        2.2,
        0,
        0,
        14.2,
        0,
        59.8,
        0.05,
        0.32,
        0.01,
        0.42,
        0.12,
        30.00
    ),
    -- Ячмень
    (
        org_id,
        'Ячмень',
        1.98,
        1.32,
        10.3,
        7.8,
        2.4,
        0,
        0,
        18.5,
        0,
        56.4,
        0.06,
        0.35,
        0.02,
        0.48,
        0.14,
        28.75
    ),
    -- Соевый шрот
    (
        org_id,
        'Соевый шрот',
        2.25,
        1.52,
        43.2,
        30.2,
        1.8,
        100,
        0,
        15.8,
        0,
        8.5,
        0.28,
        0.65,
        0.03,
        2.08,
        0.38,
        45.00
    ),
    -- Рыбная мука
    (
        org_id,
        'Рыбная мука',
        2.18,
        1.48,
        61.0,
        18.3,
        9.4,
        100,
        0,
        0,
        0,
        0,
        5.85,
        2.95,
        1.2,
        0.85,
        0.68,
        85.00
    );

    RAISE NOTICE 'Добавлено 5 стандартных компонентов для организации "%"', org_name;
END;
$$;


--
-- Name: assign_ration_to_group(uuid, uuid, uuid, double precision, double precision, double precision); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.assign_ration_to_group(p_group_id uuid, p_ration_id uuid, p_organization_id uuid, p_morning_feeding double precision DEFAULT NULL::double precision, p_day_feeding double precision DEFAULT NULL::double precision, p_night_feeding double precision DEFAULT NULL::double precision) RETURNS uuid
    LANGUAGE plpgsql
    AS $$DECLARE
    v_assignment_id uuid;
    v_existing_record_id uuid;
BEGIN
    -- Проверка обязательных полей
    IF p_group_id IS NULL THEN
        RAISE EXCEPTION 'Group ID cannot be null';
    END IF;
    
    IF p_ration_id IS NULL THEN
        RAISE EXCEPTION 'Ration ID cannot be null';
    END IF;
    
    IF p_organization_id IS NULL THEN
        RAISE EXCEPTION 'Organization ID cannot be null';
    END IF;
    
    -- Проверка существования группы и принадлежности к организации
    IF NOT EXISTS (
        SELECT 1 FROM groups 
        WHERE id = p_group_id AND organization_id = p_organization_id
    ) THEN
        RAISE EXCEPTION 'Group with ID % does not exist or belongs to another organization', p_group_id;
    END IF;
    
    -- Проверка существования рациона и принадлежности к организации
    IF NOT EXISTS (
        SELECT 1 FROM rations 
        WHERE id = p_ration_id AND organization_id = p_organization_id
    ) THEN
        RAISE EXCEPTION 'Ration with ID % does not exist or belongs to another organization', p_ration_id;
    END IF;
    
    -- Проверяем, существует ли уже запись для этой группы
    SELECT id INTO v_existing_record_id 
    FROM group_rations 
    WHERE group_id = p_group_id;
    
    IF v_existing_record_id IS NOT NULL THEN
        -- Обновляем существующую запись
        UPDATE group_rations
        SET 
            ration_id = p_ration_id,
            morning_feeding = p_morning_feeding,
            day_feeding = p_day_feeding,
            night_feeding = p_night_feeding,
            created_at = CURRENT_TIMESTAMP
        WHERE id = v_existing_record_id
        RETURNING id INTO v_assignment_id;
    ELSE
        -- Создаем новую запись
        INSERT INTO group_rations (
            group_id,
            ration_id,
            morning_feeding,
            day_feeding,
            night_feeding
        ) VALUES (
            p_group_id,
            p_ration_id,
            p_morning_feeding,
            p_day_feeding,
            p_night_feeding
        )
        RETURNING id INTO v_assignment_id;
    END IF;
    
    RETURN v_assignment_id;
END;$$;


--
-- Name: authenticate_user(text, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.authenticate_user(p_login_or_phone text, p_password text) RETURNS TABLE(user_id uuid, user_phone character varying, user_login text, user_password text, user_organization uuid, user_role_id uuid, user_role_name text, user_permissions_id uuid[], user_permissions_name text[])
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        u.id AS user_id,
        u.phone AS user_phone,
        u.username AS user_login,
        u.password AS user_password,
        u.organization_id AS user_organization,
        u.role_id AS user_role_id,
        r.role AS user_role_name,
        ARRAY_AGG(p.id) AS user_permissions_id,
        ARRAY_AGG(p.permission) AS user_permissions_name
    FROM 
        users u
    LEFT JOIN 
        roles r ON u.role_id = r.id
    LEFT JOIN 
        roles_permissions rp ON r.id = rp.role_id
    LEFT JOIN 
        permissions p ON rp.permission_id = p.id
    WHERE 
        (u.username = p_login_or_phone OR u.phone = p_login_or_phone)
        AND u.password = p_password
    GROUP BY 
        u.id, u.phone, u.username, u.password, u.organization_id, u.role_id, r.role;
    
    -- If no user found, check if it's because of wrong credentials
    IF NOT FOUND THEN
        IF NOT EXISTS (
            SELECT 1 FROM users 
            WHERE username = p_login_or_phone OR phone = p_login_or_phone
        ) THEN
            RAISE EXCEPTION 'User with login/phone % not found', p_login_or_phone;
        ELSE
            RAISE EXCEPTION 'Invalid password for user %', p_login_or_phone;
        END IF;
    END IF;
END;
$$;


--
-- Name: authenticate_user2(text, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.authenticate_user2(p_login_or_phone text, p_password text) RETURNS TABLE(user_id uuid, user_phone character varying, user_login text, user_password text, user_organization uuid, user_role_id uuid, user_role_name text, user_permissions_id uuid[], user_permissions_name text[])
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        u.id AS user_id,
        u.phone AS user_phone,
        u.username AS user_login,
        u.password AS user_password,
        u.organization_id AS user_organization,
        u.role_id AS user_role_id,
        r.role AS user_role_name,
        CASE 
            WHEN r.id IS NULL THEN ARRAY[]::uuid[] 
            ELSE ARRAY_AGG(p.id) 
        END AS user_permissions_id,
        CASE 
            WHEN r.id IS NULL THEN ARRAY[]::text[] 
            ELSE ARRAY_AGG(p.permission) 
        END AS user_permissions_name
    FROM 
        users u
    LEFT JOIN 
        roles r ON u.role_id = r.id
    LEFT JOIN 
        roles_permissions rp ON r.id = rp.role_id
    LEFT JOIN 
        permissions p ON rp.permission_id = p.id
    WHERE 
        (u.username = p_login_or_phone OR u.phone = p_login_or_phone)
        AND u.password = p_password
    GROUP BY 
        u.id, u.phone, u.username, u.password, u.organization_id, u.role_id, r.role, r.id;
    
    -- If no user found, check if it's because of wrong credentials
    IF NOT FOUND THEN
        IF NOT EXISTS (
            SELECT 1 FROM users 
            WHERE username = p_login_or_phone OR phone = p_login_or_phone
        ) THEN
            RAISE EXCEPTION 'User with login/phone % not found', p_login_or_phone;
        ELSE
            RAISE EXCEPTION 'Invalid password for user %', p_login_or_phone;
        END IF;
    END IF;
END;
$$;


--
-- Name: create_component(uuid, text, double precision, integer, integer, real, integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.create_component(p_organization_id uuid, p_name text, p_cost double precision, p_sv integer, p_sp integer, p_cep real, p_ndk integer) RETURNS uuid
    LANGUAGE plpgsql
    AS $$
DECLARE
    new_component_id UUID;
BEGIN
    -- Проверка обязательных полей
    IF p_organization_id IS NULL THEN
        RAISE EXCEPTION 'Organization ID cannot be null';
    END IF;
    
    IF p_name IS NULL OR p_name = '' THEN
        RAISE EXCEPTION 'Component name cannot be empty';
    END IF;
    
    -- Проверка существования организации
    IF NOT EXISTS (SELECT 1 FROM organizations WHERE id = p_organization_id) THEN
        RAISE EXCEPTION 'Organization with ID % does not exist', p_organization_id;
    END IF;
    
    -- Вставка нового компонента с автоматической генерацией ID
    INSERT INTO components (
        organization_id,
        name,
        cost,
        sv,
        sp,
        cep,
        ndk
    ) VALUES (
        p_organization_id,
        p_name,
        p_cost,
        p_sv,
        p_sp,
        p_cep,
        p_ndk
    )
    RETURNING id INTO new_component_id;
    
    RETURN new_component_id;
END;
$$;


--
-- Name: create_group_ration(uuid, uuid, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.create_group_ration(p_group_id uuid, p_ration_id uuid, p_accounting text DEFAULT NULL::text) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    -- Проверка существования группы
    IF NOT EXISTS (SELECT 1 FROM groups WHERE id = p_group_id) THEN
        RAISE EXCEPTION 'Группа с ID % не найдена', p_group_id;
    END IF;

    -- Проверка существования рациона
    IF NOT EXISTS (SELECT 1 FROM rations WHERE id = p_ration_id) THEN
        RAISE EXCEPTION 'Рацион с ID % не найдена', p_ration_id;
    END IF;

    -- Проверка, что группа и рацион принадлежат одной организации
    IF NOT EXISTS (
        SELECT 1 
        FROM groups g
        JOIN rations r ON g.organization_id = r.organization_id
        WHERE g.id = p_group_id AND r.id = p_ration_id
    ) THEN
        RAISE EXCEPTION 'Группа и рацион должны принадлежать одной организации';
    END IF;

    -- Проверка, что у группы еще нет рациона
    IF EXISTS (SELECT 1 FROM group_rations WHERE group_id = p_group_id) THEN
        RAISE EXCEPTION 'У группы уже есть назначенный рацион. Используйте функцию update_group_ration';
    END IF;

    -- Создаем новую запись
    INSERT INTO group_rations (
        group_id,
        ration_id,
        created_at
    ) VALUES (
        p_group_id,
        p_ration_id,
        CURRENT_TIMESTAMP
    );
END;
$$;


--
-- Name: create_medicine(uuid, text, text, text, text, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.create_medicine(p_organization_id uuid, p_name text, p_substance text, p_drug_elimination_period text, p_shelf_life text, p_factory text) RETURNS uuid
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_id UUID := uuid_generate_v4();
BEGIN
    INSERT INTO medicine (
        id,
        organization_id,
        name,
        substance,
        drug_elimination_period,
        shelf_life,
        factory
    )
    VALUES (
        v_id,
        p_organization_id,
        p_name,
        p_substance,
        p_drug_elimination_period,
        p_shelf_life,
        p_factory
    );

    RETURN v_id;
END;
$$;


--
-- Name: create_organization(text, text, character varying, character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.create_organization(p_name text, p_description text, p_inn character varying, p_ogrn character varying) RETURNS uuid
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_organization_id uuid;
BEGIN
    -- Вставляем новую организацию и возвращаем её ID
    INSERT INTO organizations (
        id, 
        name, 
        description, 
        inn, 
        ogrn, 
        created_at
    ) VALUES (
        gen_random_uuid(), 
        p_name, 
        p_description, 
        p_inn, 
        p_ogrn, 
        NOW()
    )
    RETURNING id INTO v_organization_id;
    
    -- Возвращаем ID созданной организации
    RETURN v_organization_id;
END;
$$;


--
-- Name: create_ration_with_components(uuid, text, text, jsonb); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.create_ration_with_components(p_organization_id uuid, p_name text, p_description text DEFAULT NULL::text, p_components jsonb DEFAULT '[]'::jsonb) RETURNS uuid
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_ration_id uuid;
    v_component_record jsonb;
    v_component_id uuid;
    v_kg double precision;
    v_component_cost double precision;
BEGIN
    -- Validate required fields
    IF p_organization_id IS NULL THEN
        RAISE EXCEPTION 'Organization ID cannot be null';
    END IF;
    
    IF p_name IS NULL OR p_name = '' THEN
        RAISE EXCEPTION 'Ration name cannot be empty';
    END IF;
    
    -- Check organization exists
    IF NOT EXISTS (SELECT 1 FROM organizations WHERE id = p_organization_id) THEN
        RAISE EXCEPTION 'Organization with ID % does not exist', p_organization_id;
    END IF;
    
    -- Create the ration
    INSERT INTO rations (
        organization_id,
        name,
        description
    ) VALUES (
        p_organization_id,
        p_name,
        p_description
    )
    RETURNING id INTO v_ration_id;
    
    -- Process components if provided
    IF p_components IS NOT NULL AND jsonb_array_length(p_components) > 0 THEN
        FOR v_component_record IN SELECT * FROM jsonb_array_elements(p_components)
        LOOP
            -- Extract component data
            v_component_id := (v_component_record->>'component_id')::uuid;
            v_kg := (v_component_record->>'kg')::double precision;
			v_component_cost := (v_component_record->>'cost')::double precision;
            
            -- Validate component exists and belongs to organization
            IF NOT EXISTS (
                SELECT 1 FROM components 
                WHERE id = v_component_id AND organization_id = p_organization_id
            ) THEN
                RAISE EXCEPTION 'Component with ID % does not exist or belongs to another organization', v_component_id;
            END IF;
            
            -- Add component to ration
            INSERT INTO rations_components (
                ration_id,
                component_id,
                kg,
                cost
            ) VALUES (
                v_ration_id,
                v_component_id,
                v_kg,
                v_component_cost
            );
        END LOOP;
    END IF;
    
    RETURN v_ration_id;
END;
$$;


--
-- Name: create_user(uuid, text, text, text, character varying, uuid, character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.create_user(p_organization_id uuid, p_username text, p_password text, p_name text, p_phone character varying, p_role_id uuid, p_tg_id character varying) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO users (id, organization_id, username, password, name, phone, role_id, created_at, tg_id)
    VALUES (gen_random_uuid(), p_organization_id, p_username, p_password, p_name, p_phone, p_role_id, NOW(), p_tg_id);
END;
$$;


--
-- Name: delete_calvings_by_cow(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.delete_calvings_by_cow(p_cow_id uuid) RETURNS integer
    LANGUAGE plpgsql
    AS $$
DECLARE
    deleted_count INTEGER;
BEGIN
    DELETE FROM calvings 
    WHERE cow_id = p_cow_id;
    
    GET DIAGNOSTICS deleted_count = ROW_COUNT;
    RETURN deleted_count;
END;
$$;


--
-- Name: delete_component(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.delete_component(p_component_id uuid) RETURNS void
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_usage_count integer;
BEGIN
    -- Проверка обязательных полей
    IF p_component_id IS NULL THEN
        RAISE EXCEPTION 'Component ID cannot be null';
    END IF;
    
    -- Проверка существования компонента
    IF NOT EXISTS (SELECT 1 FROM components WHERE id = p_component_id) THEN
        RAISE EXCEPTION 'Component with ID % does not exist', p_component_id;
    END IF;
    
    -- Проверка использования компонента в других таблицах (пример для таблицы ration_components)
    SELECT COUNT(*) INTO v_usage_count
    FROM rations_components
    WHERE component_id = p_component_id;
    
    IF v_usage_count > 0 THEN
        RAISE EXCEPTION 'Cannot delete component - it is used in % rations', v_usage_count;
    END IF;
    
    -- Удаление компонента
    DELETE FROM components
    WHERE id = p_component_id;
END;
$$;


--
-- Name: delete_daily_action(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.delete_daily_action(action_id uuid) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    DELETE FROM daily_actions 
    WHERE id = action_id;
END;
$$;


--
-- Name: delete_group(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.delete_group(p_group_id uuid) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    DELETE FROM groups
    WHERE id = p_group_id;
END;
$$;


--
-- Name: delete_group_type(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.delete_group_type(p_id uuid) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    DELETE FROM group_types
    WHERE id = p_id;
END;
$$;


--
-- Name: delete_identification_field(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.delete_identification_field(p_id uuid) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    -- Удаляем записи из таблицы animal_identification, где field_id соответствует p_id
    DELETE FROM animal_identification
    WHERE field_id = p_id;

    -- Удаляем запись из таблицы identification_fields, где id соответствует p_id
    DELETE FROM identification_fields
    WHERE id = p_id;
END;
$$;


--
-- Name: delete_insemination_by_cow(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.delete_insemination_by_cow(p_cow_id uuid) RETURNS integer
    LANGUAGE plpgsql
    AS $$
DECLARE
    deleted_count INTEGER;
BEGIN
    DELETE FROM insemination 
    WHERE cow_id = p_cow_id;
    
    GET DIAGNOSTICS deleted_count = ROW_COUNT;
    RETURN deleted_count;
END;
$$;


--
-- Name: delete_medicine(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.delete_medicine(p_id uuid) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
BEGIN
    DELETE FROM medicine
    WHERE id = p_id;

    RETURN FOUND; -- true, если запись была удалена
END;
$$;


--
-- Name: delete_pregnancy_by_cow(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.delete_pregnancy_by_cow(p_cow_id uuid) RETURNS integer
    LANGUAGE plpgsql
    AS $$
DECLARE
    deleted_count INTEGER;
BEGIN
    DELETE FROM pregnancy 
    WHERE cow_id = p_cow_id;
    
    GET DIAGNOSTICS deleted_count = ROW_COUNT;
    RETURN deleted_count;
END;
$$;


--
-- Name: delete_research(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.delete_research(research_id_to_delete uuid) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    DELETE FROM research 
    WHERE id = research_id_to_delete;
    
    IF NOT FOUND THEN
        RAISE NOTICE 'Исследование с ID % не найдено', research_id_to_delete;
    END IF;
END;
$$;


--
-- Name: fill_daily_feeding_records(uuid, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fill_daily_feeding_records(p_organization_id uuid, p_event_date date) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO feeding_record (
        event_date,
        organization_id,
        group_id,
        animal_count,
        group_ration_id,
        total_kg,
        total_kg_for_group,
        feeding_time,
        feeding_coefficient,
        fact_kg,
        mark,
        feeding_mark
    )
    SELECT
        p_event_date,
        p_organization_id,
        g.id,
        COALESCE(g.animal_count, 0),
        gr.ration_id,
        gr.morning_feeding,
        COALESCE(g.animal_count, 0) * gr.morning_feeding,
        'morning',
        gr.morning_feeding,
        NULL,
        NULL,
        NULL
    FROM groups g
    JOIN group_rations gr ON gr.group_id = g.id
    WHERE g.organization_id = p_organization_id
      AND gr.morning_feeding IS NOT NULL
      AND NOT EXISTS (
          SELECT 1 FROM feeding_record fr
          WHERE fr.organization_id = p_organization_id
            AND fr.group_id = g.id
            AND fr.event_date = p_event_date
            AND fr.feeding_time = 'morning'
      );

    INSERT INTO feeding_record (
        event_date,
        organization_id,
        group_id,
        animal_count,
        group_ration_id,
        total_kg,
        total_kg_for_group,
        feeding_time,
        feeding_coefficient,
        fact_kg,
        mark,
        feeding_mark
    )
    SELECT
        p_event_date,
        p_organization_id,
        g.id,
        COALESCE(g.animal_count, 0),
        gr.ration_id,
        gr.day_feeding,
        COALESCE(g.animal_count, 0) * gr.day_feeding,
        'day',
        gr.day_feeding,
        NULL,
        NULL,
        NULL
    FROM groups g
    JOIN group_rations gr ON gr.group_id = g.id
    WHERE g.organization_id = p_organization_id
      AND gr.day_feeding IS NOT NULL
      AND NOT EXISTS (
          SELECT 1 FROM feeding_record fr
          WHERE fr.organization_id = p_organization_id
            AND fr.group_id = g.id
            AND fr.event_date = p_event_date
            AND fr.feeding_time = 'day'
      );

    INSERT INTO feeding_record (
        event_date,
        organization_id,
        group_id,
        animal_count,
        group_ration_id,
        total_kg,
        total_kg_for_group,
        feeding_time,
        feeding_coefficient,
        fact_kg,
        mark,
        feeding_mark
    )
    SELECT
        p_event_date,
        p_organization_id,
        g.id,
        COALESCE(g.animal_count, 0),
        gr.ration_id,
        gr.night_feeding,
        COALESCE(g.animal_count, 0) * gr.night_feeding,
        'night',
        gr.night_feeding,
        NULL,
        NULL,
        NULL
    FROM groups g
    JOIN group_rations gr ON gr.group_id = g.id
    WHERE g.organization_id = p_organization_id
      AND gr.night_feeding IS NOT NULL
      AND NOT EXISTS (
          SELECT 1 FROM feeding_record fr
          WHERE fr.organization_id = p_organization_id
            AND fr.group_id = g.id
            AND fr.event_date = p_event_date
            AND fr.feeding_time = 'night'
      );
END;
$$;


--
-- Name: get_actions_by_organization_and_type(uuid, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_actions_by_organization_and_type(p_organization_id uuid, p_action_type text) RETURNS TABLE(action_id uuid, animal_id uuid, animal_tag_number text, action_type text, action_subtype text, action_date date, performed_by text, action_result text, action_medicine text, action_dose text, action_notes text, next_action_date date, old_group_id uuid, new_group_id uuid, old_group_name text, new_group_name text, old_type text, new_type text, created_at timestamp without time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        da.id AS action_id,
        da.animal_id,
        a.tag_number AS animal_tag_number,
        da.action_type,
        da.action_subtype,
        da.date AS action_date,
        da.performed_by,
        da.result AS action_result,
        da.medicine AS action_medicine,
        da.dose AS action_dose,
        da.notes AS action_notes,
        da.next_action_date,
        da.old_group_id,
        da.new_group_id,
        og.name AS old_group_name,
        ng.name AS new_group_name,
        da.old_type, 
        da.new_type,
        da.created_at
    FROM 
        daily_actions da
    JOIN 
        animals a ON da.animal_id = a.id
    LEFT JOIN
        groups og ON da.old_group_id = og.id AND og.organization_id = p_organization_id
    LEFT JOIN
        groups ng ON da.new_group_id = ng.id AND ng.organization_id = p_organization_id
    WHERE 
        a.organization_id = p_organization_id
        AND da.action_type = p_action_type
    ORDER BY 
        da.date DESC, 
        da.created_at DESC;
END;
$$;


--
-- Name: get_active_animals(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_active_animals(p_organization_id uuid) RETURNS TABLE(animal_id uuid, tag_number text, group_name text, status text, type text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        a.id AS animal_id,
        a.tag_number,
        g.name,
        a.status,
        a.type
    FROM 
        animals a
    JOIN 
        groups g ON a.group_id = g.id
    WHERE 
        a.organization_id = p_organization_id AND 
        a.status = 'Активное';
END;
$$;


--
-- Name: get_all_breeds(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_all_breeds() RETURNS TABLE(breed_id uuid, breed_name text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT id, name
    FROM breeds       ;
END;
$$;


--
-- Name: get_animal_calvings(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_animal_calvings(input_cow_id uuid) RETURNS TABLE(id uuid, cow_id uuid, calving_date date, complication text, calving_type text, veterinar text, treatments text, pathology text, calf_id uuid, calf_tag_number text, insemination_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        c.id,
        c.cow_id,
        c.date AS calving_date,
        c.complication,
        c.type AS calving_type,
        c.veterinar,
        c.treatments,
        c.pathology,
        c.calf_id,
        calf.tag_number AS calf_tag_number,
        c.insemination_id
    FROM 
        calvings c
    LEFT JOIN
        animals calf ON c.calf_id = calf.id
    WHERE 
        c.cow_id = input_cow_id
    ORDER BY 
        c.date DESC;  -- Сортировка по дате отела (новые записи сначала)
END;
$$;


--
-- Name: get_animal_children_from_animals(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_animal_children_from_animals(input_mother_id uuid) RETURNS TABLE(id uuid, cow_id uuid, calving_date date, complication text, calving_type text, veterinar text, treatments text, pathology text, calf_id uuid, calf_tag_number text, insemination_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT
        NULL::uuid AS id,                  -- нет записи в calvings
        input_mother_id        AS cow_id,
        a.birth_date           AS calving_date,
        NULL::text             AS complication,
        NULL::text             AS calving_type,
        NULL::text             AS veterinar,
        NULL::text             AS treatments,
        NULL::text             AS pathology,
        a.id                   AS calf_id,
        a.tag_number           AS calf_tag_number,
        ins.id                 AS insemination_id
    FROM animals a
    LEFT JOIN LATERAL (
        -- выбираем последнюю инсеминацию матери, которая произошла не позже даты рождения телёнка
        SELECT i.id
        FROM insemination i
        WHERE i.cow_id = input_mother_id
          AND (
                a.birth_date IS NULL    -- если birth_date неизвестна — разрешаем любую
                OR i.date <= a.birth_date
              )
        ORDER BY i.date DESC
        LIMIT 1
    ) ins ON TRUE
    WHERE a.mother_id = input_mother_id
      -- исключаем тех потомков, для которых уже есть запись в calvings (чтобы не дублировать)
      AND NOT EXISTS (
          SELECT 1 FROM calvings c WHERE c.calf_id = a.id
      )
    ORDER BY a.birth_date DESC NULLS LAST;
END;
$$;


--
-- Name: get_animal_daily_actions(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_animal_daily_actions(input_animal_id uuid) RETURNS TABLE(id uuid, animal_id uuid, action_type text, action_subtype text, action_date date, performed_by text, result text, medicine text, dose text, notes text, next_action_date date, old_group_id uuid, new_group_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        da.id,
        da.animal_id,
        da.action_type,
        da.action_subtype,
        da.date AS action_date,
        da.performed_by,
        da.result,
        da.medicine,
        da.dose,
        da.notes,
        da.next_action_date,
        da.old_group_id,
        da.new_group_id
    FROM 
        daily_actions da
    WHERE 
        da.animal_id = input_animal_id
    ORDER BY 
        da.date DESC;  -- Сортировка по дате (новые записи сначала)
END;
$$;


--
-- Name: get_animal_detail2(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_animal_detail2(p_animal_id uuid) RETURNS TABLE(id uuid, organization_id uuid, tag_number text, type text, breed text, mother_id uuid, mother_tag_number text, father_id jsonb, father_tag_numbers jsonb, status text, group_id uuid, group_name text, origin text, origin_location text, birth_date date, date_of_receipt date, date_of_disposal date, reason_of_disposal text, identification_data jsonb)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    WITH animal_identifications AS (
        SELECT 
            ai.animal_id,
            jsonb_object_agg(f.field_name, ai.value) AS ident_data
        FROM 
            animal_identification ai
        JOIN 
            identification_fields f ON ai.field_id = f.id
        WHERE 
            ai.animal_id = p_animal_id
        GROUP BY ai.animal_id
    ),
    father_data AS (
        SELECT 
            a.id,
            CASE 
                -- Если father_id это массив UUID
                WHEN jsonb_typeof(a.father_id) = 'array' THEN
                    (SELECT jsonb_agg(f.tag_number)
                     FROM animals f
                     WHERE f.id IN (
                         SELECT elem::uuid
                         FROM jsonb_array_elements_text(a.father_id) AS elem
                     ))
                -- Если father_id это объект с массивом fathers
                WHEN jsonb_typeof(a.father_id) = 'object' AND a.father_id ? 'fathers' THEN
                    (SELECT jsonb_agg(f.tag_number)
                     FROM animals f
                     WHERE f.id IN (
                         SELECT (elem->>'id')::uuid
                         FROM jsonb_array_elements(a.father_id->'fathers') AS elem
                     ))
                -- Если father_id это одиночный UUID (как строка)
                WHEN jsonb_typeof(a.father_id) = 'string' THEN
                    (SELECT jsonb_build_array(f.tag_number)
                     FROM animals f
                     WHERE f.id = (a.father_id#>>'{}')::uuid)
                ELSE '[]'::jsonb
            END AS father_tags
        FROM animals a
        WHERE a.id = p_animal_id
    )
    SELECT 
        a.id,
        a.organization_id,
        a.tag_number,
        a.type,
        a.breed,
        a.mother_id,
        mother.tag_number AS mother_tag_number,
        a.father_id,
        fd.father_tags AS father_tag_numbers,
        a.status,
        a.group_id,
        g.name AS group_name,
        a.origin,
        a.origin_location,
        a.birth_date,
        a.date_of_receipt,
        a.date_of_disposal,
        a.reason_of_disposal,
        COALESCE(ai.ident_data, '{}'::jsonb) AS identification_data
    FROM 
        animals a
    LEFT JOIN groups g ON a.group_id = g.id
    LEFT JOIN animals mother ON a.mother_id = mother.id
    LEFT JOIN animal_identifications ai ON a.id = ai.animal_id
    LEFT JOIN father_data fd ON a.id = fd.id
    WHERE a.id = p_animal_id;
END;
$$;


--
-- Name: get_animal_details(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_animal_details(p_animal_id uuid) RETURNS TABLE(id uuid, organization_id uuid, tag_number text, type text, breed text, mother_id uuid, mother_tag_number text, father_id uuid, father_tag_number text, status text, group_id uuid, group_name text, origin text, origin_location text, birth_date date, date_of_receipt date, date_of_disposal date, reason_of_disposal text, identification_data jsonb)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    WITH animal_identifications AS (
        SELECT 
            ai.animal_id,
            jsonb_object_agg(if.field_name, ai.value) AS ident_data
        FROM 
            animal_identification ai
        JOIN 
            identification_fields if ON ai.field_id = if.id
        WHERE 
            ai.animal_id = p_animal_id
        GROUP BY 
            ai.animal_id
    )
    SELECT 
        a.id,
        a.organization_id,
        a.tag_number,
        a.type,
        a.breed,
        a.mother_id,
        mother.tag_number AS mother_tag_number,
        a.father_id,
        father.tag_number AS father_tag_number,
        a.status,
        a.group_id,
        g.name AS group_name,
        a.origin,
        a.origin_location,
        a.birth_date,
        a.date_of_receipt,
        a.date_of_disposal,
        a.reason_of_disposal,
        COALESCE(ai.ident_data, '{}'::jsonb) AS identification_data
    FROM 
        animals a
    LEFT JOIN 
        groups g ON a.group_id = g.id
    LEFT JOIN
        animals mother ON a.mother_id = mother.id
    LEFT JOIN
        animals father ON a.father_id = father.id
    LEFT JOIN
        animal_identifications ai ON a.id = ai.animal_id
    WHERE 
        a.id = p_animal_id;
END;
$$;


--
-- Name: get_animal_details2(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_animal_details2(p_animal_id uuid) RETURNS TABLE(id uuid, organization_id uuid, tag_number text, type text, breed text, mother_id uuid, mother_tag_number text, father_id jsonb, father_tag_numbers jsonb, status text, group_id uuid, group_name text, origin text, origin_location text, birth_date date, date_of_receipt date, date_of_disposal date, reason_of_disposal text, identification_data jsonb)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    WITH animal_identifications AS (
        SELECT 
            ai.animal_id,
            jsonb_object_agg(f.field_name, ai.value) AS ident_data
        FROM 
            animal_identification ai
        JOIN 
            identification_fields f ON ai.field_id = f.id
        WHERE 
            ai.animal_id = p_animal_id
        GROUP BY ai.animal_id
    )
    SELECT 
        a.id,
        a.organization_id,
        a.tag_number,
        a.type,
        a.breed,
        a.mother_id,
        mother.tag_number AS mother_tag_number,
        a.father_id,
        (
            SELECT jsonb_agg(f.tag_number)
            FROM animals f
            WHERE f.id IN (
                SELECT elem::uuid
                FROM jsonb_array_elements_text(a.father_id) AS elem
            )
        ) AS father_tag_numbers,
        a.status,
        a.group_id,
        g.name AS group_name,
        a.origin,
        a.origin_location,
        a.birth_date,
        a.date_of_receipt,
        a.date_of_disposal,
        a.reason_of_disposal,
        COALESCE(ai.ident_data, '{}'::jsonb) AS identification_data
    FROM 
        animals a
    LEFT JOIN groups g ON a.group_id = g.id
    LEFT JOIN animals mother ON a.mother_id = mother.id
    LEFT JOIN animal_identifications ai ON a.id = ai.animal_id
    WHERE a.id = p_animal_id;
END;
$$;


--
-- Name: get_animal_details3(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_animal_details3(p_animal_id uuid) RETURNS TABLE(id uuid, organization_id uuid, tag_number text, type text, breed text, mother_id uuid, mother_tag_number text, father_id uuid, father_tag_numbers jsonb, status text, group_id uuid, group_name text, origin text, origin_location text, birth_date date, date_of_receipt date, date_of_disposal date, reason_of_disposal text, identification_data jsonb)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    WITH animal_identifications AS (
        SELECT 
            ai.animal_id,
            jsonb_object_agg(f.field_name, ai.value) AS ident_data
        FROM 
            animal_identification ai
        JOIN 
            identification_fields f ON ai.field_id = f.id
        WHERE 
            ai.animal_id = p_animal_id
        GROUP BY ai.animal_id
    )
    SELECT 
        a.id,
        a.organization_id,
        a.tag_number,
        a.type,
        a.breed,
        a.mother_id,
        mother.tag_number AS mother_tag_number,
        a.father_id,
        (
            SELECT jsonb_agg(f.tag_number)
            FROM animals_bk f
            WHERE f.id = a.father_id
        ) AS father_tag_numbers,
        a.status,
        a.group_id,
        g.name AS group_name,
        a.origin,
        a.origin_location,
        a.birth_date,
        a.date_of_receipt,
        a.date_of_disposal,
        a.reason_of_disposal,
        COALESCE(ai.ident_data, '{}'::jsonb) AS identification_data
    FROM 
        animals_bk a
    LEFT JOIN groups g ON a.group_id = g.id
    LEFT JOIN animals_bk mother ON a.mother_id = mother.id
    LEFT JOIN animal_identifications ai ON a.id = ai.animal_id
    WHERE a.id = p_animal_id;
END;
$$;


--
-- Name: get_animal_details4(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_animal_details4(p_animal_id uuid) RETURNS TABLE(id uuid, organization_id uuid, tag_number text, type text, breed text, mother_id uuid, mother_tag_number text, father_id jsonb, father_tag_numbers jsonb, status text, group_id uuid, group_name text, origin text, origin_location text, birth_date date, date_of_receipt date, date_of_disposal date, reason_of_disposal text, identification_data jsonb)
    LANGUAGE plpgsql
    AS $$BEGIN
    RETURN QUERY
    WITH animal_identifications AS (
        SELECT 
            ai.animal_id,
            jsonb_object_agg(f.field_name, ai.value) AS ident_data
        FROM 
            animal_identification ai
        JOIN 
            identification_fields f ON ai.field_id = f.id
        WHERE 
            ai.animal_id = p_animal_id
        GROUP BY ai.animal_id
    )
    SELECT 
        a.id,
        a.organization_id,
        a.tag_number,
        a.type,
        a.breed,
        a.mother_id,
        mother.tag_number AS mother_tag_number,
        a.father_id,  -- оставляем как jsonb
        (
            -- Обрабатываем father_id как JSON массив UUID
            SELECT jsonb_agg(f.tag_number)
            FROM animals f
            WHERE f.id IN (
                SELECT elem::uuid
                FROM jsonb_array_elements_text(a.father_id) AS elem  -- Приводим elem к UUID
            )
        ) AS father_tag_numbers,
        a.status,
        a.group_id,
        g.name AS group_name,
        a.origin,
        a.origin_location,
        a.birth_date,
        a.date_of_receipt,
        a.date_of_disposal,
        a.reason_of_disposal,
        COALESCE(ai.ident_data, '{}'::jsonb) AS identification_data
    FROM 
        animals a
    LEFT JOIN groups g ON a.group_id = g.id
    LEFT JOIN animals mother ON a.mother_id = mother.id
    LEFT JOIN animal_identifications ai ON a.id = ai.animal_id
    WHERE a.id = p_animal_id;
END;
$$;


--
-- Name: get_animal_id_by_tag_and_organization(text, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_animal_id_by_tag_and_organization(p_tag_number text, p_org_id uuid) RETURNS uuid
    LANGUAGE plpgsql
    AS $$
DECLARE
    animal_id UUID;
BEGIN
    SELECT a.id INTO animal_id
    FROM animals a
    WHERE a.tag_number = p_tag_number
      AND a.organization_id = p_org_id;

    RETURN animal_id;
END;
$$;


--
-- Name: get_animal_id_by_value(text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_animal_id_by_value(p_value text) RETURNS uuid
    LANGUAGE plpgsql
    AS $$
DECLARE
    result_id uuid;
BEGIN
    -- Выполняем запрос для получения animal_id
    SELECT animal_id INTO result_id
    FROM animal_identification
    WHERE value = p_value;

    -- Возвращаем результат
    RETURN result_id;
END;
$$;


--
-- Name: get_animal_origin_locations(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_animal_origin_locations(p_organization_id uuid) RETURNS TABLE(origin_location text)
    LANGUAGE sql
    AS $$
SELECT DISTINCT origin_location
FROM animals
WHERE organization_id = p_organization_id
  AND origin_location IS NOT NULL
ORDER BY origin_location;
$$;


--
-- Name: get_animal_origins(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_animal_origins(p_organization_id uuid) RETURNS TABLE(origin text)
    LANGUAGE sql
    AS $$
SELECT DISTINCT origin
FROM animals
WHERE organization_id = p_organization_id
  AND origin IS NOT NULL
ORDER BY origin;
$$;


--
-- Name: get_animal_researches(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_animal_researches(p_animal_id uuid) RETURNS TABLE(id uuid, organization_id uuid, animal_id uuid, research_name text, material_type text, collection_date date, collected_by text, research_result text, notes text, created_at timestamp without time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        r.id,
        r.organization_id,
        r.animal_id,
        r.research_name,
        r.material_type,
        r.collection_date,
        r.collected_by,
        r.result AS research_result,
        r.notes,
        r.created_at
    FROM 
        research r
    WHERE 
        r.animal_id = p_animal_id
    ORDER BY 
        r.collection_date DESC,
        r.created_at DESC;
END;
$$;


--
-- Name: get_animal_weights(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_animal_weights(input_animal_id uuid) RETURNS TABLE(id uuid, animal_id uuid, tag_number text, organization_id uuid, birth_date date, age integer, weighing_date date, weight double precision, method text, notes text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        w.id,
        w.animal_id,
        a.tag_number,
        a.organization_id,
        a.birth_date,
        CASE 
            WHEN a.birth_date IS NULL THEN NULL
            ELSE (EXTRACT(YEAR FROM age(current_date, a.birth_date)) * 12 + 
                 EXTRACT(MONTH FROM age(current_date, a.birth_date)))::int
        END AS age,
        w.date AS weighing_date,
        w.weight,
        w.method,
        w.notes
    FROM 
        weights w
    JOIN 
        animals a ON w.animal_id = a.id
    WHERE 
        w.animal_id = input_animal_id
    ORDER BY 
        w.date DESC;  -- Сортировка по дате взвешивания (новые записи сначала)
END;
$$;


--
-- Name: get_animal_weights2(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_animal_weights2(input_animal_id uuid) RETURNS TABLE(id uuid, animal_id uuid, tag_number text, organization_id uuid, birth_date date, age integer, weighing_date date, weight double precision, sup double precision, method text, notes text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    WITH weighted_data AS (
        SELECT 
            w.id AS weight_id,
            w.animal_id,
            a.tag_number,
            a.organization_id,
            a.birth_date,
            w.date AS weighing_date,
            w.weight,
            w.method,
            w.notes,
            LAG(w.date) OVER (PARTITION BY w.animal_id ORDER BY w.date) AS prev_date,
            LAG(w.weight) OVER (PARTITION BY w.animal_id ORDER BY w.date) AS prev_weight
        FROM 
            weights w
        JOIN 
            animals a ON w.animal_id = a.id
        WHERE 
            w.animal_id = input_animal_id
    )
    SELECT 
        weight_id AS id,
        weighted_data.animal_id,
        weighted_data.tag_number,
        weighted_data.organization_id,
        weighted_data.birth_date,
        CASE 
            WHEN weighted_data.birth_date IS NULL THEN NULL
            ELSE (EXTRACT(YEAR FROM age(weighted_data.weighing_date, weighted_data.birth_date)) * 12 + EXTRACT(MONTH FROM age(weighted_data.weighing_date, weighted_data.birth_date)))::int
        END AS age,
        weighted_data.weighing_date,
        weighted_data.weight,
        CASE
            WHEN weighted_data.prev_date IS NOT NULL AND weighted_data.prev_weight IS NOT NULL THEN
                ROUND(((weighted_data.weight - weighted_data.prev_weight) / GREATEST((weighted_data.weighing_date - weighted_data.prev_date), 1))::numeric, 2)::double precision
            ELSE NULL
        END AS sup,
        weighted_data.method,
        CASE
            WHEN weighted_data.prev_date IS NOT NULL AND weighted_data.prev_weight IS NOT NULL THEN
                COALESCE(weighted_data.notes, '') ||
                ROUND(((weighted_data.weight - weighted_data.prev_weight) / GREATEST((weighted_data.weighing_date - weighted_data.prev_date), 1))::numeric, 2) || ' кг/сут'
            ELSE weighted_data.notes
        END AS notes
    FROM 
        weighted_data
    ORDER BY 
        weighted_data.weighing_date DESC;
END;
$$;


--
-- Name: get_animals_by_org_all_types(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_animals_by_org_all_types(p_organization_id uuid) RETURNS TABLE(id uuid, tag_number text, birth_date date, breed text, group_name text, status text, origin text, origin_location text, type text, mother_tag_number text, father_tag_numbers jsonb, last_vaccination_date date)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        a.id,
        a.tag_number,
        a.birth_date,
        a.breed,
        g.name AS group_name,
        a.status,
        a.origin,
        a.origin_location,
        a.type,
        -- мать
        (
            SELECT mother.tag_number
            FROM animals AS mother
            WHERE mother.id = a.mother_id
        ) AS mother_tag_number,

        -- отцы / бык осеменения
        CASE
            -- если есть father_id в animals
            WHEN a.father_id IS NOT NULL THEN
                (
                    SELECT jsonb_agg(father.tag_number)
                    FROM animals AS father
                    WHERE father.id IN (
                        SELECT elem::uuid
                        FROM jsonb_array_elements_text(a.father_id) AS elem
                    )
                )

            -- если father_id пуст, но есть calvings + insemination
            ELSE
                (
                    SELECT jsonb_agg(
                        jsonb_build_object(
                            'bull_name', i.bull_name,
                            'sperm_batch', i.sperm_batch
                        )
                    )
                    FROM calvings c
                    JOIN insemination i ON i.id = c.insemination_id
                    WHERE c.calf_id = a.id
                )
        END AS father_tag_numbers,

        -- последняя вакцинация / обработка
        (
            SELECT da.date
            FROM daily_actions da
            WHERE da.animal_id = a.id
              AND (da.action_type = 'Вакцинации и обработки' or da.action_type = 'Вакцинация')
            ORDER BY da.date DESC
            LIMIT 1
        ) AS last_vaccination_date

    FROM animals a
    LEFT JOIN groups g ON g.id = a.group_id
    WHERE a.organization_id = p_organization_id;
END;
$$;


--
-- Name: get_animals_by_org_and_type(uuid, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_animals_by_org_and_type(p_organization_id uuid, p_type text) RETURNS TABLE(id uuid, tag_number text, birth_date date, breed text, group_name text, status text, origin text, origin_location text, mother_tag_number text, father_tag_number text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
    	a.id,
        a.tag_number,
        a.birth_date,
        a.breed,
        g.name AS group_name,
        a.status,
        a.origin,
        a.origin_location,
        (SELECT mother.tag_number FROM animals AS mother WHERE mother.id = a.mother_id) AS mother_tag_number,
        (SELECT father.tag_number FROM animals AS father WHERE father.id = a.father_id) AS father_tag_number
    FROM 
        animals AS a
    LEFT JOIN 
        groups AS g ON a.group_id = g.id
    WHERE 
        a.organization_id = p_organization_id AND
        a.type = p_type;
END;
$$;


--
-- Name: get_animals_by_org_and_type2(uuid, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_animals_by_org_and_type2(p_organization_id uuid, p_type text) RETURNS TABLE(id uuid, tag_number text, birth_date date, breed text, group_name text, status text, origin text, origin_location text, mother_tag_number text, father_tag_numbers jsonb)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        a.id,
        a.tag_number,
        a.birth_date,
        a.breed,
        g.name AS group_name,
        a.status,
        a.origin,
        a.origin_location,
        (SELECT mother.tag_number 
         FROM animals AS mother 
         WHERE mother.id = a.mother_id) AS mother_tag_number,
        -- JSON-массив отцов
        (
            SELECT jsonb_agg(father.tag_number)
            FROM animals AS father
            WHERE father.id IN (
                SELECT elem::uuid
                FROM jsonb_array_elements_text(a.father_id) AS elem
            )
        ) AS father_tag_numbers
    FROM 
        animals AS a
    LEFT JOIN 
        groups AS g ON a.group_id = g.id
    WHERE 
        a.organization_id = p_organization_id
        AND a.type = p_type;
END;
$$;


--
-- Name: get_animals_with_if_by_organization(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_animals_with_if_by_organization(org_id uuid) RETURNS TABLE(animal_id uuid, tag_number text, type text, breed text, mother_tag_number text, father_tag_number text, status text, group_id uuid, group_name text, origin text, origin_location text, birth_date date, date_of_receipt date, date_of_disposal date, reason_of_disposal text, consumption text, live_weight_at_disposal double precision, last_weigh_date date, last_weight_weight character varying, identification_field_name text, identification_value text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    WITH animal_data AS (
        SELECT 
            a.id,
            a.tag_number,
            a.type,
            a.breed,
            m.tag_number AS mother_tag_number,
            f.tag_number AS father_tag_number,
            a.status,
            a.group_id,
            g.name AS group_name,
            a.origin,
            a.origin_location,
            a.birth_date,
            a.date_of_receipt,
            a.date_of_disposal,
            a.reason_of_disposal,
            a.consumption,
            a.live_weight_at_disposal,
            a.last_weigh_date,
            a.last_weight_weight
        FROM 
            animals a
        LEFT JOIN 
            animals m ON a.mother_id = m.id
        LEFT JOIN 
            animals f ON a.father_id = f.id
        LEFT JOIN 
            groups g ON a.group_id = g.id
        WHERE 
            a.organization_id = org_id
    )
    SELECT 
        ad.id AS animal_id,
        ad.tag_number,
        ad.type,
        ad.breed,
        ad.mother_tag_number,
        ad.father_tag_number,
        ad.status,
        ad.group_id,
        ad.group_name,
        ad.origin,
        ad.origin_location,
        ad.birth_date,
        ad.date_of_receipt,
        ad.date_of_disposal,
        ad.reason_of_disposal,
        ad.consumption,
        ad.live_weight_at_disposal,
        ad.last_weigh_date,
        ad.last_weight_weight,
        if.field_name AS identification_field_name,
        ai.value AS identification_value
    FROM 
        animal_data ad
    LEFT JOIN 
        animal_identification ai ON ad.id = ai.animal_id
    LEFT JOIN 
        identification_fields if ON ai.field_id = if.id
    ORDER BY 
        ad.id, if.field_name;
END;
$$;


--
-- Name: get_animals_with_if_by_organization2(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_animals_with_if_by_organization2(org_id uuid) RETURNS TABLE(animal_id uuid, tag_number text, type text, breed text, mother_tag_number text, father_tag_numbers text, status text, group_id uuid, group_name text, origin text, origin_location text, birth_date date, date_of_receipt date, date_of_disposal date, reason_of_disposal text, consumption text, live_weight_at_disposal double precision, last_weigh_date date, last_weight_weight character varying, identification_field_name text, identification_value text)
    LANGUAGE plpgsql
    AS $$

BEGIN
    RETURN QUERY
    WITH animal_data AS (
        SELECT 
            a.id,
            a.tag_number,
            a.type,
            a.breed,
            m.tag_number AS mother_tag_number,
            -- Получаем список тегов отцов из JSONB массива
            (
                SELECT string_agg(f.tag_number, ', ' ORDER BY f.tag_number)
                FROM jsonb_array_elements_text(
                    CASE 
                        WHEN jsonb_typeof(a.father_id) = 'array' THEN a.father_id
                        ELSE '[]'::jsonb
                    END
                ) AS father_uuid
                LEFT JOIN animals f ON f.id = father_uuid::uuid
                WHERE f.tag_number IS NOT NULL
            ) AS father_tag_numbers,
            a.status,
            a.group_id,
            g.name AS group_name,
            a.origin,
            a.origin_location,
            a.birth_date,
            a.date_of_receipt,
            a.date_of_disposal,
            a.reason_of_disposal,
            a.consumption,
            a.live_weight_at_disposal,
            a.last_weigh_date,
            a.last_weight_weight
        FROM 
            animals a
        LEFT JOIN 
            animals m ON a.mother_id = m.id
        LEFT JOIN 
            groups g ON a.group_id = g.id
        WHERE 
            a.organization_id = org_id
    )
    SELECT 
        ad.id AS animal_id,
        ad.tag_number,
        ad.type,
        ad.breed,
        ad.mother_tag_number,
        ad.father_tag_numbers,  -- переименовано с father_tag_number на father_tag_numbers
        ad.status,
        ad.group_id,
        ad.group_name,
        ad.origin,
        ad.origin_location,
        ad.birth_date,
        ad.date_of_receipt,
        ad.date_of_disposal,
        ad.reason_of_disposal,
        ad.consumption,
        ad.live_weight_at_disposal,
        ad.last_weigh_date,
        ad.last_weight_weight,
        if.field_name AS identification_field_name,
        ai.value AS identification_value
    FROM 
        animal_data ad
    LEFT JOIN 
        animal_identification ai ON ad.id = ai.animal_id
    LEFT JOIN 
        identification_fields if ON ai.field_id = if.id
    ORDER BY 
        ad.id, if.field_name;
END;
$$;


--
-- Name: get_animals_with_reproduction_data(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_animals_with_reproduction_data(p_organization_id uuid) RETURNS TABLE(animal_id uuid, organization_id uuid, tag_number text, animal_type text, animal_status text, birth_date date, is_barren boolean, insemination_id uuid, insemination_date date, insemination_type text, bull_id uuid, pregnancy_id uuid, pregnancy_date date, pregnancy_status text, expected_calving_date date, calving_id uuid, calving_date date, calving_complication text, calving_type text, calf_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        a.id AS animal_id,
        a.organization_id,
        a.tag_number,
        a.type AS animal_type,
        a.status AS animal_status,
        a.birth_date,
        COALESCE(b.is_barren, FALSE) AS is_barren,
        i.id AS insemination_id,
        i.date AS insemination_date,
        i.insemination_type AS insemination_type,
        i.bull_id AS bull_id,
        p.id AS pregnancy_id,
        p.date AS pregnancy_date,
        p.status AS pregnancy_status,
        p.expected_calving_date,
        c.id AS calving_id,
        c.date AS calving_date,
        c.complication AS calving_complication,
        c.type AS calving_type,
        c.calf_id AS calf_id
    FROM 
        animals a
    LEFT JOIN 
        barren b ON a.id = b.animal_id
    LEFT JOIN 
        insemination i ON a.id = i.cow_id
    LEFT JOIN 
        pregnancy p ON i.id = p.insemination_id
    LEFT JOIN 
        calvings c ON i.id = c.insemination_id
    WHERE 
        a.organization_id = p_organization_id
        AND a.status = 'Активное'
        AND (
            a.type = 'Корова' or a.type = 'Нетель'
            OR (a.type = 'Телка' AND age(a.birth_date) > interval '12 months')
        )
    ORDER BY 
        a.tag_number, i.date DESC, p.date DESC, c.date DESC;
END;
$$;


--
-- Name: get_animals_with_reproduction_data2(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_animals_with_reproduction_data2(p_organization_id uuid) RETURNS TABLE(animal_id uuid, organization_id uuid, tag_number text, animal_type text, animal_status text, birth_date date, is_barren boolean, insemination_id uuid, insemination_date date, insemination_type text, bull_id jsonb, bull_tag_numbers jsonb, pregnancy_id uuid, pregnancy_date date, pregnancy_status text, expected_calving_date date, calving_id uuid, calving_date date, calving_complication text, calving_type text, calf_id uuid)
    LANGUAGE plpgsql
    AS $_$
BEGIN
    RETURN QUERY
    SELECT 
        a.id AS animal_id,
        a.organization_id,
        a.tag_number,
        a.type AS animal_type,
        a.status AS animal_status,
        a.birth_date,
        COALESCE(b.is_barren, FALSE) AS is_barren,
        i.id AS insemination_id,
        i.date AS insemination_date,
        i.insemination_type AS insemination_type,
        i.bull_id AS bull_id,

        /* Собираем tag_number всех быков из рассчитанного списка UUID */
        (
            SELECT jsonb_agg(bu.tag_number)
            FROM animals bu
            WHERE bu.id = ANY (COALESCE(bids.bull_ids, ARRAY[]::uuid[]))
        ) AS bull_tag_numbers,

        p.id AS pregnancy_id,
        p.date AS pregnancy_date,
        p.status AS pregnancy_status,
        p.expected_calving_date,
        c.id AS calving_id,
        c.date AS calving_date,
        c.complication AS calving_complication,
        c.type AS calving_type,
        c.calf_id AS calf_id
    FROM animals a
    LEFT JOIN barren b       ON a.id = b.animal_id
    LEFT JOIN insemination i ON a.id = i.cow_id
    LEFT JOIN pregnancy p    ON i.id = p.insemination_id
    LEFT JOIN calvings c     ON i.id = c.insemination_id

    /* Унифицируем bull_id → массив uuid (поддерживаем оба формата jsonb) */
    LEFT JOIN LATERAL (
        SELECT array_agg(DISTINCT id) AS bull_ids
        FROM (
            /* Формат 1: bull_id = ["uuid1","uuid2",...] */
            SELECT elem::uuid AS id
            FROM jsonb_array_elements_text(COALESCE(i.bull_id, '[]'::jsonb)) elem
            WHERE jsonb_typeof(COALESCE(i.bull_id, '[]'::jsonb)) = 'array'

            UNION ALL

            /* Формат 2: bull_id = { "fathers":[ {"id":"uuid", ...}, ... ] } */
            SELECT (fo->>'id')::uuid AS id
            FROM jsonb_array_elements(COALESCE(i.bull_id->'fathers', '[]'::jsonb)) fo
            WHERE jsonb_typeof(COALESCE(i.bull_id, '{}'::jsonb)) = 'object'
              AND (fo ? 'id')
              AND (fo->>'id') ~* '^[0-9a-fA-F-]{36}$'
        ) s
    ) AS bids ON TRUE

    WHERE 
        a.organization_id = p_organization_id
        AND a.status = 'Активное'
        AND (
            a.type = 'Корова' OR a.type = 'Нетель'
            OR (a.type = 'Телка' AND age(a.birth_date) > interval '12 months')
        )
    ORDER BY a.tag_number, i.date DESC, p.date DESC, c.date DESC;
END;
$_$;


--
-- Name: get_animals_with_reproduction_data3(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_animals_with_reproduction_data3(p_organization_id uuid) RETURNS TABLE(animal_id uuid, organization_id uuid, tag_number text, animal_type text, animal_status text, birth_date date, is_barren boolean, insemination_id uuid, insemination_date date, insemination_type text, bull_id jsonb, bull_tag_numbers jsonb, pregnancy_id uuid, pregnancy_date date, pregnancy_status text, expected_calving_date date, calving_id uuid, calving_date date, calving_complication text, calving_type text, calf_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        a.id AS animal_id,
        a.organization_id,
        a.tag_number,
        a.type AS animal_type,
        a.status AS animal_status,
        a.birth_date,
        COALESCE(b.is_barren, FALSE) AS is_barren,
        i.id AS insemination_id,
        i.date AS insemination_date,
        i.insemination_type AS insemination_type,
        
        /* Преобразуем bull_id в jsonb (один бык или массив) */
        CASE 
            WHEN i.bull_id IS NOT NULL THEN 
                jsonb_build_array(i.bull_id::text)
            ELSE '[]'::jsonb
        END AS bull_id,

        /* Получаем тег быка */
        CASE 
            WHEN i.bull_id IS NOT NULL THEN
                jsonb_build_array(bull.tag_number)
            ELSE '[]'::jsonb
        END AS bull_tag_numbers,

        p.id AS pregnancy_id,
        p.date AS pregnancy_date,
        p.status AS pregnancy_status,
        p.expected_calving_date,
        c.id AS calving_id,
        c.date AS calving_date,
        c.complication AS calving_complication,
        c.type AS calving_type,
        c.calf_id AS calf_id
    FROM animals_bk a  -- Используем animals_bk вместо animals
    LEFT JOIN barren b       ON a.id = b.animal_id
    LEFT JOIN insemination i ON a.id = i.cow_id
    LEFT JOIN animals_bk bull ON i.bull_id = bull.id  -- Используем animals_bk для быков
    LEFT JOIN pregnancy p    ON i.id = p.insemination_id
    LEFT JOIN calvings c     ON i.id = c.insemination_id
    WHERE 
        a.organization_id = p_organization_id
        AND a.status = 'Активное'
        AND (
            a.type = 'Корова' OR a.type = 'Нетель'
            OR (a.type = 'Телка' AND age(a.birth_date) > interval '12 months')
        )
    ORDER BY a.tag_number, i.date DESC, p.date DESC, c.date DESC;
END;
$$;


--
-- Name: get_animals_with_reproduction_data4(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_animals_with_reproduction_data4(p_organization_id uuid) RETURNS TABLE(animal_id uuid, organization_id uuid, tag_number text, animal_type text, animal_status text, birth_date date, is_barren boolean, insemination_id uuid, insemination_date date, insemination_type text, bull_id jsonb, bull_tag_numbers jsonb, pregnancy_id uuid, pregnancy_date date, pregnancy_status text, expected_calving_date date, calving_id uuid, calving_date date, calving_complication text, calving_type text, calf_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        a.id AS animal_id,
        a.organization_id,
        a.tag_number,
        a.type AS animal_type,
        a.status AS animal_status,
        a.birth_date,
        COALESCE(b.is_barren, FALSE) AS is_barren,
        i.id AS insemination_id,
        i.date AS insemination_date,
        i.insemination_type AS insemination_type,
        
        /* bull_id уже в формате jsonb, используем как есть */
        COALESCE(i.bull_id, '[]'::jsonb) AS bull_id,

        /* Получаем теги быков из JSON массива */
        CASE 
            WHEN i.bull_id IS NOT NULL AND jsonb_array_length(i.bull_id) > 0 THEN
                (
                    SELECT jsonb_agg(COALESCE(bull_animal.tag_number, 'Неизвестно'))
                    FROM jsonb_array_elements_text(i.bull_id) AS bull_uuid
                    LEFT JOIN animals bull_animal ON bull_animal.id = bull_uuid::uuid
                )
            ELSE '[]'::jsonb
        END AS bull_tag_numbers,

        p.id AS pregnancy_id,
        p.date AS pregnancy_date,
        p.status AS pregnancy_status,
        p.expected_calving_date,
        c.id AS calving_id,
        c.date AS calving_date,
        c.complication AS calving_complication,
        c.type AS calving_type,
        c.calf_id AS calf_id
    FROM animals a
    LEFT JOIN barren b ON a.id = b.animal_id
    LEFT JOIN insemination i ON a.id = i.cow_id
    LEFT JOIN pregnancy p ON i.id = p.insemination_id
    LEFT JOIN calvings c ON i.id = c.insemination_id
    WHERE 
        a.organization_id = p_organization_id
        AND a.status = 'Активное'
        AND (
            a.type = 'Корова' OR a.type = 'Нетель'
            OR (a.type = 'Телка' AND age(a.birth_date) > interval '12 months')
        )
    ORDER BY a.tag_number, i.date DESC, p.date DESC, c.date DESC;
END;
$$;


--
-- Name: get_barren_cows(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_barren_cows(p_organization_id uuid) RETURNS TABLE(animal_id uuid, tag_number text, organization_id uuid, status text, is_barren boolean)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        a.id AS animal_id,
        a.tag_number,
        a.organization_id,
        a.status,
        b.is_barren
    FROM 
        animals a
    JOIN 
        barren b ON a.id = b.animal_id
    WHERE 
        a.organization_id = p_organization_id
    ORDER BY 
        a.tag_number;
END;
$$;


--
-- Name: get_birth_weight_statistics(uuid, date, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_birth_weight_statistics(p_organization_id uuid, p_date_from date, p_date_to date) RETURNS TABLE(sex text, animal_count bigint, avg_weight numeric, min_weight numeric, max_weight numeric, total_animals bigint, ratio numeric)
    LANGUAGE sql
    AS $$
WITH birth_weights AS (
    SELECT
        a.id AS animal_id,
        a.type AS sex,
        w.weight
    FROM animals a
    JOIN weights w
      ON w.animal_id = a.id
     AND w.date = a.birth_date
    WHERE a.organization_id = p_organization_id
      AND a.birth_date BETWEEN p_date_from AND p_date_to
      AND a.type IN ('Бычок', 'Телка')
),
stats AS (
    SELECT
        COUNT(*) AS total_animals
    FROM birth_weights
)
SELECT
    bw.sex,

    COUNT(*)                    AS animal_count,
    ROUND(AVG(bw.weight)::numeric, 2)    AS avg_weight,
    MIN(bw.weight)              AS min_weight,
    MAX(bw.weight)              AS max_weight,

    s.total_animals,
    ROUND(COUNT(*)::numeric / NULLIF(s.total_animals, 0), 4) AS ratio
FROM birth_weights bw
CROSS JOIN stats s
GROUP BY bw.sex, s.total_animals
ORDER BY bw.sex;
$$;


--
-- Name: get_blood_test_statistics(uuid, date, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_blood_test_statistics(p_organization_id uuid, p_date_from date, p_date_to date) RETURNS TABLE(research_id uuid, animal_id uuid, research_name text, collection_date date, result text, total_tests bigint, positive_count bigint, negative_count bigint, positive_ratio numeric, negative_ratio numeric)
    LANGUAGE sql
    AS $$
WITH filtered_research AS (
    SELECT
        r.id,
        r.animal_id,
        r.research_name,
        r.collection_date,
        r.result
    FROM research r
    JOIN animals a ON a.id = r.animal_id
    WHERE a.organization_id = p_organization_id
      AND r.collection_date BETWEEN p_date_from AND p_date_to
),
stats AS (
    SELECT
        research_name,
        COUNT(*) AS total_tests,
        COUNT(*) FILTER (WHERE result = 'true')  AS positive_count,
        COUNT(*) FILTER (WHERE result = 'false') AS negative_count
    FROM filtered_research
    GROUP BY research_name
)
SELECT
    fr.id              AS research_id,
    fr.animal_id,
    fr.research_name,
    fr.collection_date,
    fr.result,

    s.total_tests,
    s.positive_count,
    s.negative_count,
    ROUND(
        s.positive_count::numeric
        / NULLIF(s.total_tests, 0),
        4
    ) AS positive_ratio,
    ROUND(
        s.negative_count::numeric
        / NULLIF(s.total_tests, 0),
        4
    ) AS negative_ratio
FROM filtered_research fr
JOIN stats s
  ON s.research_name = fr.research_name
ORDER BY fr.research_name, fr.collection_date;
$$;


--
-- Name: get_bulls_by_organization(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_bulls_by_organization(p_organization_id uuid) RETURNS TABLE(id uuid, tag_number text, type text, birth_date date, status text, organization_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT a.id, a.tag_number, a.type, a.birth_date, a.status, a.organization_id
    FROM animals a
    WHERE a.type = 'Бык'
      AND a.status = 'Активное'
      AND a.organization_id = p_organization_id 
    ORDER BY a.tag_number;
END;
$$;


--
-- Name: get_bulls_by_organization2(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_bulls_by_organization2(p_organization_id uuid) RETURNS TABLE(id uuid, tag_number text, type text, birth_date date, status text, organization_id uuid)
    LANGUAGE plpgsql
    AS $$

BEGIN
    RETURN QUERY
    SELECT a.id, a.tag_number, a.type, a.birth_date, a.status, a.organization_id
    FROM animals_bk a
    WHERE a.type = 'Бык'
      AND a.status = 'Активное'
      AND a.organization_id = p_organization_id 
    ORDER BY a.tag_number;
END;
$$;


--
-- Name: get_calvings_with_statistics(uuid, date, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_calvings_with_statistics(p_organization_id uuid, p_date_from date, p_date_to date) RETURNS TABLE(calving_id uuid, cow_id uuid, calf_id uuid, calving_date date, calving_type text, total_calvings bigint, live_count bigint, abort_count bigint, stillborn_count bigint, live_ratio numeric, abort_ratio numeric, stillborn_ratio numeric)
    LANGUAGE sql
    AS $$
WITH filtered_calvings AS (
    SELECT
        c.id,
        c.cow_id,
        c.calf_id,
        c.date,
        c.type
    FROM calvings c
    JOIN animals a ON a.id = c.cow_id
    WHERE a.organization_id = p_organization_id
      AND c.date BETWEEN p_date_from AND p_date_to
),
stats AS (
    SELECT
        COUNT(*) AS total_calvings,
        COUNT(*) FILTER (WHERE type = 'Живой') AS live_count,
        COUNT(*) FILTER (WHERE type = 'Аборт') AS abort_count,
        COUNT(*) FILTER (WHERE type = 'Мертворожденный') AS stillborn_count
    FROM filtered_calvings
)
SELECT
    fc.id            AS calving_id,
    fc.cow_id,
    fc.calf_id,
    fc.date          AS calving_date,
    fc.type          AS calving_type,

    s.total_calvings,
    s.live_count,
    s.abort_count,
    s.stillborn_count,

    ROUND(s.live_count::numeric / NULLIF(s.total_calvings, 0), 4)       AS live_ratio,
    ROUND(s.abort_count::numeric / NULLIF(s.total_calvings, 0), 4)      AS abort_ratio,
    ROUND(s.stillborn_count::numeric / NULLIF(s.total_calvings, 0), 4)  AS stillborn_ratio
FROM filtered_calvings fc
CROSS JOIN stats s
ORDER BY fc.date;
$$;


--
-- Name: get_cow_inseminations(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_cow_inseminations(p_cow_id uuid) RETURNS TABLE(id uuid, cow_id uuid, date date, insemination_type text, sperm_batch text, sperm_manufacturer text, bull_id uuid, bull_tag_number text, embryo_id text, embryo_manufacturer text, technician text, notes text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        i.id,
        i.cow_id,
        i.date,
        i.insemination_type,
        i.sperm_batch,
        i.sperm_manufacturer,
        i.bull_id,
        a.tag_number AS bull_tag_number,
        i.embryo_id,
        i.embryo_manufacturer,
        i.technician,
        i.notes
    FROM 
        insemination i
    LEFT JOIN 
        animals a ON i.bull_id = a.id
    WHERE 
        i.cow_id = p_cow_id
    ORDER BY 
        i.date DESC;
END;
$$;


--
-- Name: get_cow_pregnancies(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_cow_pregnancies(p_cow_id uuid) RETURNS TABLE(id uuid, cow_id uuid, check_date date, pregnancy_status text, expected_calving_date date)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        p.id,
        p.cow_id,
        p.date AS check_date,
        p.status AS pregnancy_status,
        p.expected_calving_date
    FROM 
        pregnancy p
    WHERE 
        p.cow_id = p_cow_id
    ORDER BY 
        p.date DESC;
END;
$$;


--
-- Name: get_cows_by_organization(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_cows_by_organization(p_organization_id uuid) RETURNS TABLE(id uuid, tag_number text, type text, birth_date date, status text, organization_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT a.id, a.tag_number, a.type, a.birth_date, a.status, a.organization_id
    FROM animals a
    WHERE (a.type = 'Корова' OR 
           (a.type = 'Телка' AND 
            CURRENT_DATE >= (a.birth_date + INTERVAL '12 months')))
      AND a.status = 'Активное'
      AND a.organization_id = p_organization_id 
    ORDER BY a.tag_number;
END;
$$;


--
-- Name: get_cows_by_organization2(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_cows_by_organization2(p_organization_id uuid) RETURNS TABLE(id uuid, tag_number text, type text, birth_date date, status text, organization_id uuid)
    LANGUAGE plpgsql
    AS $$

BEGIN
    RETURN QUERY
    SELECT a.id, a.tag_number, a.type, a.birth_date, a.status, a.organization_id
    FROM animals a
    WHERE (a.type = 'Корова' OR 
           (a.type = 'Телка' AND 
            CURRENT_DATE >= (a.birth_date + INTERVAL '12 months')))
      AND a.status = 'Активное'
      AND a.organization_id = p_organization_id 
    ORDER BY a.tag_number;
END;
$$;


--
-- Name: get_daily_weight_gain_statistics(uuid, date, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_daily_weight_gain_statistics(p_organization_id uuid, p_date_from date, p_date_to date) RETURNS TABLE(animal_id uuid, weigh_date date, weight double precision, prev_weight double precision, days_diff integer, daily_gain numeric, avg_daily_gain numeric, min_daily_gain numeric, max_daily_gain numeric)
    LANGUAGE sql
    AS $$
WITH ordered_weights AS (
    SELECT
        w.animal_id,
        w.date AS weigh_date,
        w.weight,
        LAG(w.weight) OVER (PARTITION BY w.animal_id ORDER BY w.date) AS prev_weight,
        LAG(w.date)   OVER (PARTITION BY w.animal_id ORDER BY w.date) AS prev_date
    FROM weights w
    JOIN animals a ON a.id = w.animal_id
    WHERE a.organization_id = p_organization_id
),
gains AS (
    SELECT
        animal_id,
        weigh_date,
        weight,
        prev_weight,
        (weigh_date - prev_date) AS days_diff,
        CASE
            WHEN prev_weight IS NOT NULL
             AND weigh_date > prev_date
            THEN (weight - prev_weight)::numeric
                 / (weigh_date - prev_date)
        END AS daily_gain
    FROM ordered_weights
    WHERE weigh_date BETWEEN p_date_from AND p_date_to
),
stats AS (
    SELECT
        ROUND(AVG(daily_gain), 4) AS avg_daily_gain,
        ROUND(MIN(daily_gain), 4) AS min_daily_gain,
        ROUND(MAX(daily_gain), 4) AS max_daily_gain
    FROM gains
    WHERE daily_gain IS NOT NULL
)
SELECT
    g.animal_id,
    g.weigh_date,
    g.weight,
    g.prev_weight,
    g.days_diff,
    g.daily_gain,

    s.avg_daily_gain,
    s.min_daily_gain,
    s.max_daily_gain
FROM gains g
CROSS JOIN stats s
WHERE g.daily_gain IS NOT NULL
ORDER BY g.weigh_date;
$$;


--
-- Name: get_feeding_info_by_date(uuid, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_feeding_info_by_date(org_id uuid, target_date date) RETURNS TABLE(group_id uuid, group_name text, animal_count bigint, total_fact_kg double precision, feeding_details jsonb, data_source text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    -- Проверяем, есть ли записи за указанную дату
    IF EXISTS (SELECT 1 FROM feeding_record WHERE organization_id = org_id AND event_date = target_date LIMIT 1) THEN
        -- Возвращаем данные из feeding_record
        RETURN QUERY
        WITH group_data AS (
		    SELECT
		        fr.group_id,
		        g.name AS group_name,
		        fr.animal_count,
		        SUM(fr.fact_kg) AS total_fact_kg,
		        jsonb_agg(jsonb_build_object(
		            'ration_id', fr.group_ration_id,
		            'ration_name', r.name,
		            'feeding_time', fr.feeding_time,
		            'feeding_coefficient', fr.feeding_coefficient,
		            'fact_kg', fr.fact_kg,
		            'mark', fr.mark,
		            'feeding_mark', fr.feeding_mark
		        )) AS feeding_details
		    FROM
		        feeding_record fr
		    JOIN
		        groups g ON fr.group_id = g.id AND g.organization_id = org_id
		    JOIN
		        rations r ON fr.group_ration_id = r.id
		    WHERE
		        fr.organization_id = org_id
		        AND fr.event_date = target_date
		    GROUP BY
		        fr.group_id,
		        g.name,
		        fr.animal_count
		)
        SELECT
            gd.group_id,
            gd.group_name,
            gd.animal_count,
            gd.total_fact_kg,
            gd.feeding_details,
            'feeding_record' AS data_source
        FROM
            group_data gd;
    ELSE
        -- Возвращаем данные из get_organization_groups_with_stats с NULL для дополнительных полей
        RETURN QUERY
        SELECT
            gws.group_id,
            gws.group_name,
            gws.animal_count,
            NULL::double precision AS total_fact_kg,
            NULL::jsonb AS feeding_details,
            'groups_stats' AS data_source
        FROM
            get_organization_groups_with_stats(org_id) gws;
    END IF;
END;
$$;


--
-- Name: get_feeding_info_by_date2(uuid, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_feeding_info_by_date2(org_id uuid, target_date date) RETURNS TABLE(group_id uuid, group_name text, animal_count bigint, total_fact_kg double precision, total_cost double precision, feeding_details jsonb, data_source text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    -- Проверяем, есть ли записи за указанную дату
    IF EXISTS (SELECT 1 FROM feeding_record WHERE organization_id = org_id AND event_date = target_date LIMIT 1) THEN
        -- Возвращаем данные из feeding_record
        RETURN QUERY
        WITH ration_costs AS (
            SELECT 
                rc.ration_id,
                SUM(rc.kg * COALESCE(rc.cost, 0)) AS cost_per_ration
            FROM 
                rations_components rc
            JOIN 
                rations r ON rc.ration_id = r.id AND r.organization_id = org_id
            GROUP BY 
                rc.ration_id
        ),
        group_data AS (
            SELECT
                fr.group_id,
                g.name AS group_name,
                fr.animal_count,
                SUM(fr.fact_kg) AS total_fact_kg,
                SUM(COALESCE(rc.cost_per_ration, 0) * fr.animal_count) AS total_cost,
                jsonb_agg(jsonb_build_object(
                    'ration_id', fr.group_ration_id,
                    'ration_name', r.name,
                    'feeding_time', fr.feeding_time,
                    'feeding_coefficient', fr.feeding_coefficient,
                    'fact_kg', fr.fact_kg,
                    'mark', fr.mark,
                    'feeding_mark', fr.feeding_mark,
                    'ration_cost', COALESCE(rc.cost_per_ration, 0)
                )) AS feeding_details
            FROM
                feeding_record fr
            JOIN
                groups g ON fr.group_id = g.id AND g.organization_id = org_id
            JOIN
                rations r ON fr.group_ration_id = r.id
            LEFT JOIN
                ration_costs rc ON fr.group_ration_id = rc.ration_id
            WHERE
                fr.organization_id = org_id
                AND fr.event_date = target_date
            GROUP BY
                fr.group_id,
                g.name,
                fr.animal_count
        )
        SELECT
            gd.group_id,
            gd.group_name,
            gd.animal_count,
            gd.total_fact_kg,
            gd.total_cost,
            gd.feeding_details,
            'feeding_record' AS data_source
        FROM
            group_data gd;
    ELSE
        -- Возвращаем данные из get_organization_groups_with_stats с NULL для дополнительных полей
        RETURN QUERY
        SELECT
            gws.group_id,
            gws.group_name,
            gws.animal_count,
            NULL::double precision AS total_fact_kg,
            NULL::double precision AS total_cost,
            NULL::jsonb AS feeding_details,
            'groups_stats' AS data_source
        FROM
            get_organization_groups_with_stats(org_id) gws;
    END IF;
END;
$$;


--
-- Name: get_group_feeding_last_30_days(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_group_feeding_last_30_days(org_id uuid) RETURNS TABLE(event_date date, organization_id uuid, group_id uuid, group_name text, daily_fact_kg double precision, feeding_details jsonb)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    WITH daily_totals AS (
        SELECT 
            fr.event_date,
            fr.organization_id,
            fr.group_id,
            g.name AS group_name,
            SUM(fr.fact_kg) AS daily_fact_kg,
            jsonb_agg(jsonb_build_object(
                'feeding_time', fr.feeding_time,
                'feeding_coefficient', fr.feeding_coefficient,
                'fact_kg', fr.fact_kg,
                'ration_id', fr.group_ration_id,
                'ration_name', r.name
            )) AS feeding_details
        FROM 
            feeding_record fr
        JOIN 
            groups g ON fr.group_id = g.id AND g.organization_id = org_id
        JOIN
            rations r ON fr.group_ration_id = r.id
        WHERE 
            fr.organization_id = org_id
            AND fr.event_date BETWEEN (CURRENT_DATE - INTERVAL '30 days') AND CURRENT_DATE
        GROUP BY
            fr.event_date,
            fr.organization_id,
            fr.group_id,
            g.name
    )
    SELECT 
        dt.event_date,
        dt.organization_id,
        dt.group_id,
        dt.group_name,
        dt.daily_fact_kg,
        dt.feeding_details
    FROM 
        daily_totals dt
    ORDER BY
        dt.group_id,
        dt.event_date;
END;
$$;


--
-- Name: get_group_feeding_stats_last_30_days(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_group_feeding_stats_last_30_days(p_organization_id uuid, p_group_id uuid) RETURNS TABLE(event_date date, organization_id uuid, group_id uuid, group_name text, group_ration_id uuid, group_ration_name text, daily_fact_kg double precision)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    WITH daily_feeding AS (
        SELECT 
            fr.event_date,
            fr.organization_id,
            fr.group_id,
            g.name AS group_name,
            fr.group_ration_id,
            r.name AS group_ration_name,
            SUM(fr.fact_kg) AS daily_fact_kg
        FROM 
            feeding_record fr
        JOIN 
            groups g ON fr.group_id = g.id AND g.organization_id = p_organization_id
        JOIN 
            rations r ON fr.group_ration_id = r.id AND r.organization_id = p_organization_id
        WHERE 
            fr.organization_id = p_organization_id
            AND fr.group_id = p_group_id
            AND fr.event_date BETWEEN (CURRENT_DATE - INTERVAL '30 days') AND CURRENT_DATE
        GROUP BY
            fr.event_date,
            fr.organization_id,
            fr.group_id,
            g.name,
            fr.group_ration_id,
            r.name
    )
    SELECT 
        df.event_date,
        df.organization_id,
        df.group_id,
        df.group_name,
        df.group_ration_id,
        df.group_ration_name,
        df.daily_fact_kg
    FROM 
        daily_feeding df
    ORDER BY
        df.event_date DESC;
END;
$$;


--
-- Name: get_group_id(text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_group_id(p_name text) RETURNS uuid
    LANGUAGE plpgsql
    AS $$
DECLARE
    result_id uuid;
BEGIN
    -- Выполняем запрос для получения id группы
    SELECT id INTO result_id
    FROM groups
    WHERE name = p_name;

    -- Возвращаем результат
    RETURN result_id;
END;
$$;


--
-- Name: get_group_nutrition_last_30_days(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_group_nutrition_last_30_days(p_organization_id uuid, p_group_id uuid) RETURNS TABLE(event_date date, organization_id uuid, group_id uuid, group_name text, group_ration_id uuid, group_ration_name text, total_sv double precision, total_sp double precision, total_cep double precision, total_ndk double precision)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    WITH ration_nutrients AS (
        SELECT 
            rc.ration_id,
            SUM(rc.kg * COALESCE(c.sv, 0)) AS sum_sv,
            SUM(rc.kg * COALESCE(c.sp, 0)) AS sum_sp,
            SUM(rc.kg * COALESCE(c.cep, 0)) AS sum_cep,
            SUM(rc.kg * COALESCE(c.ndk, 0)) AS sum_ndk
        FROM 
            rations_components rc
        JOIN 
            components c ON rc.component_id = c.id AND c.organization_id = p_organization_id
        GROUP BY 
            rc.ration_id
    ),
    daily_nutrition AS (
        SELECT 
            fr.event_date,
            fr.organization_id,
            fr.group_id,
            g.name AS group_name,
            fr.group_ration_id,
            r.name AS group_ration_name,
            fr.animal_count,
            COALESCE(rn.sum_sv, 0) * fr.animal_count AS total_sv,
            COALESCE(rn.sum_sp, 0) * fr.animal_count AS total_sp,
            COALESCE(rn.sum_cep, 0) * fr.animal_count AS total_cep,
            COALESCE(rn.sum_ndk, 0) * fr.animal_count AS total_ndk
        FROM 
            feeding_record fr
        JOIN 
            groups g ON fr.group_id = g.id AND g.organization_id = p_organization_id
        JOIN 
            rations r ON fr.group_ration_id = r.id AND r.organization_id = p_organization_id
        LEFT JOIN 
            ration_nutrients rn ON fr.group_ration_id = rn.ration_id
        WHERE 
            fr.organization_id = p_organization_id
            AND fr.group_id = p_group_id
            AND fr.event_date BETWEEN (CURRENT_DATE - INTERVAL '30 days') AND CURRENT_DATE
    )
    SELECT 
        dn.event_date,
        dn.organization_id,
        dn.group_id,
        dn.group_name,
        dn.group_ration_id,
        dn.group_ration_name,
        dn.total_sv,
        dn.total_sp,
        dn.total_cep,
        dn.total_ndk
    FROM 
        daily_nutrition dn
    ORDER BY
        dn.event_date DESC;
END;
$$;


--
-- Name: get_group_ration_costs_last_30_days(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_group_ration_costs_last_30_days(p_organization_id uuid, p_group_id uuid) RETURNS TABLE(event_date date, organization_id uuid, group_id uuid, group_name text, group_ration_id uuid, group_ration_name text, ration_cost double precision, total_ration_cost double precision)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    WITH daily_feeding AS (
        SELECT 
            fr.event_date,
            fr.organization_id,
            fr.group_id,
            g.name AS group_name,
            fr.group_ration_id,
            r.name AS group_ration_name,
            fr.animal_count,
            -- Расчет стоимости рациона (сумма kg * cost для всех компонентов)
            (SELECT COALESCE(SUM(rc.kg * COALESCE(rc.cost, 0)), 0) 
             FROM rations_components rc 
             WHERE rc.ration_id = fr.group_ration_id) AS ration_cost
        FROM 
            feeding_record fr
        JOIN 
            groups g ON fr.group_id = g.id AND g.organization_id = p_organization_id
        JOIN 
            rations r ON fr.group_ration_id = r.id AND r.organization_id = p_organization_id
        WHERE 
            fr.organization_id = p_organization_id
            AND fr.group_id = p_group_id
            AND fr.event_date BETWEEN (CURRENT_DATE - INTERVAL '30 days') AND CURRENT_DATE
        GROUP BY
            fr.event_date,
            fr.organization_id,
            fr.group_id,
            g.name,
            fr.group_ration_id,
            r.name,
            fr.animal_count
    )
    SELECT 
        df.event_date,
        df.organization_id,
        df.group_id,
        df.group_name,
        df.group_ration_id,
        df.group_ration_name,
        df.ration_cost,
        df.ration_cost * df.animal_count AS total_ration_cost
    FROM 
        daily_feeding df
    ORDER BY
        df.event_date DESC;
END;
$$;


--
-- Name: get_group_ration_costs_last_year(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_group_ration_costs_last_year(p_organization_id uuid, p_group_id uuid) RETURNS TABLE(event_date date, organization_id uuid, group_id uuid, group_name text, group_ration_id uuid, group_ration_name text, ration_cost double precision, total_ration_cost double precision, month_year text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    WITH ration_costs AS (
        SELECT 
            rc.ration_id,
            SUM(rc.kg * COALESCE(rc.cost, 0)) AS cost_per_ration
        FROM 
            rations_components rc
        JOIN 
            rations r ON rc.ration_id = r.id AND r.organization_id = p_organization_id
        GROUP BY 
            rc.ration_id
    ),
    daily_feeding AS (
        SELECT 
            fr.event_date,
            fr.organization_id,
            fr.group_id,
            g.name AS group_name,
            fr.group_ration_id,
            r.name AS group_ration_name,
            fr.animal_count,
            COALESCE(rc.cost_per_ration, 0) AS ration_cost,
            to_char(fr.event_date, 'YYYY-MM') AS month_year
        FROM 
            feeding_record fr
        JOIN 
            groups g ON fr.group_id = g.id AND g.organization_id = p_organization_id
        JOIN 
            rations r ON fr.group_ration_id = r.id AND r.organization_id = p_organization_id
        LEFT JOIN 
            ration_costs rc ON fr.group_ration_id = rc.ration_id
        WHERE 
            fr.organization_id = p_organization_id
            AND fr.group_id = p_group_id
            AND fr.event_date BETWEEN (CURRENT_DATE - INTERVAL '1 year') AND CURRENT_DATE
    )
    SELECT 
        df.event_date,
        df.organization_id,
        df.group_id,
        df.group_name,
        df.group_ration_id,
        df.group_ration_name,
        df.ration_cost,
        df.ration_cost * df.animal_count AS total_ration_cost,
        df.month_year
    FROM 
        daily_feeding df
    ORDER BY
        df.event_date DESC;
END;
$$;


--
-- Name: get_group_ration_costs_last_year_monthly(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_group_ration_costs_last_year_monthly(p_organization_id uuid, p_group_id uuid) RETURNS TABLE(month_year text, organization_id uuid, group_id uuid, group_name text, avg_ration_cost double precision, avg_total_ration_cost double precision, feeding_days_count bigint)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    WITH ration_costs AS (
        SELECT 
            rc.ration_id,
            SUM(rc.kg * COALESCE(rc.cost, 0)) AS cost_per_ration
        FROM 
            rations_components rc
        JOIN 
            rations r ON rc.ration_id = r.id AND r.organization_id = p_organization_id
        GROUP BY 
            rc.ration_id
    ),
    daily_costs AS (
        SELECT 
            fr.event_date,
            fr.organization_id,
            fr.group_id,
            g.name AS group_name,
            COALESCE(rc.cost_per_ration, 0) AS ration_cost,
            fr.animal_count,
            to_char(fr.event_date, 'YYYY-MM') AS month_year
        FROM 
            feeding_record fr
        JOIN 
            groups g ON fr.group_id = g.id AND g.organization_id = p_organization_id
        LEFT JOIN 
            ration_costs rc ON fr.group_ration_id = rc.ration_id
        WHERE 
            fr.organization_id = p_organization_id
            AND fr.group_id = p_group_id
            AND fr.event_date BETWEEN (CURRENT_DATE - INTERVAL '1 year') AND CURRENT_DATE
    ),
    monthly_stats AS (
        SELECT 
            dc.month_year,
            dc.organization_id,
            dc.group_id,
            dc.group_name,
            AVG(dc.ration_cost) AS avg_ration_cost,
            AVG(dc.ration_cost * dc.animal_count) AS avg_total_ration_cost,
            COUNT(DISTINCT dc.event_date) AS feeding_days_count
        FROM 
            daily_costs dc
        GROUP BY
            dc.month_year,
            dc.organization_id,
            dc.group_id,
            dc.group_name
    )
    SELECT 
        ms.month_year,
        ms.organization_id,
        ms.group_id,
        ms.group_name,
        ms.avg_ration_cost,
        ms.avg_total_ration_cost,
        ms.feeding_days_count
    FROM 
        monthly_stats ms
    ORDER BY
        ms.month_year DESC;
END;
$$;


--
-- Name: get_group_types_by_organization(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_group_types_by_organization(p_organization_id uuid) RETURNS TABLE(id uuid, name text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        group_types.id,
        group_types.name
    FROM 
        group_types
    WHERE 
        organization_id = p_organization_id;
END;
$$;


--
-- Name: get_groups(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_groups(org_id uuid) RETURNS TABLE(id uuid, name text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT g.id, g.name
    FROM groups g
    WHERE g.organization_id = org_id;
END;
$$;


--
-- Name: get_groups_by_organization(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_groups_by_organization(p_organization_id uuid) RETURNS TABLE(id uuid, name text, type_id uuid, type_name text, description text, location text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        g.id,
        g.name,
        g.type_id,
        gt.name AS type_name,
        g.description,
        g.location
    FROM 
        groups g
    JOIN 
        group_types gt ON g.type_id = gt.id
    WHERE 
        g.organization_id = p_organization_id;
END;
$$;


--
-- Name: get_groups_with_stats(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_groups_with_stats(p_organization_id uuid) RETURNS TABLE(group_id uuid, group_name text, group_description text, group_location text, active_animals_count bigint, ration_id uuid, ration_name text, ration_description text, created_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        g.id AS group_id,
        g.name AS group_name,
        g.description AS group_description,
        g.location AS group_location,
        COUNT(a.id) FILTER (WHERE a.status = 'Активное') AS active_animals_count,
        gr.ration_id,
        r.name AS ration_name,
        r.description AS ration_description,
        gr.created_at
    FROM 
        groups g
    LEFT JOIN 
        animals a ON g.id = a.group_id AND g.organization_id = a.organization_id
    LEFT JOIN 
        group_rations gr ON g.id = gr.group_id
    LEFT JOIN 
        rations r ON gr.ration_id = r.id
    WHERE 
        g.organization_id = p_organization_id
    GROUP BY 
        g.id, g.name, g.description, g.location, gr.ration_id, r.name, r.description, gr.created_at
    ORDER BY 
        g.name;
END;
$$;


--
-- Name: get_groups_with_stats2(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_groups_with_stats2(p_organization_id uuid) RETURNS TABLE(group_id uuid, group_name text, active_animals_count bigint, morning_feeding double precision, day_feeding double precision, night_feeding double precision, ration_id uuid, ration_name text, ration_cost_per_head double precision, total_ration_cost double precision, sv_per_head integer, sp_per_head integer, cep_per_head double precision, ndk_per_head integer, total_sv integer, total_sp integer, total_cep double precision, total_ndk integer)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    WITH group_stats AS (
        SELECT 
            g.id AS group_id,
            g.name AS group_name,
            COUNT(a.id) FILTER (WHERE a.status = 'Активное') AS active_animals_count,
            gr.morning_feeding,
            gr.day_feeding,
            gr.night_feeding,
            gr.ration_id
        FROM 
            groups g
        LEFT JOIN 
            animals a ON g.id = a.group_id AND g.organization_id = a.organization_id
        LEFT JOIN 
            group_rations gr ON g.id = gr.group_id
        WHERE 
            g.organization_id = p_organization_id
        GROUP BY 
            g.id, g.name, gr.morning_feeding, gr.day_feeding, gr.night_feeding, gr.ration_id
    ),
    ration_costs AS (
        SELECT
            rc.ration_id,
            SUM(rc.kg * rc.cost) AS total_cost_per_head,
            SUM(rc.kg * c.sv) AS total_sv_per_head,
            SUM(rc.kg * c.sp) AS total_sp_per_head,
            SUM(rc.kg * c.cep) AS total_cep_per_head,
            SUM(rc.kg * c.ndk) AS total_ndk_per_head
        FROM
            rations_components rc
        JOIN
            components c ON rc.component_id = c.id
        GROUP BY
            rc.ration_id
    )
    SELECT
        gs.group_id,
        gs.group_name,
        gs.active_animals_count,
        gs.morning_feeding,
        gs.day_feeding,
        gs.night_feeding,
        gs.ration_id,
        r.name AS ration_name,
        COALESCE(rc.total_cost_per_head, 0)::float8 AS ration_cost_per_head,
        COALESCE(rc.total_cost_per_head * gs.active_animals_count, 0)::float8 AS total_ration_cost,
        COALESCE(rc.total_sv_per_head, 0)::int AS sv_per_head,
        COALESCE(rc.total_sp_per_head, 0)::int AS sp_per_head,
        COALESCE(rc.total_cep_per_head, 0)::float8 AS cep_per_head,
        COALESCE(rc.total_ndk_per_head, 0)::int AS ndk_per_head,
        COALESCE(rc.total_sv_per_head * gs.active_animals_count, 0)::int AS total_sv,
        COALESCE(rc.total_sp_per_head * gs.active_animals_count, 0)::int AS total_sp,
        COALESCE(rc.total_cep_per_head * gs.active_animals_count, 0)::float8 AS total_cep,
        COALESCE(rc.total_ndk_per_head * gs.active_animals_count, 0)::int AS total_ndk
    FROM
        group_stats gs
    LEFT JOIN
        ration_costs rc ON gs.ration_id = rc.ration_id
    LEFT JOIN
        rations r ON gs.ration_id = r.id
    ORDER BY
        gs.group_name;
END;
$$;


--
-- Name: get_identification_fields(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_identification_fields(org_id uuid) RETURNS TABLE(id uuid, field_name text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT if.id, if.field_name
    FROM identification_fields if
    WHERE if.organization_id = org_id;
END;
$$;


--
-- Name: get_medicines_by_organization(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_medicines_by_organization(p_organization_id uuid) RETURNS TABLE(id uuid, organization_id uuid, name text, substance text, drug_elimination_period text, shelf_life text, factory text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT
        m.id,
        m.organization_id,
        m.name,
        m.substance,
        m.drug_elimination_period,
        m.shelf_life,
        m.factory
    FROM medicine m
    WHERE m.organization_id = p_organization_id
    ORDER BY m.name;
END;
$$;


--
-- Name: get_organization_components(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_organization_components(p_organization_id uuid) RETURNS TABLE(component_id uuid, component_name text, cost double precision, sv integer, sp integer, cep real, ndk integer)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        c.id,
        c.name,
        c.cost,
        c.sv,
        c.sp,
        c.cep,
        c.ndk
    FROM 
        components c
    WHERE 
        c.organization_id = p_organization_id
    ORDER BY 
        c.name;
END;
$$;


--
-- Name: get_organization_groups_with_stats(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_organization_groups_with_stats(org_id uuid) RETURNS TABLE(group_id uuid, group_name text, animal_count bigint, group_ration_id uuid, group_ration_name text, morning_feeding double precision, day_feeding double precision, night_feeding double precision, total_kg double precision, total_kg_for_group double precision)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    WITH group_animal_counts AS (
        SELECT 
            g.id AS group_id,
            g.name AS group_name,
            COUNT(a.id) AS animal_count
        FROM 
            groups g
        LEFT JOIN 
            animals a ON g.id = a.group_id AND a.organization_id = org_id
        WHERE 
            g.organization_id = org_id
        GROUP BY 
            g.id, g.name
    ),
    group_ration_info AS (
        SELECT
            gr.group_id,
            gr.ration_id AS group_ration_id,
            r.name AS group_ration_name,
            gr.morning_feeding,
            gr.day_feeding,
            gr.night_feeding,
            COALESCE(SUM(rc.kg), 0) AS total_kg
        FROM
            group_rations gr
        JOIN
            rations r ON gr.ration_id = r.id AND r.organization_id = org_id
        LEFT JOIN
            rations_components rc ON gr.ration_id = rc.ration_id
        GROUP BY
            gr.group_id, gr.ration_id, r.name, gr.morning_feeding, gr.day_feeding, gr.night_feeding
    )
    SELECT
        gac.group_id,
        gac.group_name,
        gac.animal_count,
        gri.group_ration_id,
        gri.group_ration_name,
        gri.morning_feeding,
        gri.day_feeding,
        gri.night_feeding,
        COALESCE(gri.total_kg, 0) AS total_kg,
        COALESCE(gri.total_kg * gac.animal_count, 0) AS total_kg_for_group
    FROM
        group_animal_counts gac
    LEFT JOIN
        group_ration_info gri ON gac.group_id = gri.group_id;
END;
$$;


--
-- Name: get_organization_groups_with_stats2(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_organization_groups_with_stats2(org_id uuid) RETURNS TABLE(group_id uuid, group_name text, animal_count bigint, group_ration_id uuid, group_ration_name text, morning_feeding double precision, day_feeding double precision, night_feeding double precision, total_kg double precision, total_kg_for_group double precision, total_cost double precision)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    WITH group_animal_counts AS (
        SELECT 
            g.id AS group_id,
            g.name AS group_name,
            COUNT(a.id) AS animal_count
        FROM 
            groups g
        LEFT JOIN 
            animals a ON g.id = a.group_id AND a.organization_id = org_id
        WHERE 
            g.organization_id = org_id
        GROUP BY 
            g.id, g.name
    ),
    ration_costs AS (
        SELECT 
            rc.ration_id,
            SUM(rc.kg * COALESCE(rc.cost, 0)) AS cost_per_ration
        FROM 
            rations_components rc
        JOIN 
            rations r ON rc.ration_id = r.id AND r.organization_id = org_id
        GROUP BY 
            rc.ration_id
    ),
    group_ration_info AS (
        SELECT
            gr.group_id,
            gr.ration_id AS group_ration_id,
            r.name AS group_ration_name,
            gr.morning_feeding,
            gr.day_feeding,
            gr.night_feeding,
            COALESCE(SUM(rc.kg), 0) AS total_kg,
            COALESCE(rc_cost.cost_per_ration, 0) AS ration_cost
        FROM
            group_rations gr
        JOIN
            rations r ON gr.ration_id = r.id AND r.organization_id = org_id
        LEFT JOIN
            rations_components rc ON gr.ration_id = rc.ration_id
        LEFT JOIN
            ration_costs rc_cost ON gr.ration_id = rc_cost.ration_id
        GROUP BY
            gr.group_id, gr.ration_id, r.name, gr.morning_feeding, 
            gr.day_feeding, gr.night_feeding, rc_cost.cost_per_ration
    )
    SELECT
        gac.group_id,
        gac.group_name,
        gac.animal_count,
        gri.group_ration_id,
        gri.group_ration_name,
        gri.morning_feeding,
        gri.day_feeding,
        gri.night_feeding,
        COALESCE(gri.total_kg, 0) AS total_kg,
        COALESCE(gri.total_kg * gac.animal_count, 0) AS total_kg_for_group,
        COALESCE(gri.ration_cost * gac.animal_count, 0) AS total_cost
    FROM
        group_animal_counts gac
    LEFT JOIN
        group_ration_info gri ON gac.group_id = gri.group_id;
END;
$$;


--
-- Name: get_pregnancy_by_organization(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_pregnancy_by_organization(org_id uuid) RETURNS TABLE(organization_id uuid, cow_id uuid, cow_tag_number text, status text, insemination_type text, insemination_date date, bull_id uuid, bull_tag_number text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        a.organization_id,
        a.id AS cow_id,
        a.tag_number AS cow_tag_number,  -- Tag number коровы
        p.status,
        i.insemination_type,
        i.date AS insemination_date,
        i.bull_id,
        bull.tag_number AS bull_tag_number  -- Tag number быка
    FROM 
        animals a
    LEFT JOIN 
        pregnancy p ON a.id = p.cow_id
    LEFT JOIN 
        insemination i ON a.id = i.cow_id
    LEFT JOIN
        animals bull ON i.bull_id = bull.id  -- Дополнительное соединение для получения данных быка
    WHERE 
        a.organization_id = org_id
        AND p.status IS NOT NULL
    ORDER BY 
        a.id, i.date DESC;
END;
$$;


--
-- Name: get_pregnancy_by_organization2(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_pregnancy_by_organization2(org_id uuid) RETURNS TABLE(organization_id uuid, cow_id uuid, cow_tag_number text, status text, insemination_type text, insemination_date date, bull_id uuid, bull_tag_number text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        a.organization_id,
        a.id AS cow_id,
        a.tag_number AS cow_tag_number,  -- Tag number коровы
        p.status,
        i.insemination_type,
        i.date AS insemination_date,
        -- Извлекаем первого быка из JSONB массива
        CASE 
            WHEN i.bull_id IS NOT NULL AND jsonb_array_length(i.bull_id) > 0 THEN
                (i.bull_id->>0)::uuid
            ELSE NULL
        END AS bull_id,
        -- Получаем тег первого быка
        CASE 
            WHEN i.bull_id IS NOT NULL AND jsonb_array_length(i.bull_id) > 0 THEN
                (SELECT tag_number FROM animals WHERE id = (i.bull_id->>0)::uuid)
            ELSE NULL
        END AS bull_tag_number
    FROM 
        animals a
    LEFT JOIN 
        pregnancy p ON a.id = p.cow_id
    LEFT JOIN 
        insemination i ON a.id = i.cow_id
    WHERE 
        a.organization_id = org_id
        AND p.status IS NOT NULL
    ORDER BY 
        a.id, i.date DESC;
END;
$$;


--
-- Name: get_pregnancy_statistics(uuid, date, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_pregnancy_statistics(p_organization_id uuid, p_date_from date, p_date_to date) RETURNS TABLE(pregnancy_id uuid, cow_id uuid, pregnancy_date date, status text, expected_calving_date date, total_records bigint, status_count bigint, status_ratio numeric)
    LANGUAGE sql
    AS $$
WITH filtered_pregnancy AS (
    SELECT
        p.id,
        p.cow_id,
        p.date,
        p.status,
        p.expected_calving_date
    FROM pregnancy p
    JOIN animals a ON a.id = p.cow_id
    WHERE a.organization_id = p_organization_id
      AND p.date BETWEEN p_date_from AND p_date_to
),
status_stats AS (
    SELECT
        status,
        COUNT(*) AS status_count,
        COUNT(*) OVER () AS total_records
    FROM filtered_pregnancy
    GROUP BY status
)
SELECT
    fp.id                   AS pregnancy_id,
    fp.cow_id,
    fp.date                 AS pregnancy_date,
    fp.status,
    fp.expected_calving_date,

    ss.total_records,
    ss.status_count,
    ROUND(
        ss.status_count::numeric
        / NULLIF(ss.total_records, 0),
        4
    ) AS status_ratio
FROM filtered_pregnancy fp
JOIN status_stats ss
  ON ss.status = fp.status
ORDER BY fp.status, fp.date;
$$;


--
-- Name: get_ration_summary_enhanced(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_ration_summary_enhanced(p_ration_id uuid) RETURNS TABLE(ration_id uuid, ration_name text, ration_description text, organization_id uuid, created_at timestamp with time zone, total_dry_matter double precision, total_ne_maintenance double precision, total_ne_gain double precision, total_crude_protein double precision, total_degradable_protein double precision, total_crude_fat double precision, total_byproduct double precision, total_roughage double precision, total_ndf double precision, total_forage_ndf double precision, total_starch double precision, total_calcium double precision, total_phosphorus double precision, total_salt double precision, total_potassium double precision, total_sulfur double precision, total_cost double precision, components_count double precision)
    LANGUAGE plpgsql
    AS $$
DECLARE
    temp_component RECORD;
    temp_dry_matter float8 := 0;
    temp_ne_maintenance float8 := 0;
    temp_ne_gain float8 := 0;
    temp_crude_protein float8 := 0;
    temp_degradable_protein float8 := 0;
    temp_crude_fat float8 := 0;
    temp_byproduct float8 := 0;
    temp_roughage float8 := 0;
    temp_ndf float8 := 0;
    temp_forage_ndf float8 := 0;
    temp_starch float8 := 0;
    temp_calcium float8 := 0;
    temp_phosphorus float8 := 0;
    temp_salt float8 := 0;
    temp_potassium float8 := 0;
    temp_sulfur float8 := 0;
    temp_cost float8 := 0;
    temp_count float8 := 0;
BEGIN
    -- Сначала получаем основную информацию о рационе
    SELECT 
        r.id, r.name, r.description, r.organization_id, r.created_at
    INTO 
        ration_id, ration_name, ration_description, organization_id, created_at
    FROM 
        rations r
    WHERE 
        r.id = p_ration_id;
    
    -- Если рацион не найден, возвращаем NULL
    IF NOT FOUND THEN
        RETURN;
    END IF;
    
    -- Рассчитываем показатели для каждого компонента и суммируем
    FOR temp_component IN
        SELECT 
            c."Dry Matter",
            c."NE Maintenance",
            c."NE Gain",
            c."Crude Protein",
            c."Degradable Protein",
            c."Crude Fat",
            c."ByProduct, % DM",
            c."Roughage, % DM",
            c."NDF",
            c."Forage NDF",
            c."Starch",
            c."Calcium",
            c."Phosphorus",
            c."Salt",
            c."Potassium",
            c."Sulfur",
            COALESCE(rc.cost, 0) as cost,
            rc.count
        FROM 
            rations_components rc
        JOIN 
            components c ON rc.component_id = c.id
        WHERE 
            rc.ration_id = p_ration_id
    LOOP
        -- Умножаем каждый показатель на количество компонента и добавляем к общей сумме
        temp_dry_matter := temp_dry_matter + (temp_component."Dry Matter" * temp_component.count);
        temp_ne_maintenance := temp_ne_maintenance + (temp_component."NE Maintenance" * temp_component.count);
        temp_ne_gain := temp_ne_gain + (temp_component."NE Gain" * temp_component.count);
        temp_crude_protein := temp_crude_protein + (temp_component."Crude Protein" * temp_component.count);
        temp_degradable_protein := temp_degradable_protein + (temp_component."Degradable Protein" * temp_component.count);
        temp_crude_fat := temp_crude_fat + (temp_component."Crude Fat" * temp_component.count);
        temp_byproduct := temp_byproduct + (temp_component."ByProduct, % DM" * temp_component.count);
        temp_roughage := temp_roughage + (temp_component."Roughage, % DM" * temp_component.count);
        temp_ndf := temp_ndf + (temp_component."NDF" * temp_component.count);
        temp_forage_ndf := temp_forage_ndf + (temp_component."Forage NDF" * temp_component.count);
        temp_starch := temp_starch + (temp_component."Starch" * temp_component.count);
        temp_calcium := temp_calcium + (temp_component."Calcium" * temp_component.count);
        temp_phosphorus := temp_phosphorus + (temp_component."Phosphorus" * temp_component.count);
        temp_salt := temp_salt + (temp_component."Salt" * temp_component.count);
        temp_potassium := temp_potassium + (temp_component."Potassium" * temp_component.count);
        temp_sulfur := temp_sulfur + (temp_component."Sulfur" * temp_component.count);
        temp_cost := temp_cost + (temp_component.cost * temp_component.count);
        temp_count := temp_count + 1;
    END LOOP;
    
    -- Заполняем возвращаемые значения
    total_dry_matter := temp_dry_matter;
    total_ne_maintenance := temp_ne_maintenance;
    total_ne_gain := temp_ne_gain;
    total_crude_protein := temp_crude_protein;
    total_degradable_protein := temp_degradable_protein;
    total_crude_fat := temp_crude_fat;
    total_byproduct := temp_byproduct;
    total_roughage := temp_roughage;
    total_ndf := temp_ndf;
    total_forage_ndf := temp_forage_ndf;
    total_starch := temp_starch;
    total_calcium := temp_calcium;
    total_phosphorus := temp_phosphorus;
    total_salt := temp_salt;
    total_potassium := temp_potassium;
    total_sulfur := temp_sulfur;
    total_cost := temp_cost;
    components_count := temp_count;
    
    RETURN NEXT;
END;
$$;


--
-- Name: get_ration_summary_enhanced2(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_ration_summary_enhanced2(p_ration_id uuid) RETURNS TABLE(ration_id uuid, ration_name text, ration_description text, organization_id uuid, created_at timestamp with time zone, total_sv double precision, total_sp double precision, total_cep double precision, total_ndk double precision, total_cost double precision, components_count bigint)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    WITH ration_info AS (
        SELECT 
            r.id,
            r.name,
            r.description,
            r.organization_id,
            r.created_at
        FROM 
            rations r
        WHERE 
            r.id = p_ration_id
    ),
    component_stats AS (
        SELECT 
            SUM(rc.kg * COALESCE(c.sv, 0)) AS total_sv,
            SUM(rc.kg * COALESCE(c.sp, 0)) AS total_sp,
            SUM(rc.kg * COALESCE(c.cep, 0)) AS total_cep,
            SUM(rc.kg * COALESCE(c.ndk, 0)) AS total_ndk,
            SUM(rc.kg * COALESCE(rc.cost, 0)) AS total_cost,
            COUNT(*) AS components_count
        FROM 
            rations_components rc
        JOIN 
            components c ON rc.component_id = c.id
        WHERE 
            rc.ration_id = p_ration_id
    )
    SELECT 
        ri.id,
        ri.name,
        ri.description,
        ri.organization_id,
        ri.created_at,
        COALESCE(cs.total_sv, 0),
        COALESCE(cs.total_sp, 0),
        COALESCE(cs.total_cep, 0),
        COALESCE(cs.total_ndk, 0),
        COALESCE(cs.total_cost, 0),
        COALESCE(cs.components_count, 0)
    FROM 
        ration_info ri
    LEFT JOIN 
        component_stats cs ON true;
END;
$$;


--
-- Name: get_rations_with_components(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_rations_with_components(p_organization_id uuid) RETURNS TABLE(ration_id uuid, ration_name text, ration_description text, created_at timestamp with time zone, component_id uuid, component_name text, kg double precision, cost double precision, sv integer, sp integer, cep real, ndk integer)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        r.id AS ration_id,
        r.name AS ration_name,
        r.description AS ration_description,
        r.created_at,
        c.id AS component_id,
        c.name AS component_name,
        rc.kg,
        rc.cost,
        c.sv,
        c.sp,
        c.cep,
        c.ndk
    FROM 
        rations r
    JOIN 
        rations_components rc ON r.id = rc.ration_id
    JOIN 
        components c ON rc.component_id = c.id
    WHERE 
        r.organization_id = p_organization_id
    ORDER BY 
        r.name, c.name;
END;
$$;


--
-- Name: get_rations_with_components_by_org(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_rations_with_components_by_org(p_organization_id uuid) RETURNS TABLE(ration_id uuid, ration_name text, ration_description text, created_at timestamp with time zone, component_id uuid, component_name text, component_count double precision, component_measure text, component_cost double precision, component_dry_matter double precision, component_ne_maintenance double precision, component_ne_gain double precision, component_crude_protein double precision, component_degradable_protein double precision, component_crude_fat double precision, component_byproduct double precision, component_roughage double precision, component_ndf double precision, component_forage_ndf double precision, component_starch double precision, component_calcium double precision, component_phosphorus double precision, component_salt double precision, component_potassium double precision, component_sulfur double precision)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        r.id AS ration_id,
        r.name AS ration_name,
        r.description AS ration_description,
        r.created_at,
        c.id AS component_id,
        c.name AS component_name,
        rc.count AS component_count,
        rc.measure AS component_measure,
        rc.cost AS component_cost,
        c."Dry Matter" AS component_dry_matter,
        c."NE Maintenance" AS component_ne_maintenance,
        c."NE Gain" AS component_ne_gain,
        c."Crude Protein" AS component_crude_protein,
        c."Degradable Protein" AS component_degradable_protein,
        c."Crude Fat" AS component_crude_fat,
        c."ByProduct, % DM" AS component_byproduct,
        c."Roughage, % DM" AS component_roughage,
        c."NDF" AS component_ndf,
        c."Forage NDF" AS component_forage_ndf,
        c."Starch" AS component_starch,
        c."Calcium" AS component_calcium,
        c."Phosphorus" AS component_phosphorus,
        c."Salt" AS component_salt,
        c."Potassium" AS component_potassium,
        c."Sulfur" AS component_sulfur
    FROM 
        rations r
    JOIN 
        rations_components rc ON r.id = rc.ration_id
    JOIN 
        components c ON rc.component_id = c.id
    WHERE 
        r.organization_id = p_organization_id
    ORDER BY 
        r.name, c.name;
END;
$$;


--
-- Name: get_rations_with_components_json(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_rations_with_components_json(p_organization_id uuid) RETURNS TABLE(ration_id uuid, ration_name text, ration_description text, created_at timestamp with time zone, components jsonb)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        r.id AS ration_id,
        r.name AS ration_name,
        r.description AS ration_description,
        r.created_at,
        jsonb_agg(
            jsonb_build_object(
                'id', c.id,
                'name', c.name,
                'count', rc.count,
                'measure', rc.measure,
                'cost', rc.cost,
                'nutrients', jsonb_build_object(
                    'dry_matter', c."Dry Matter",
                    'ne_maintenance', c."NE Maintenance",
                    'ne_gain', c."NE Gain",
                    'crude_protein', c."Crude Protein",
                    'degradable_protein', c."Degradable Protein",
                    'crude_fat', c."Crude Fat",
                    'byproduct', c."ByProduct, % DM",
                    'roughage', c."Roughage, % DM",
                    'ndf', c."NDF",
                    'forage_ndf', c."Forage NDF",
                    'starch', c."Starch",
                    'calcium', c."Calcium",
                    'phosphorus', c."Phosphorus",
                    'salt', c."Salt",
                    'potassium', c."Potassium",
                    'sulfur', c."Sulfur"
                )
            )
        ) AS components
    FROM 
        rations r
    JOIN 
        rations_components rc ON r.id = rc.ration_id
    JOIN 
        components c ON rc.component_id = c.id
    WHERE 
        r.organization_id = p_organization_id
    GROUP BY 
        r.id, r.name, r.description, r.created_at
    ORDER BY 
        r.name;
END;
$$;


--
-- Name: get_research_by_organization(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_research_by_organization(p_organization_id uuid) RETURNS TABLE(research_id uuid, organization_id uuid, animal_id uuid, animal_tag_number text, research_name text, material_type text, collection_date date, collected_by text, research_result text, research_notes text, created_at timestamp without time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        r.id AS research_id,
        r.organization_id,
        r.animal_id,
        a.tag_number AS animal_tag_number,
        r.research_name,
        r.material_type,
        r.collection_date,
        r.collected_by,
        r.result AS research_result,
        r.notes AS research_notes,
        r.created_at
    FROM 
        research r
    JOIN
        animals a ON r.animal_id = a.id
    WHERE 
        r.organization_id = p_organization_id
    ORDER BY 
        r.collection_date DESC NULLS LAST,
        r.created_at DESC;
END;
$$;


--
-- Name: get_type_actions_by_organization_and_type(uuid, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_type_actions_by_organization_and_type(p_organization_id uuid, p_action_type text) RETURNS TABLE(action_id uuid, animal_id uuid, animal_tag_number text, action_type text, action_subtype text, action_date date, performed_by text, action_result text, action_medicine text, action_dose text, action_notes text, next_action_date date, old_type text, new_type text, created_at timestamp without time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        da.id AS action_id,
        da.animal_id,
        a.tag_number AS animal_tag_number,
        da.action_type,
        da.action_subtype,
        da.date AS action_date,
        da.performed_by,
        da.result AS action_result,
        da.medicine AS action_medicine,
        da.dose AS action_dose,
        da.notes AS action_notes,
        da.next_action_date,
        da.old_type,
        da.new_type,
        da.created_at
    FROM 
        daily_actions da
    JOIN 
        animals a ON da.animal_id = a.id
    LEFT JOIN
        groups og ON da.old_group_id = og.id AND og.organization_id = p_organization_id
    LEFT JOIN
        groups ng ON da.new_group_id = ng.id AND ng.organization_id = p_organization_id
    WHERE 
        a.organization_id = p_organization_id
        AND da.action_type = p_action_type
    ORDER BY 
        da.date DESC, 
        da.created_at DESC;
END;
$$;


--
-- Name: get_user_info(text, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_user_info(p_username text, p_password text) RETURNS text
    LANGUAGE plpgsql
    AS $$
DECLARE
    result TEXT;
BEGIN
    SELECT STRING_AGG(user_info, '; ')
    INTO result
    FROM (
        SELECT u.id::TEXT || ', ' || 
               u.organization_id::TEXT || ', ' || 
               o.name || ', ' || 
               u.name || ', ' || 
               u.role_id::TEXT || ', ' || 
               COALESCE(STRING_AGG(p.permission, '; '), 'Нет прав') AS user_info  -- Изменено на p.permission
        FROM users u
        JOIN organizations o ON u.organization_id = o.id
        LEFT JOIN roles r ON u.role_id = r.id
        LEFT JOIN roles_permissions rp ON r.id = rp.role_id
        LEFT JOIN permissions p ON rp.permission_id = p.id  -- Добавлен JOIN с таблицей permissions
        WHERE (u.username = p_username) AND u.password = p_password  -- Здесь нужно учесть хеширование
        GROUP BY u.id, u.organization_id, o.name, u.name, u.role_id
    ) AS subquery;

    RETURN result;
END;
$$;


--
-- Name: get_user_info2(text, character varying, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_user_info2(p_username text, p_phone character varying, p_password text) RETURNS text
    LANGUAGE plpgsql
    AS $$
DECLARE
    result TEXT;
BEGIN
    SELECT STRING_AGG(user_info, '; ')
    INTO result
    FROM (
        SELECT u.id::TEXT || ', ' || 
               u.organization_id::TEXT || ', ' || 
               o.name || ', ' || 
               u.name || ', ' || 
               u.role_id::TEXT || ', ' || 
               COALESCE(STRING_AGG(p.permission, '; '), 'Нет прав') AS user_info  -- Изменено на p.permission
        FROM users u
        JOIN organizations o ON u.organization_id = o.id
        LEFT JOIN roles r ON u.role_id = r.id
        LEFT JOIN roles_permissions rp ON r.id = rp.role_id
        LEFT JOIN permissions p ON rp.permission_id = p.id  -- Добавлен JOIN с таблицей permissions
        WHERE (u.username = p_username or u.phone = p_phone) AND u.password = p_password  -- Здесь нужно учесть хеширование
        GROUP BY u.id, o.name, u.name, u.phone
    ) AS subquery;

    RETURN result;
END;
$$;


--
-- Name: get_user_info2(text, character varying, text, character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_user_info2(p_username text, p_phone character varying, p_password text, p_tg_id character varying) RETURNS text
    LANGUAGE plpgsql
    AS $$
DECLARE
    result TEXT;
BEGIN
    SELECT STRING_AGG(user_info, '; ')
    INTO result
    FROM (
        SELECT u.id::TEXT || ', ' || 
               COALESCE(u.organization_id::TEXT, 'Нет организации') || ', ' || 
               COALESCE(o.name, 'Нет организации') || ', ' || 
               u.name || ', ' || 
               COALESCE(u.role_id::TEXT, 'Нет роли') || ', ' || 
               COALESCE(
                   (SELECT STRING_AGG(p.permission, '; ') 
                    FROM permissions p
                    JOIN roles_permissions rp ON p.id = rp.permission_id
                    WHERE rp.role_id = u.role_id),
                   'Нет прав'
               ) AS user_info
        FROM users u
        LEFT JOIN organizations o ON u.organization_id = o.id
        WHERE ((u.username = p_username or u.phone = p_phone) AND u.password = p_password) or (u.tg_id = p_tg_id)
        GROUP BY u.id, o.name, u.name, u.phone, u.role_id
    ) AS subquery;

    RETURN result;
END;
$$;


--
-- Name: get_users_by_organization(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_users_by_organization(org_id uuid) RETURNS TABLE(id uuid, organization_id uuid, username text, password text, role_id uuid, created_at timestamp without time zone, name text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        u.id,
        u.organization_id,
        u.username,
        u.password,
        u.role_id,
        u.created_at,
        u.name
    FROM 
        users u
    WHERE 
        u.organization_id = org_id;
END;
$$;


--
-- Name: get_vaccination_medicines(uuid, date, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_vaccination_medicines(p_organization_id uuid, p_date_from date, p_date_to date) RETURNS TABLE(medicine text)
    LANGUAGE sql
    AS $$
    SELECT DISTINCT
        da.medicine
    FROM daily_actions da
    JOIN animals a ON a.id = da.animal_id
    WHERE a.organization_id = p_organization_id
      AND da.action_subtype = 'Вакцинация'
      AND da.date BETWEEN p_date_from AND p_date_to
      AND da.medicine IS NOT NULL
    ORDER BY da.medicine;
$$;


--
-- Name: get_vaccination_medicines_2(uuid, date, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_vaccination_medicines_2(p_organization_id uuid, p_date_from date, p_date_to date) RETURNS TABLE(medicine_id uuid, medicine_name text)
    LANGUAGE sql
    AS $$
    SELECT DISTINCT
        m.id AS medicine_id,
        m.name AS medicine_name
    FROM daily_actions da
    JOIN animals a ON a.id = da.animal_id
    -- Джоин с таблицей medicine по названию и организации
    JOIN medicine m ON m.name = da.medicine 
                   AND m.organization_id = p_organization_id
    WHERE a.organization_id = p_organization_id
      AND da.action_subtype = 'Вакцинация'
      AND da.date BETWEEN p_date_from AND p_date_to
    ORDER BY m.name;
$$;


--
-- Name: get_vaccination_statistics(uuid, date, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_vaccination_statistics(p_organization_id uuid, p_date_from date, p_date_to date) RETURNS TABLE(action_id uuid, animal_id uuid, action_date date, medicine text, performed_by text, notes text, total_vaccinations bigint, medicine_count bigint, medicine_ratio numeric)
    LANGUAGE sql
    AS $$
WITH filtered_actions AS (
    SELECT
        da.id,
        da.animal_id,
        da.date,
        da.medicine,
        da.performed_by,
        da.notes
    FROM daily_actions da
    JOIN animals a ON a.id = da.animal_id
    WHERE a.organization_id = p_organization_id
      AND da.action_subtype = 'Вакцинация'
      AND da.date BETWEEN p_date_from AND p_date_to
),
stats AS (
    SELECT
        medicine,
        COUNT(*) AS medicine_count,
        COUNT(*) OVER () AS total_vaccinations
    FROM filtered_actions
    GROUP BY medicine
)
SELECT
    fa.id            AS action_id,
    fa.animal_id,
    fa.date          AS action_date,
    fa.medicine,
    fa.performed_by,
    fa.notes,

    s.total_vaccinations,
    s.medicine_count,
    ROUND(
        s.medicine_count::numeric
        / NULLIF(s.total_vaccinations, 0),
        4
    ) AS medicine_ratio
FROM filtered_actions fa
LEFT JOIN stats s
  ON s.medicine IS NOT DISTINCT FROM fa.medicine
ORDER BY fa.date;
$$;


--
-- Name: get_weight_at_12_months_statistics(uuid, date, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.get_weight_at_12_months_statistics(p_organization_id uuid, p_date_from date, p_date_to date) RETURNS TABLE(animal_id uuid, birth_date date, target_date date, weigh_date date, weight double precision, animal_count bigint, avg_weight numeric, min_weight numeric, max_weight numeric)
    LANGUAGE sql
    AS $$
WITH targets AS (
    SELECT
        a.id AS animal_id,
        a.birth_date,
        (a.birth_date + INTERVAL '12 months')::date AS target_date
    FROM animals a
    WHERE a.organization_id = p_organization_id
      AND a.birth_date IS NOT NULL
      AND (a.birth_date + INTERVAL '12 months')::date
          BETWEEN p_date_from AND p_date_to
),
weights_12m AS (
    SELECT DISTINCT ON (t.animal_id)
        t.animal_id,
        t.birth_date,
        t.target_date,
        w.date AS weigh_date,
        w.weight
    FROM targets t
    JOIN weights w
      ON w.animal_id = t.animal_id
     AND w.date >= t.target_date
    ORDER BY t.animal_id, w.date
),
stats AS (
    SELECT
        COUNT(*) AS animal_count,
        ROUND(AVG(weight)::numeric, 2) AS avg_weight,
        MIN(weight)::numeric AS min_weight,
        MAX(weight)::numeric AS max_weight
    FROM weights_12m
)
SELECT
    w.animal_id,
    w.birth_date,
    w.target_date,
    w.weigh_date,
    w.weight,

    s.animal_count,
    s.avg_weight,
    s.min_weight,
    s.max_weight
FROM weights_12m w
CROSS JOIN stats s
ORDER BY w.target_date;
$$;


--
-- Name: if_netel_insert_insemination_and_pregnancy(uuid, date, date, text, text, text, text, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.if_netel_insert_insemination_and_pregnancy(p_animal_id uuid, p_date date, p_expected_calving_date date, p_insemination_type text, p_sperm_batch text, p_technician text, p_notes text, p_status text) RETURNS TABLE(insemination_id uuid, pregnancy_id uuid)
    LANGUAGE plpgsql
    AS $$
DECLARE
    new_insemination_id UUID := gen_random_uuid();
    new_pregnancy_id UUID := NULL;
BEGIN
    -- Вставляем insemination
    INSERT INTO insemination(
        id,
        cow_id,
        date,
        insemination_type,
        sperm_batch,
        technician,
        notes
    ) VALUES (
        new_insemination_id,
        p_animal_id,
        p_date,
        p_insemination_type,
        p_sperm_batch,
        p_technician,
        p_notes
    );

    -- Если ожидаемая дата отёла указана, создаём pregnancy запись
    IF p_expected_calving_date IS NOT NULL THEN
        new_pregnancy_id := gen_random_uuid();

        INSERT INTO pregnancy(
            id,
            cow_id,
            date,
            status,
            expected_calving_date
        ) VALUES (
            new_pregnancy_id,
            p_animal_id,
            p_date,
            p_status,
            p_expected_calving_date
        );
    END IF;

    -- Возвращаем оба id
    RETURN QUERY SELECT new_insemination_id, new_pregnancy_id;
END;
$$;


--
-- Name: insert_animal(uuid, uuid, text, date, text, text, uuid, uuid, text, uuid, text, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.insert_animal(p_id uuid, p_organization_id uuid, p_tag_number text DEFAULT NULL::text, p_birth_date date DEFAULT NULL::date, p_type text DEFAULT NULL::text, p_breed text DEFAULT NULL::text, p_mother_id uuid DEFAULT NULL::uuid, p_father_id uuid DEFAULT NULL::uuid, p_status text DEFAULT NULL::text, p_group_id uuid DEFAULT NULL::uuid, p_origin text DEFAULT NULL::text, p_origin_location text DEFAULT NULL::text) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    -- Вставка данных в таблицу animals
    INSERT INTO animals (
        id, organization_id, tag_number, birth_date, type, 
        breed, mother_id, father_id, 
        group_id, status, origin, origin_location
    ) VALUES (
        p_id, 
        p_organization_id, 
        COALESCE(p_tag_number, NULL), 
        COALESCE(p_birth_date, NULL), 
        COALESCE(p_type, NULL), 
        COALESCE(p_breed, NULL), 
        COALESCE(p_mother_id, NULL), 
        COALESCE(p_father_id, NULL), 
        COALESCE(p_group_id, NULL), 
        COALESCE(p_status, NULL), 
        COALESCE(p_origin, NULL), 
        COALESCE(p_origin_location, NULL)
    );

EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE 'Error occurred: %', SQLERRM;
        -- Здесь можно добавить код для обработки ошибок, например, логирование
END;
$$;


--
-- Name: insert_animal2(uuid, uuid, text, date, text, text, uuid, jsonb, text, uuid, text, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.insert_animal2(p_id uuid, p_organization_id uuid, p_tag_number text DEFAULT NULL::text, p_birth_date date DEFAULT NULL::date, p_type text DEFAULT NULL::text, p_breed text DEFAULT NULL::text, p_mother_id uuid DEFAULT NULL::uuid, p_father_id jsonb DEFAULT NULL::jsonb, p_status text DEFAULT NULL::text, p_group_id uuid DEFAULT NULL::uuid, p_origin text DEFAULT NULL::text, p_origin_location text DEFAULT NULL::text) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    -- Вставка данных в таблицу animals (father_id сохраняется как jsonb)
    INSERT INTO animals (
        id, organization_id, tag_number, birth_date, type,
        breed, mother_id, father_id,
        group_id, status, origin, origin_location
    ) VALUES (
        p_id,
        p_organization_id,
        COALESCE(p_tag_number, NULL),
        COALESCE(p_birth_date, NULL),
        COALESCE(p_type, NULL),
        COALESCE(p_breed, NULL),
        COALESCE(p_mother_id, NULL),
        COALESCE(p_father_id, NULL),       -- вставляем jsonb сюда
        COALESCE(p_group_id, NULL),
        COALESCE(p_status, NULL),
        COALESCE(p_origin, NULL),
        COALESCE(p_origin_location, NULL)
    );

EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE 'Error occurred in insert_animal: %', SQLERRM;
END;
$$;


--
-- Name: insert_animal_from_csv(uuid, text, date, text, text, uuid, uuid, text, uuid, text, text, text, date, date, character varying, double precision, date, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.insert_animal_from_csv(p_organization_id uuid, p_tag_number text, p_birth_date date, p_type text, p_breed text, p_mother_id uuid, p_father_id uuid, p_status text, p_group_id uuid, p_origin text, p_origin_location text, p_consumption text, p_date_of_receipt date, p_date_of_disposal date, p_last_weight_weight character varying, p_live_weight_at_disposal double precision, p_last_weigh_date date, p_reason_of_disposal text) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    -- Вставка данных в таблицу animals
    INSERT INTO animals (
        id, organization_id, tag_number, birth_date, type, 
        breed, mother_id, father_id, 
        group_id, status, origin, origin_location,
        consumption, date_of_receipt, date_of_disposal,
        last_weight_weight, live_weight_at_disposal,
        last_weigh_date, reason_of_disposal
    ) VALUES (
        gen_random_uuid(), p_organization_id, p_tag_number, p_birth_date,
		p_type, p_breed, p_mother_id, p_father_id, 
        p_group_id, p_status, p_origin, p_origin_location,
        p_consumption, p_date_of_receipt, p_date_of_disposal,
        p_last_weight_weight, p_live_weight_at_disposal,
        p_last_weigh_date, p_reason_of_disposal
    );

EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE 'Error occurred: %', SQLERRM;
        -- Здесь можно добавить код для обработки ошибок, например, логирование
END;
$$;


--
-- Name: insert_animal_identification(uuid, uuid, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.insert_animal_identification(p_animal_id uuid, p_field_id uuid, p_value text) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    -- Вставка данных в таблицу animal_identification
    INSERT INTO animal_identification (
        id, animal_id, field_id, value, created_date
    ) VALUES (
        gen_random_uuid(),  -- Генерация уникального идентификатора для записи
        p_animal_id,        -- Уникальный идентификатор животного
        p_field_id,         -- Уникальный идентификатор поля
        p_value,            -- Значение
        NOW()               -- Дата и время создания
    );
END;
$$;


--
-- Name: insert_animal_return_id(uuid, uuid, text, date, text, text, uuid, uuid, text, uuid, text, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.insert_animal_return_id(p_id uuid, p_organization_id uuid, p_tag_number text DEFAULT NULL::text, p_birth_date date DEFAULT NULL::date, p_type text DEFAULT NULL::text, p_breed text DEFAULT NULL::text, p_mother_id uuid DEFAULT NULL::uuid, p_father_id uuid DEFAULT NULL::uuid, p_status text DEFAULT NULL::text, p_group_id uuid DEFAULT NULL::uuid, p_origin text DEFAULT NULL::text, p_origin_location text DEFAULT NULL::text) RETURNS uuid
    LANGUAGE plpgsql
    AS $$
BEGIN
    -- Вставка данных в таблицу animals
    INSERT INTO animals (
        id, organization_id, tag_number, birth_date, type, 
        breed, mother_id, father_id, 
        group_id, status, origin, origin_location
    ) VALUES (
        p_id, 
        p_organization_id, 
        COALESCE(p_tag_number, NULL), 
        COALESCE(p_birth_date, NULL), 
        COALESCE(p_type, NULL), 
        COALESCE(p_breed, NULL), 
        COALESCE(p_mother_id, NULL), 
        COALESCE(p_father_id, NULL), 
        COALESCE(p_group_id, NULL), 
        COALESCE(p_status, NULL), 
        COALESCE(p_origin, NULL), 
        COALESCE(p_origin_location, NULL)
    );
    
    RETURN p_id;

EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE 'Error occurred: %', SQLERRM;
        -- Можно также вернуть NULL в случае ошибки
        RETURN NULL;
END;
$$;


--
-- Name: insert_calving(uuid, date, text, text, text, text, text, uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.insert_calving(p_cow_id uuid, p_date date, p_complication text, p_type text, p_veterinar text, p_treatments text, p_pathology text, p_calf_id uuid, p_insemination_id uuid) RETURNS uuid
    LANGUAGE plpgsql
    AS $$
DECLARE
    new_id UUID := gen_random_uuid();
BEGIN
    INSERT INTO calvings(
        id,
        cow_id,
        date,
        complication,
        type,
        veterinar,
        treatments,
        pathology,
        calf_id,
        insemination_id
    ) VALUES (
        new_id,
        p_cow_id,
        p_date,
        p_complication,
        p_type,
        p_veterinar,
        p_treatments,
        p_pathology,
        p_calf_id,
        p_insemination_id
    );
    
    RETURN new_id;
END;
$$;


--
-- Name: insert_calving2(uuid, date, text, text, text, text, text, uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.insert_calving2(p_cow_id uuid, p_date date, p_complication text, p_type text, p_veterinar text DEFAULT NULL::text, p_treatments text DEFAULT NULL::text, p_pathology text DEFAULT NULL::text, p_calf_id uuid DEFAULT NULL::uuid, p_insemination_id uuid DEFAULT NULL::uuid) RETURNS uuid
    LANGUAGE plpgsql
    AS $$
DECLARE
    new_id UUID := gen_random_uuid();
BEGIN
    INSERT INTO calvings(
        id,
        cow_id,
        date,
        complication,
        type,
        veterinar,
        treatments,
        pathology,
        calf_id,
        insemination_id
    ) VALUES (
        new_id,
        p_cow_id,
        p_date,
        p_complication,
        p_type,
        p_veterinar,
        p_treatments,
        p_pathology,
        p_calf_id,
        p_insemination_id
    );
    
    RETURN new_id;
END;
$$;


--
-- Name: insert_daily_action(uuid, uuid, text, text, date, text, text, text, text, text, date, uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.insert_daily_action(p_id uuid, p_animal_id uuid, p_action_type text, p_action_subtype text DEFAULT NULL::text, p_date date DEFAULT NULL::date, p_performed_by text DEFAULT NULL::text, p_result text DEFAULT NULL::text, p_medicine text DEFAULT NULL::text, p_dose text DEFAULT NULL::text, p_notes text DEFAULT NULL::text, p_next_action_date date DEFAULT NULL::date, p_old_group_id uuid DEFAULT NULL::uuid, p_new_group_id uuid DEFAULT NULL::uuid) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO daily_actions (
        id,
        animal_id,
        action_type,
        action_subtype,
        date,
        performed_by,
        result,
        medicine,
        dose,
        notes,
        next_action_date,
        old_group_id,
        new_group_id,
        created_at
    ) VALUES (
        p_id,
        p_animal_id,
        p_action_type,
        p_action_subtype,
        COALESCE(p_date, CURRENT_DATE),
        p_performed_by,
        p_result,
        p_medicine,
        p_dose,
        p_notes,
        p_next_action_date,
        p_old_group_id,
        p_new_group_id,
        NOW()
    );
END;
$$;


--
-- Name: insert_daily_action_new_type(uuid, uuid, text, text, date, text, text, text, text, text, date, text, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.insert_daily_action_new_type(p_id uuid, p_animal_id uuid, p_action_type text, p_action_subtype text DEFAULT NULL::text, p_date date DEFAULT NULL::date, p_performed_by text DEFAULT NULL::text, p_result text DEFAULT NULL::text, p_medicine text DEFAULT NULL::text, p_dose text DEFAULT NULL::text, p_notes text DEFAULT NULL::text, p_next_action_date date DEFAULT NULL::date, p_old_type text DEFAULT NULL::text, p_new_type text DEFAULT NULL::text) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO daily_actions (
        id,
        animal_id,
        action_type,
        action_subtype,
        date,
        performed_by,
        result,
        medicine,
        dose,
        notes,
        next_action_date,
        old_type,
        new_type,
        created_at
    ) VALUES (
        p_id,
        p_animal_id,
        p_action_type,
        p_action_subtype,
        COALESCE(p_date, CURRENT_DATE),
        p_performed_by,
        p_result,
        p_medicine,
        p_dose,
        p_notes,
        p_next_action_date,
        p_old_type,
        p_new_type,
        NOW()
    );
END;
$$;


--
-- Name: insert_daily_action_with_medicine(uuid, uuid, text, text, date, text, text, text, text, text, date, uuid, uuid, text, text, character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.insert_daily_action_with_medicine(p_id uuid, p_animal_id uuid, p_action_type text, p_action_subtype text, p_date date, p_performed_by text DEFAULT NULL::text, p_result text DEFAULT NULL::text, p_medicine text DEFAULT NULL::text, p_dose text DEFAULT NULL::text, p_notes text DEFAULT NULL::text, p_next_action_date date DEFAULT NULL::date, p_old_group_id uuid DEFAULT NULL::uuid, p_new_group_id uuid DEFAULT NULL::uuid, p_old_type text DEFAULT NULL::text, p_new_type text DEFAULT NULL::text, p_drug_elimination_period character varying DEFAULT NULL::character varying) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO daily_actions (
        id,
        animal_id,
        action_type,
        action_subtype,
        date,
        performed_by,
        result,
        medicine,
        dose,
        notes,
        next_action_date,
        old_group_id,
        new_group_id,
        created_at,
        old_type,
        new_type,
        drug_elimination_period
    ) VALUES (
        p_id,
        p_animal_id,
        p_action_type,
        p_action_subtype,
        p_date,
        p_performed_by,
        p_result,
        p_medicine,
        p_dose,
        p_notes,
        p_next_action_date,
        p_old_group_id,
        p_new_group_id,
        NOW(),
        p_old_type,
        p_new_type,
        p_drug_elimination_period
    );
END;
$$;


--
-- Name: insert_insemination(uuid, date, text, text, text, uuid, text, text, text, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.insert_insemination(p_cow_id uuid, p_date date, p_insemination_type text, p_sperm_batch text DEFAULT NULL::text, p_sperm_manufacturer text DEFAULT NULL::text, p_bull_id uuid DEFAULT NULL::uuid, p_embryo_id text DEFAULT NULL::text, p_embryo_manufacturer text DEFAULT NULL::text, p_technician text DEFAULT NULL::text, p_notes text DEFAULT NULL::text) RETURNS uuid
    LANGUAGE plpgsql
    AS $$
DECLARE
    new_id UUID := gen_random_uuid();
BEGIN
    INSERT INTO insemination(
        id,
        cow_id,
        date,
        insemination_type,
        sperm_batch,
        sperm_manufacturer,
        bull_id,
        embryo_id,
        embryo_manufacturer,
        technician,
        notes
    ) VALUES (
        new_id,
        p_cow_id,
        p_date,
        p_insemination_type,
        p_sperm_batch,
        p_sperm_manufacturer,
        p_bull_id,
        p_embryo_id,
        p_embryo_manufacturer,
        p_technician,
        p_notes
    );
    
    RETURN new_id;
END;
$$;


--
-- Name: insert_insemination2(uuid, date, text, text, text, uuid, text, text, text, text, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.insert_insemination2(p_cow_id uuid, p_date date, p_insemination_type text, p_sperm_batch text DEFAULT NULL::text, p_sperm_manufacturer text DEFAULT NULL::text, p_bull_id uuid DEFAULT NULL::uuid, p_embryo_id text DEFAULT NULL::text, p_embryo_manufacturer text DEFAULT NULL::text, p_technician text DEFAULT NULL::text, p_notes text DEFAULT NULL::text, p_bull_name text DEFAULT NULL::text) RETURNS uuid
    LANGUAGE plpgsql
    AS $$
DECLARE
    new_id UUID := gen_random_uuid();
BEGIN
    INSERT INTO insemination(
        id,
        cow_id,
        date,
        insemination_type,
        sperm_batch,
        sperm_manufacturer,
        bull_id,
        embryo_id,
        embryo_manufacturer,
        technician,
        notes, bull_name
    ) VALUES (
        new_id,
        p_cow_id,
        p_date,
        p_insemination_type,
        p_sperm_batch,
        p_sperm_manufacturer,
        p_bull_id,
        p_embryo_id,
        p_embryo_manufacturer,
        p_technician,
        p_notes, p_bull_name
    );
    
    RETURN new_id;
END;
$$;


--
-- Name: insert_insemination3(uuid, date, text, text, text, jsonb, text, text, text, text, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.insert_insemination3(p_cow_id uuid, p_date date, p_insemination_type text, p_sperm_batch text DEFAULT NULL::text, p_sperm_manufacturer text DEFAULT NULL::text, p_bull_id jsonb DEFAULT NULL::jsonb, p_embryo_id text DEFAULT NULL::text, p_embryo_manufacturer text DEFAULT NULL::text, p_technician text DEFAULT NULL::text, p_notes text DEFAULT NULL::text, p_bull_name text DEFAULT NULL::text) RETURNS uuid
    LANGUAGE plpgsql
    AS $$
DECLARE
    new_id UUID := gen_random_uuid();
BEGIN
    INSERT INTO insemination(
        id,
        cow_id,
        date,
        insemination_type,
        sperm_batch,
        sperm_manufacturer,
        bull_id,
        embryo_id,
        embryo_manufacturer,
        technician,
        notes,
        bull_name
    ) VALUES (
        new_id,
        p_cow_id,
        p_date,
        p_insemination_type,
        p_sperm_batch,
        p_sperm_manufacturer,
        p_bull_id,            -- вставляем jsonb
        p_embryo_id,
        p_embryo_manufacturer,
        p_technician,
        p_notes,
        p_bull_name
    );

    RETURN new_id;
END;
$$;


--
-- Name: insert_insemination3(uuid, date, text, text, text, uuid, text, text, text, text, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.insert_insemination3(p_cow_id uuid, p_date date, p_insemination_type text, p_sperm_batch text DEFAULT NULL::text, p_sperm_manufacturer text DEFAULT NULL::text, p_bull_id uuid DEFAULT NULL::uuid, p_embryo_id text DEFAULT NULL::text, p_embryo_manufacturer text DEFAULT NULL::text, p_technician text DEFAULT NULL::text, p_notes text DEFAULT NULL::text, p_bull_name text DEFAULT NULL::text) RETURNS uuid
    LANGUAGE plpgsql
    AS $$

DECLARE
    new_id UUID := gen_random_uuid();
BEGIN
    INSERT INTO insemination(
        id,
        cow_id,
        date,
        insemination_type,
        sperm_batch,
        sperm_manufacturer,
        bull_id,
        embryo_id,
        embryo_manufacturer,
        technician,
        notes,
        bull_name
    ) VALUES (
        new_id,
        p_cow_id,
        p_date,
        p_insemination_type,
        p_sperm_batch,
        p_sperm_manufacturer,
        p_bull_id,            -- вставляем jsonb
        p_embryo_id,
        p_embryo_manufacturer,
        p_technician,
        p_notes,
        p_bull_name
    );

    RETURN new_id;
END;
$$;


--
-- Name: insert_insemination4(uuid, date, text, text, text, jsonb, text, text, text, text, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.insert_insemination4(p_cow_id uuid, p_date date, p_insemination_type text, p_sperm_batch text DEFAULT NULL::text, p_sperm_manufacturer text DEFAULT NULL::text, p_bull_id jsonb DEFAULT NULL::jsonb, p_embryo_id text DEFAULT NULL::text, p_embryo_manufacturer text DEFAULT NULL::text, p_technician text DEFAULT NULL::text, p_notes text DEFAULT NULL::text, p_bull_name text DEFAULT NULL::text) RETURNS uuid
    LANGUAGE plpgsql
    AS $$
DECLARE
    new_id UUID := gen_random_uuid();
BEGIN
    INSERT INTO insemination(
        id,
        cow_id,
        date,
        insemination_type,
        sperm_batch,
        sperm_manufacturer,
        bull_id,
        embryo_id,
        embryo_manufacturer,
        technician,
        notes,
        bull_name
    ) VALUES (
        new_id,
        p_cow_id,
        p_date,
        p_insemination_type,
        p_sperm_batch,
        p_sperm_manufacturer,
        p_bull_id,            -- вставляем jsonb
        p_embryo_id,
        p_embryo_manufacturer,
        p_technician,
        p_notes,
        p_bull_name
    );

    RETURN new_id;
END;
$$;


--
-- Name: insert_pregnancy(uuid, date, text, date, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.insert_pregnancy(p_cow_id uuid, p_date date, p_status text, p_expected_calving_date date, p_insemination_id uuid) RETURNS uuid
    LANGUAGE plpgsql
    AS $$
DECLARE
    new_id UUID := gen_random_uuid();
BEGIN
    INSERT INTO pregnancy(
        id,
        cow_id,
        date,
        status,
        expected_calving_date, 
        insemination_id
    ) VALUES (
        new_id,
        p_cow_id,
        p_date,
        p_status,
        p_expected_calving_date, 
        p_insemination_id
    );
    
    RETURN new_id;
END;
$$;


--
-- Name: insert_pregnancy2(uuid, date, text, date, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.insert_pregnancy2(p_cow_id uuid, p_date date, p_status text, p_expected_calving_date date, p_insemination_id uuid) RETURNS uuid
    LANGUAGE plpgsql
    AS $$

DECLARE
    new_id UUID := gen_random_uuid();
BEGIN
    INSERT INTO pregnancy(
        id,
        cow_id,
        date,
        status,
        expected_calving_date, 
        insemination_id
    ) VALUES (
        new_id,
        p_cow_id,
        p_date,
        p_status,
        p_expected_calving_date, 
        p_insemination_id
    );
    
    RETURN new_id;
END;
$$;


--
-- Name: insert_research(uuid, uuid, uuid, text, text, date, text, text, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.insert_research(p_id uuid, p_organization_id uuid, p_animal_id uuid, p_research_name text, p_material_type text DEFAULT NULL::text, p_collection_date date DEFAULT NULL::date, p_collected_by text DEFAULT NULL::text, p_result text DEFAULT NULL::text, p_notes text DEFAULT NULL::text) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO research (
        id,
        organization_id,
        animal_id,
        research_name,
        material_type,
        collection_date,
        collected_by,
        result,
        notes,
        created_at
    ) VALUES (
        p_id,
        p_organization_id,
        p_animal_id,
        p_research_name,
        p_material_type,
        p_collection_date,
        p_collected_by,
        p_result,
        p_notes,
        NOW()
    );
END;
$$;


--
-- Name: insert_weight_record(uuid, uuid, date, double precision, text, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.insert_weight_record(p_id uuid, p_animal_id uuid, p_date date, p_weight double precision, p_method text DEFAULT NULL::text, p_notes text DEFAULT NULL::text) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO weights (id, animal_id, date, weight, method, notes)
    VALUES (p_id, p_animal_id, p_date, p_weight, p_method, p_notes);
END;
$$;


--
-- Name: log_user_action(uuid, uuid, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.log_user_action(p_user_id uuid, p_organization_id uuid, p_action_type text) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    -- Проверяем существование пользователя
    IF NOT EXISTS(SELECT 1 FROM users WHERE id = p_user_id) THEN
        RAISE EXCEPTION 'User with id % does not exist', p_user_id;
    END IF;

    -- Проверяем существование типа действия
    IF NOT EXISTS(SELECT 1 FROM user_action_types WHERE type_name = p_action_type) THEN
        RAISE EXCEPTION 'Action type % does not exist', p_action_type;
    END IF;

    -- Вставляем запись о действии
    INSERT INTO user_actions(id, user_id, organization_id, action_type, action_date)
    VALUES(gen_random_uuid(), p_user_id, p_organization_id, p_action_type, NOW())
    ON CONFLICT DO NOTHING;
END;
$$;


--
-- Name: log_user_action(uuid, character varying, character varying, uuid, jsonb, jsonb, character varying, character varying, text, jsonb); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.log_user_action(p_user_id uuid, p_action_type character varying, p_table_name character varying DEFAULT NULL::character varying, p_record_id uuid DEFAULT NULL::uuid, p_old_values jsonb DEFAULT NULL::jsonb, p_new_values jsonb DEFAULT NULL::jsonb, p_session_id character varying DEFAULT NULL::character varying, p_status character varying DEFAULT 'success'::character varying, p_error_message text DEFAULT NULL::text, p_additional_info jsonb DEFAULT NULL::jsonb) RETURNS uuid
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_audit_id UUID;
BEGIN
    -- Генерируем новый UUID для записи
    v_audit_id := gen_random_uuid();
    
    -- Вставляем запись в таблицу аудита
    INSERT INTO user_actions_audit (
        id,
        user_id,
        action_type,
        table_name,
        record_id,
        old_values,
        new_values,
        session_id,
        action_timestamp,
        status,
        error_message,
        additional_info
    ) VALUES (
        v_audit_id,
        p_user_id,
        p_action_type,
        p_table_name,
        p_record_id,
        p_old_values,
        p_new_values,
        p_session_id,
        NOW(), -- Используем текущее время
        p_status,
        p_error_message,
        p_additional_info
    );
    
    -- Возвращаем ID созданной записи
    RETURN v_audit_id;
EXCEPTION
    WHEN foreign_key_violation THEN
        -- Обработка ошибки внешнего ключа
        RAISE EXCEPTION 'Ошибка внешнего ключа: пользователь или тип действия не существуют';
    WHEN OTHERS THEN
        -- Обработка других ошибок
        RAISE EXCEPTION 'Ошибка при записи действия: %', SQLERRM;
END;
$$;


--
-- Name: mark_animal_as_barren(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.mark_animal_as_barren(p_animal_id uuid) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    -- Пытаемся вставить новую запись
    INSERT INTO barren (animal_id, is_barren)
    VALUES (p_animal_id, TRUE)
    ON CONFLICT (animal_id) 
    DO UPDATE SET is_barren = TRUE;
    
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Не удалось добавить запись для animal_id = %', p_animal_id;
    END IF;
END;
$$;


--
-- Name: record_feeding(date, uuid, uuid, bigint, uuid, double precision, double precision, double precision, text, double precision, integer, integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.record_feeding(p_event_date date, p_organization_id uuid, p_group_id uuid, p_animal_count bigint, p_group_ration_id uuid, p_total_kg double precision, p_total_kg_for_group double precision, p_fact_kg double precision, p_feeding_time text DEFAULT NULL::text, p_feeding_coefficient double precision DEFAULT NULL::double precision, p_mark integer DEFAULT NULL::integer, p_feeding_mark integer DEFAULT NULL::integer) RETURNS uuid
    LANGUAGE plpgsql
    AS $$
DECLARE
    new_record_id uuid;
BEGIN
    -- Проверка существования группы и рациона
    IF NOT EXISTS (SELECT 1 FROM groups WHERE id = p_group_id AND organization_id = p_organization_id) THEN
        RAISE EXCEPTION 'Group not found in this organization';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM rations WHERE id = p_group_ration_id AND organization_id = p_organization_id) THEN
        RAISE EXCEPTION 'Ration not found in this organization';
    END IF;
    
    -- Проверка допустимых значений feeding_time (если указано)
    IF p_feeding_time IS NOT NULL AND p_feeding_time NOT IN ('morning', 'day', 'night') THEN
        RAISE EXCEPTION 'Invalid feeding_time value: %. Allowed values: morning, day, night', p_feeding_time;
    END IF;
    
    -- Проверка допустимых значений feeding_coefficient (если указано)
    IF p_feeding_coefficient IS NOT NULL AND (p_feeding_coefficient < 0 OR p_feeding_coefficient > 1) THEN
        RAISE EXCEPTION 'Feeding coefficient must be between 0 and 1, but was %', p_feeding_coefficient;
    END IF;
    
    -- Вставка записи
    INSERT INTO feeding_record (
        event_date,
        organization_id,
        group_id,
        animal_count,
        group_ration_id,
        total_kg,
        total_kg_for_group,
        fact_kg,
        feeding_time,
        feeding_coefficient,
        mark,
        feeding_mark
    ) VALUES (
        p_event_date,
        p_organization_id,
        p_group_id,
        p_animal_count,
        p_group_ration_id,
        p_total_kg,
        p_total_kg_for_group,
        p_fact_kg,
        p_feeding_time,
        p_feeding_coefficient,
        p_mark,
        p_feeding_mark
    )
    RETURNING id INTO new_record_id;
    
    RETURN new_record_id;
END;
$$;


--
-- Name: remove_animal_from_barren(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.remove_animal_from_barren(p_animal_id uuid) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
DECLARE
    rows_affected integer;
BEGIN
    -- Удаляем запись из таблицы barren
    DELETE FROM barren 
    WHERE animal_id = p_animal_id;
    
    -- Получаем количество удаленных строк
    GET DIAGNOSTICS rows_affected = ROW_COUNT;
    
    -- Возвращаем true, если запись была удалена, false если такой записи не было
    RETURN rows_affected > 0;
END;
$$;


--
-- Name: update_animal(uuid, text, text, uuid, date, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.update_animal(p_id uuid, p_tag_number text DEFAULT NULL::text, p_type text DEFAULT NULL::text, p_group_id uuid DEFAULT NULL::uuid, p_birth_date date DEFAULT NULL::date, p_status text DEFAULT NULL::text) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    -- Обновление данных в таблице animals
    UPDATE animals
    SET 
        tag_number = COALESCE(p_tag_number, tag_number),
        type = COALESCE(p_type, type),
        group_id = COALESCE(p_group_id, group_id),
        birth_date = COALESCE(p_birth_date, birth_date),
        status = COALESCE(p_status, status)
    WHERE 
        id = p_id;

    -- Проверка, обновлена ли строка
    IF NOT FOUND THEN
        RAISE NOTICE 'No animal found with id: %', p_id;
    END IF;

EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE 'Error occurred: %', SQLERRM;
        -- Здесь можно добавить код для обработки ошибок, например, логирование
END;
$$;


--
-- Name: update_animal_data_with_if(uuid, text, text, text, uuid, uuid, text, uuid, text, text, date, date, date, text, text, double precision, date, character varying, text, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.update_animal_data_with_if(p_animal_id uuid, p_tag_number text DEFAULT NULL::text, p_type text DEFAULT NULL::text, p_breed text DEFAULT NULL::text, p_mother_id uuid DEFAULT NULL::uuid, p_father_id uuid DEFAULT NULL::uuid, p_status text DEFAULT NULL::text, p_group_id uuid DEFAULT NULL::uuid, p_origin text DEFAULT NULL::text, p_origin_location text DEFAULT NULL::text, p_birth_date date DEFAULT NULL::date, p_date_of_receipt date DEFAULT NULL::date, p_date_of_disposal date DEFAULT NULL::date, p_reason_of_disposal text DEFAULT NULL::text, p_consumption text DEFAULT NULL::text, p_live_weight_at_disposal double precision DEFAULT NULL::double precision, p_last_weigh_date date DEFAULT NULL::date, p_last_weight_weight character varying DEFAULT NULL::character varying, p_identification_field_name text DEFAULT NULL::text, p_identification_value text DEFAULT NULL::text) RETURNS void
    LANGUAGE plpgsql
    AS $$	
	DECLARE
	    v_organization_id uuid;
	    v_field_id uuid;
	BEGIN
	    -- Получаем organization_id животного для использования в дальнейших запросах
	    SELECT organization_id INTO v_organization_id 
	    FROM animals 
	    WHERE id = p_animal_id;
	    
	    -- Обновляем только те поля animals, для которых переданы не-NULL значения
	    UPDATE animals SET
	        tag_number = COALESCE(p_tag_number, tag_number),
	        type = COALESCE(p_type, type),
	        breed = COALESCE(p_breed, breed),
	        mother_id = COALESCE(p_mother_id, mother_id),
	        father_id = COALESCE(to_jsonb(p_father_id), father_id),
	        status = COALESCE(p_status, status),
	        group_id = COALESCE(p_group_id, group_id),
	        origin = COALESCE(p_origin, origin),
	        origin_location = COALESCE(p_origin_location, origin_location),
	        birth_date = COALESCE(p_birth_date, birth_date),
	        date_of_receipt = COALESCE(p_date_of_receipt, date_of_receipt),
	        date_of_disposal = COALESCE(p_date_of_disposal, date_of_disposal),
	        reason_of_disposal = COALESCE(p_reason_of_disposal, reason_of_disposal),
	        consumption = COALESCE(p_consumption, consumption),
	        live_weight_at_disposal = COALESCE(p_live_weight_at_disposal, live_weight_at_disposal),
	        last_weigh_date = COALESCE(p_last_weigh_date, last_weigh_date),
	        last_weight_weight = COALESCE(p_last_weight_weight, last_weight_weight)
	    WHERE id = p_animal_id;
	    
	    -- Если передано имя поля идентификации и его значение
	    IF p_identification_field_name IS NOT NULL AND p_identification_value IS NOT NULL THEN
	        -- Ищем существующее поле идентификации
	        SELECT id INTO v_field_id 
	        FROM identification_fields 
	        WHERE field_name = p_identification_field_name 
	        AND organization_id = v_organization_id;
	        
	        -- Если поле не найдено, создаем новое
	        IF v_field_id IS NULL THEN
	            v_field_id := gen_random_uuid();
	            INSERT INTO identification_fields (id, field_name, organization_id)
	            VALUES (v_field_id, p_identification_field_name, v_organization_id);
	        END IF;
	        
	        -- Проверяем, существует ли уже запись для этого животного и поля
	        IF EXISTS (SELECT 1 FROM animal_identification 
	                  WHERE animal_id = p_animal_id AND field_id = v_field_id) THEN
	            -- Обновляем существующую запись
	            UPDATE animal_identification SET
	                value = p_identification_value,
	                created_date = CURRENT_TIMESTAMP
	            WHERE animal_id = p_animal_id AND field_id = v_field_id;
	        ELSE
	            -- Создаем новую запись
	            INSERT INTO animal_identification (id, animal_id, field_id, value, created_date)
	            VALUES (gen_random_uuid(), p_animal_id, v_field_id, p_identification_value, CURRENT_TIMESTAMP);
	        END IF;
	    END IF;
	END;
$$;


--
-- Name: update_animal_details2(uuid, uuid, text, text, text, uuid, jsonb, text, uuid, text, text, date, date, date, text, jsonb); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.update_animal_details2(p_animal_id uuid, p_organization_id uuid DEFAULT NULL::uuid, p_tag_number text DEFAULT NULL::text, p_type text DEFAULT NULL::text, p_breed text DEFAULT NULL::text, p_mother_id uuid DEFAULT NULL::uuid, p_father_ids jsonb DEFAULT NULL::jsonb, p_status text DEFAULT NULL::text, p_group_id uuid DEFAULT NULL::uuid, p_origin text DEFAULT NULL::text, p_origin_location text DEFAULT NULL::text, p_birth_date date DEFAULT NULL::date, p_date_of_receipt date DEFAULT NULL::date, p_date_of_disposal date DEFAULT NULL::date, p_reason_of_disposal text DEFAULT NULL::text, p_identification_data jsonb DEFAULT NULL::jsonb) RETURNS TABLE(id uuid, organization_id uuid, tag_number text, type text, breed text, mother_id uuid, mother_tag_number text, father_id jsonb, father_tag_numbers jsonb, status text, group_id uuid, group_name text, origin text, origin_location text, birth_date date, date_of_receipt date, date_of_disposal date, reason_of_disposal text, identification_data jsonb)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_key text;
    v_val text;
    v_field_id uuid;
BEGIN
    -- 1) Обновляем основные поля животного
    UPDATE animals AS a
    SET
        organization_id    = COALESCE(p_organization_id, a.organization_id),
        tag_number         = COALESCE(p_tag_number, a.tag_number),
        type               = COALESCE(p_type, a.type),
        breed              = COALESCE(p_breed, a.breed),
        mother_id          = COALESCE(p_mother_id, a.mother_id),
        father_id          = COALESCE(p_father_ids, a.father_id),
        status             = COALESCE(p_status, a.status),
        group_id           = COALESCE(p_group_id, a.group_id),
        origin             = COALESCE(p_origin, a.origin),
        origin_location    = COALESCE(p_origin_location, a.origin_location),
        birth_date         = COALESCE(p_birth_date, a.birth_date),
        date_of_receipt    = COALESCE(p_date_of_receipt, a.date_of_receipt),
        date_of_disposal   = COALESCE(p_date_of_disposal, a.date_of_disposal),
        reason_of_disposal = COALESCE(p_reason_of_disposal, a.reason_of_disposal)
    WHERE a.id = p_animal_id;

    -- 2) Обновляем идентификационные данные
    IF p_identification_data IS NOT NULL THEN
        -- Сначала удаляем все существующие идентификации для этого животного
        DELETE FROM animal_identification
        WHERE animal_id = p_animal_id;

        -- Вставляем новые значения идентификаций
        FOR v_key, v_val IN SELECT * FROM jsonb_each_text(p_identification_data)
            LOOP
                -- Ищем поле идентификации
                SELECT f.id
                INTO v_field_id
                FROM identification_fields f
                WHERE f.field_name = v_key
                LIMIT 1;

                -- Если поле не существует, создаём его
                IF v_field_id IS NULL THEN
                    INSERT INTO identification_fields(id, field_name)
                    VALUES (gen_random_uuid(), v_key)
                    RETURNING id INTO v_field_id;
                END IF;

                -- Вставляем идентификацию с проверкой дубликатов
                INSERT INTO animal_identification(id, animal_id, field_id, value)
                VALUES (gen_random_uuid(), p_animal_id, v_field_id, v_val)
                ON CONFLICT DO NOTHING;
            END LOOP;
    END IF;

    -- 3) Возвращаем обновленные данные
    RETURN QUERY
    WITH animal_identifications AS (
        SELECT
            ai.animal_id,
            jsonb_object_agg(f.field_name, ai.value) AS ident_data
        FROM animal_identification ai
        JOIN identification_fields f ON ai.field_id = f.id
        WHERE ai.animal_id = p_animal_id
        GROUP BY ai.animal_id
    )
    SELECT
        a.id,
        a.organization_id,
        a.tag_number,
        a.type,
        a.breed,
        a.mother_id,
        mother.tag_number AS mother_tag_number,
        a.father_id,
        -- Обработка father_id как JSONB массив UUID (та же логика)
        (
            SELECT jsonb_agg(f.tag_number)
            FROM animals f
            WHERE f.id IN (
                SELECT elem::uuid
                FROM jsonb_array_elements_text(a.father_id) AS elem
            )
        ) AS father_tag_numbers,
        a.status,
        a.group_id,
        g.name AS group_name,
        a.origin,
        a.origin_location,
        a.birth_date,
        a.date_of_receipt,
        a.date_of_disposal,
        a.reason_of_disposal,
        COALESCE(ai.ident_data, '{}'::jsonb) AS identification_data
    FROM animals a
    LEFT JOIN groups g ON a.group_id = g.id
    LEFT JOIN animals mother ON a.mother_id = mother.id
    LEFT JOIN animal_identifications ai ON a.id = ai.animal_id
    WHERE a.id = p_animal_id;
END;
$$;


--
-- Name: update_animal_status(uuid, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.update_animal_status(p_id uuid, p_status text) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE animals
    SET status = p_status
    WHERE id = p_id;
END;
$$;


--
-- Name: update_animal_with_idfields(uuid, uuid, text, text, text, uuid, jsonb, text, uuid, text, text, date, date, date, text, jsonb); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.update_animal_with_idfields(p_animal_id uuid, p_organization_id uuid DEFAULT NULL::uuid, p_tag_number text DEFAULT NULL::text, p_type text DEFAULT NULL::text, p_breed text DEFAULT NULL::text, p_mother_id uuid DEFAULT NULL::uuid, p_father_ids jsonb DEFAULT NULL::jsonb, p_status text DEFAULT NULL::text, p_group_id uuid DEFAULT NULL::uuid, p_origin text DEFAULT NULL::text, p_origin_location text DEFAULT NULL::text, p_birth_date date DEFAULT NULL::date, p_date_of_receipt date DEFAULT NULL::date, p_date_of_disposal date DEFAULT NULL::date, p_reason_of_disposal text DEFAULT NULL::text, p_identification_data jsonb DEFAULT NULL::jsonb) RETURNS void
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_key   text;
    v_val   text;
    v_field_id uuid;
BEGIN
    ------------------------------------------------------------------------
    -- 1. ОБНОВЛЕНИЕ animals
    ------------------------------------------------------------------------
    UPDATE animals AS a
    SET
        organization_id     = COALESCE(p_organization_id, a.organization_id),
        tag_number          = COALESCE(p_tag_number, a.tag_number),
        type                = COALESCE(p_type, a.type),
        breed               = COALESCE(p_breed, a.breed),
        mother_id           = COALESCE(p_mother_id, a.mother_id),
        father_id           = COALESCE(p_father_ids, a.father_id),
        status              = COALESCE(p_status, a.status),
        group_id            = COALESCE(p_group_id, a.group_id),
        origin              = COALESCE(p_origin, a.origin),
        origin_location     = COALESCE(p_origin_location, a.origin_location),
        birth_date          = COALESCE(p_birth_date, a.birth_date),
        date_of_receipt     = COALESCE(p_date_of_receipt, a.date_of_receipt),
        date_of_disposal    = COALESCE(p_date_of_disposal, a.date_of_disposal),
        reason_of_disposal  = COALESCE(p_reason_of_disposal, a.reason_of_disposal)
    WHERE a.id = p_animal_id;

    ------------------------------------------------------------------------
    -- 2. ОБНОВЛЕНИЕ ИДЕНТИФИКАЦИИ (строго: поле должно существовать)
    ------------------------------------------------------------------------
    IF p_identification_data IS NOT NULL THEN
        FOR v_key, v_val IN
            SELECT j.key, j.value::text
            FROM jsonb_each(p_identification_data) AS j(key, value)
            LOOP
                -- ключ должен быть UUID
                v_field_id := v_key::uuid;

                -- поле с таким id обязательно должно существовать
                IF NOT EXISTS (
                    SELECT 1
                    FROM identification_fields f
                    WHERE f.id = v_field_id
                ) THEN
                    RAISE EXCEPTION 'Unknown identification field id: %', v_field_id
                        USING ERRCODE = '22023';  -- invalid_parameter_value
                END IF;

                -- если запись уже есть – обновляем
                IF EXISTS (
                    SELECT 1
                    FROM animal_identification ai
                    WHERE ai.animal_id = p_animal_id
                      AND ai.field_id  = v_field_id
                ) THEN
                    UPDATE animal_identification
                    SET value = v_val,
                        created_date = now()
                    WHERE animal_id = p_animal_id
                      AND field_id  = v_field_id;
                ELSE
                    -- иначе вставляем новую
                    INSERT INTO animal_identification(id, animal_id, field_id, value, created_date)
                    VALUES (gen_random_uuid(), p_animal_id, v_field_id, v_val, now());
                END IF;
            END LOOP;
    END IF;
END;
$$;


--
-- Name: update_calf_id_in_reproduction(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.update_calf_id_in_reproduction(p_calf_id uuid, p_id uuid) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE reproduction
    SET calf_id = p_calf_id
    WHERE id = p_id;
END;
$$;


--
-- Name: update_component(uuid, text, double precision, integer, integer, real, integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.update_component(p_component_id uuid, p_name text, p_cost double precision, p_sv integer, p_sp integer, p_cep real, p_ndk integer) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    -- Проверка обязательных полей
    IF p_component_id IS NULL THEN
        RAISE EXCEPTION 'Component ID cannot be null';
    END IF;
    
    IF p_name IS NULL OR p_name = '' THEN
        RAISE EXCEPTION 'Component name cannot be empty';
    END IF;
    
    -- Проверка существования компонента
    IF NOT EXISTS (SELECT 1 FROM components WHERE id = p_component_id) THEN
        RAISE EXCEPTION 'Component with ID % does not exist', p_component_id;
    END IF;
    
    -- Обновление данных компонента
    UPDATE components
    SET 
        name = p_name,
        cost = p_cost,
        sv = p_sv,
        sp = p_sp,
        cep = p_cep,
        ndk = p_ndk
    WHERE 
        id = p_component_id;
END;
$$;


--
-- Name: update_group(uuid, uuid, text, uuid, text, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.update_group(p_group_id uuid, p_organization_id uuid DEFAULT NULL::uuid, p_group_name text DEFAULT NULL::text, p_group_type_id uuid DEFAULT NULL::uuid, p_description text DEFAULT NULL::text, p_location text DEFAULT NULL::text) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE groups
    SET 
        organization_id = COALESCE(p_organization_id, organization_id),
        name = COALESCE(p_group_name, name),
        type_id = COALESCE(p_group_type_id, type_id),
        description = COALESCE(p_description, description),
        location = COALESCE(p_location, location)
    WHERE 
        id = p_group_id;
END;
$$;


--
-- Name: update_group_ration(uuid, uuid, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.update_group_ration(p_group_id uuid, p_ration_id uuid DEFAULT NULL::uuid, p_accounting text DEFAULT NULL::text) RETURNS void
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_current_ration_id uuid;
    v_current_accounting text;
BEGIN
    -- Проверка существования группы
    IF NOT EXISTS (SELECT 1 FROM groups WHERE id = p_group_id) THEN
        RAISE EXCEPTION 'Группа с ID % не найдена', p_group_id;
    END IF;

    -- Получаем текущие значения
    SELECT ration_id, accounting 
    INTO v_current_ration_id, v_current_accounting
    FROM group_rations
    WHERE group_id = p_group_id;

    -- Если запись не существует, создаем новую
    IF NOT FOUND THEN
        IF p_ration_id IS NULL THEN
            RAISE EXCEPTION 'Не указан ration_id для новой записи';
        END IF;
        
        PERFORM create_group_ration(p_group_id, p_ration_id, p_accounting);
        RETURN;
    END IF;

    -- Проверка существования нового рациона (если он указан)
    IF p_ration_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM rations WHERE id = p_ration_id) THEN
        RAISE EXCEPTION 'Рацион с ID % не найдена', p_ration_id;
    END IF;

    -- Проверка принадлежности к одной организации (если меняем рацион)
    IF p_ration_id IS NOT NULL AND p_ration_id <> v_current_ration_id THEN
        IF NOT EXISTS (
            SELECT 1 
            FROM groups g
            JOIN rations r ON g.organization_id = r.organization_id
            WHERE g.id = p_group_id AND r.id = p_ration_id
        ) THEN
            RAISE EXCEPTION 'Группа и новый рацион должны принадлежать одной организации';
        END IF;
    END IF;

    -- Обновляем запись
    UPDATE group_rations
    SET 
        ration_id = COALESCE(p_ration_id, ration_id),
        accounting = COALESCE(p_accounting, accounting)
    WHERE group_id = p_group_id;
END;
$$;


--
-- Name: update_medicine(uuid, text, text, text, text, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.update_medicine(p_id uuid, p_name text DEFAULT NULL::text, p_substance text DEFAULT NULL::text, p_drug_elimination_period text DEFAULT NULL::text, p_shelf_life text DEFAULT NULL::text, p_factory text DEFAULT NULL::text) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE medicine
    SET
        name = COALESCE(p_name, name),
        substance = COALESCE(p_substance, substance),
        drug_elimination_period = COALESCE(p_drug_elimination_period, drug_elimination_period),
        shelf_life = COALESCE(p_shelf_life, shelf_life),
        factory = COALESCE(p_factory, factory)
    WHERE id = p_id;

    RETURN FOUND; -- true, если запись найдена и обновлена
END;
$$;


--
-- Name: update_otel(text, date, text, text, text, text, text, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.update_otel(p_status text, p_calving_date date, p_calving_type text, p_complications text, p_veterinarian text, p_treatments text, p_pathology text, p_id uuid) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE reproduction
    SET status = p_status,
        calving_date = p_calving_date,
        calving_type = p_calving_type,
        complications = p_complications,
        veterinarian = p_veterinarian,
        treatments = p_treatments,
        pathology = p_pathology
    WHERE id = p_id;
END;
$$;


--
-- Name: update_pregnancy(uuid, date, text, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.update_pregnancy(p_pregnancy_id uuid, p_date date DEFAULT NULL::date, p_status text DEFAULT NULL::text, p_expected_calving_date date DEFAULT NULL::date) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE pregnancy
    SET 
        date = COALESCE(p_date, date),
        status = COALESCE(p_status, status),
        expected_calving_date = CASE 
            WHEN p_expected_calving_date IS NOT NULL THEN p_expected_calving_date
            ELSE expected_calving_date
        END
    WHERE id = p_pregnancy_id;
    
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Запись с pregnancy_id = % не найдена', p_pregnancy_id;
    END IF;
END;
$$;


--
-- Name: update_pregnancy2(uuid, date, text, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.update_pregnancy2(p_pregnancy_id uuid, p_date date DEFAULT NULL::date, p_status text DEFAULT NULL::text, p_expected_calving_date date DEFAULT NULL::date) RETURNS void
    LANGUAGE plpgsql
    AS $$

BEGIN
    UPDATE pregnancy
    SET 
        date = COALESCE(p_date, date),
        status = COALESCE(p_status, status),
        expected_calving_date = CASE 
            WHEN p_expected_calving_date IS NOT NULL THEN p_expected_calving_date
            ELSE expected_calving_date
        END
    WHERE id = p_pregnancy_id;
    
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Запись с pregnancy_id = % не найдена', p_pregnancy_id;
    END IF;
END;
$$;


--
-- Name: update_ration_components(uuid, uuid, jsonb); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.update_ration_components(p_ration_id uuid, p_organization_id uuid, p_components jsonb DEFAULT '[]'::jsonb) RETURNS void
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_component_record jsonb;
    v_component_id uuid;
    v_kg double precision;
    v_component_cost double precision;
BEGIN
    -- Validate required fields
    IF p_ration_id IS NULL THEN
        RAISE EXCEPTION 'Ration ID cannot be null';
    END IF;
    
    IF p_organization_id IS NULL THEN
        RAISE EXCEPTION 'Organization ID cannot be null';
    END IF;
    
    -- Check ration exists and belongs to organization
    IF NOT EXISTS (
        SELECT 1 FROM rations 
        WHERE id = p_ration_id AND organization_id = p_organization_id
    ) THEN
        RAISE EXCEPTION 'Ration with ID % does not exist or belongs to another organization', p_ration_id;
    END IF;
    
    -- Process components if provided
    IF p_components IS NOT NULL AND jsonb_array_length(p_components) > 0 THEN
        FOR v_component_record IN SELECT * FROM jsonb_array_elements(p_components)
        LOOP
            -- Extract component data
            v_component_id := (v_component_record->>'component_id')::uuid;
            v_kg := (v_component_record->>'kg')::double precision;
            
            -- Validate component exists and belongs to organization
            IF NOT EXISTS (
                SELECT 1 FROM components 
                WHERE id = v_component_id AND organization_id = p_organization_id
            ) THEN
                RAISE EXCEPTION 'Component with ID % does not exist or belongs to another organization', v_component_id;
            END IF;
            
            -- Get component cost
            SELECT cost INTO v_component_cost FROM components WHERE id = v_component_id;
            
            -- Update or insert component in ration
            INSERT INTO rations_components (
                ration_id,
                component_id,
                kg,
                cost
            ) VALUES (
                p_ration_id,
                v_component_id,
                v_kg,
                v_component_cost
            )
            ON CONFLICT (ration_id, component_id) 
            DO UPDATE SET
                kg = EXCLUDED.kg,
                cost = EXCLUDED.cost,
                created_at = CASE 
                    WHEN rations_components.kg != EXCLUDED.kg THEN CURRENT_TIMESTAMP
                    ELSE rations_components.created_at
                END;
        END LOOP;
    END IF;
END;
$$;


--
-- Name: update_ration_full(uuid, text, text, uuid, jsonb); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.update_ration_full(p_ration_id uuid, p_name text, p_description text DEFAULT NULL::text, p_organization_id uuid DEFAULT NULL::uuid, p_components jsonb DEFAULT NULL::jsonb) RETURNS void
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_current_org_id uuid;
    component_record JSONB;
BEGIN
    -- Проверка обязательных параметров
    IF p_ration_id IS NULL THEN
        RAISE EXCEPTION 'ID рациона не может быть NULL';
    END IF;

    IF p_name IS NULL OR p_name = '' THEN
        RAISE EXCEPTION 'Название рациона не может быть пустым';
    END IF;

    -- Получаем текущую организацию рациона
    SELECT organization_id INTO v_current_org_id 
    FROM rations 
    WHERE id = p_ration_id;
    
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Рацион с ID % не найден', p_ration_id;
    END IF;

    -- Проверяем новую организацию (если передана)
    IF p_organization_id IS NOT NULL AND p_organization_id <> v_current_org_id THEN
        IF NOT EXISTS (SELECT 1 FROM organizations WHERE id = p_organization_id) THEN
            RAISE EXCEPTION 'Организация с ID % не найдена', p_organization_id;
        END IF;
        v_current_org_id := p_organization_id;
    END IF;

    -- Обновляем основные данные рациона
    UPDATE rations
    SET 
        name = p_name,
        description = COALESCE(p_description, description),
        organization_id = v_current_org_id
    WHERE id = p_ration_id;

    -- Если переданы компоненты, обновляем их
    IF p_components IS NOT NULL THEN
        -- Удаляем старые компоненты рациона
        DELETE FROM rations_components WHERE ration_id = p_ration_id;
        
        -- Добавляем новые компоненты
        FOR component_record IN SELECT * FROM jsonb_array_elements(p_components)
        LOOP
            -- Проверяем существование компонента
            IF NOT EXISTS (
                SELECT 1 FROM components 
                WHERE id = (component_record->>'component_id')::UUID
                AND organization_id = v_current_org_id
            ) THEN
                RAISE EXCEPTION 'Компонент с ID % не найден или принадлежит другой организации', 
                              (component_record->>'component_id')::UUID;
            END IF;

            -- Проверяем обязательные поля компонента
            IF (component_record->>'component_id') IS NULL THEN
                RAISE EXCEPTION 'ID компонента не может быть NULL';
            END IF;
            
            IF (component_record->>'count') IS NULL THEN
                RAISE EXCEPTION 'Количество компонента не может быть NULL';
            END IF;

            -- Вставляем компонент в рацион
            INSERT INTO rations_components (
                ration_id,
                component_id,
                kg,
                cost,
                created_at
            ) VALUES (
                p_ration_id,
                (component_record->>'component_id')::UUID,
                (component_record->>'count')::FLOAT8,
                (component_record->>'cost')::FLOAT8,
                CURRENT_TIMESTAMP
            );
        END LOOP;
    END IF;
END;
$$;


--
-- Name: update_reproduction_record(uuid, text, date, text, text, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.update_reproduction_record(p_id uuid, p_status text, p_check_date date DEFAULT NULL::date, p_cow_condition text DEFAULT NULL::text, p_notes text DEFAULT NULL::text, p_expected_calving_date date DEFAULT NULL::date) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE reproduction
    SET status = p_status,
        check_date = p_check_date,
        cow_condition = p_cow_condition,
        notes = p_notes,
        expected_calving_date = p_expected_calving_date
    WHERE id = p_id;
END;
$$;


--
-- Name: update_user(uuid, uuid, text, text, uuid, text, character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.update_user(p_user_id uuid, p_organization_id uuid DEFAULT NULL::uuid, p_username text DEFAULT NULL::text, p_password text DEFAULT NULL::text, p_role_id uuid DEFAULT NULL::uuid, p_name text DEFAULT NULL::text, p_phone character varying DEFAULT NULL::character varying) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    -- Проверка обязательных полей
    IF p_user_id IS NULL THEN
        RAISE EXCEPTION 'User ID cannot be null';
    END IF;
    
    -- Проверка существования пользователя
    IF NOT EXISTS (SELECT 1 FROM users WHERE id = p_user_id) THEN
        RAISE EXCEPTION 'User with ID % does not exist', p_user_id;
    END IF;
    
    -- Проверка существования организации (если указана)
    IF p_organization_id IS NOT NULL AND NOT EXISTS (
        SELECT 1 FROM organizations WHERE id = p_organization_id
    ) THEN
        RAISE EXCEPTION 'Organization with ID % does not exist', p_organization_id;
    END IF;
    
    -- Проверка существования роли (если указана)
    IF p_role_id IS NOT NULL AND NOT EXISTS (
        SELECT 1 FROM roles WHERE id = p_role_id
    ) THEN
        RAISE EXCEPTION 'Role with ID % does not exist', p_role_id;
    END IF;
    
    -- Проверка уникальности имени пользователя (если указано)
    IF p_username IS NOT NULL AND EXISTS (
        SELECT 1 FROM users 
        WHERE username = p_username AND id != p_user_id
    ) THEN
        RAISE EXCEPTION 'Username % already exists', p_username;
    END IF;
    
    -- Обновление данных пользователя
    UPDATE users
    SET 
        organization_id = COALESCE(p_organization_id, organization_id),
        username = COALESCE(p_username, username),
        password = COALESCE(p_password, password),
        role_id = COALESCE(p_role_id, role_id),
        name = COALESCE(p_name, name),
        phone = COALESCE(p_phone, phone)
    WHERE 
        id = p_user_id;
END;
$$;


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: action_types; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.action_types (
    action_code character varying(50) NOT NULL,
    description text NOT NULL
);


--
-- Name: animal_identification; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.animal_identification (
    id uuid NOT NULL,
    animal_id uuid,
    field_id uuid,
    value text,
    created_date timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);


--
-- Name: animals; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.animals (
    id uuid NOT NULL,
    organization_id uuid,
    tag_number text NOT NULL,
    type text,
    breed text,
    mother_id uuid,
    status text,
    group_id uuid,
    origin text,
    origin_location text,
    birth_date date,
    date_of_receipt date,
    date_of_disposal date,
    reason_of_disposal text,
    consumption text,
    live_weight_at_disposal double precision,
    last_weigh_date date,
    last_weight_weight character varying,
    father_id jsonb
);


--
-- Name: barren; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.barren (
    animal_id uuid NOT NULL,
    is_barren boolean NOT NULL
);


--
-- Name: breeds; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.breeds (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    name text NOT NULL
);


--
-- Name: calvings; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.calvings (
    id uuid NOT NULL,
    cow_id uuid NOT NULL,
    date date NOT NULL,
    complication text NOT NULL,
    type text NOT NULL,
    veterinar text,
    treatments text,
    pathology text,
    calf_id uuid,
    insemination_id uuid
);


--
-- Name: components; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.components (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    organization_id uuid NOT NULL,
    name text NOT NULL,
    cost double precision,
    sv integer,
    sp integer,
    cep real,
    ndk integer
);


--
-- Name: daily_actions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.daily_actions (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    animal_id uuid NOT NULL,
    action_type text,
    action_subtype text,
    date date,
    performed_by text,
    result text,
    medicine text,
    dose text,
    notes text,
    next_action_date date,
    old_group_id uuid,
    new_group_id uuid,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    old_type text,
    new_type text,
    drug_elimination_period character varying
);


--
-- Name: debug_animals_bk; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.debug_animals_bk (
    id uuid NOT NULL,
    tag_number text
);


--
-- Name: demo; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.demo (
    cmd_output text
);


--
-- Name: feeding_record; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.feeding_record (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    event_date date NOT NULL,
    organization_id uuid NOT NULL,
    group_id uuid NOT NULL,
    animal_count bigint NOT NULL,
    group_ration_id uuid NOT NULL,
    total_kg double precision NOT NULL,
    total_kg_for_group double precision NOT NULL,
    fact_kg double precision NOT NULL,
    mark integer,
    feeding_mark integer,
    created_at timestamp with time zone DEFAULT now(),
    feeding_time text,
    feeding_coefficient double precision,
    CONSTRAINT feeding_record_feeding_coefficient_check CHECK (((feeding_coefficient >= (0)::double precision) AND (feeding_coefficient <= (1)::double precision))),
    CONSTRAINT feeding_record_feeding_time_check CHECK ((feeding_time = ANY (ARRAY['morning'::text, 'day'::text, 'night'::text])))
);


--
-- Name: group_rations; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.group_rations (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    group_id uuid NOT NULL,
    ration_id uuid NOT NULL,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    morning_feeding double precision,
    day_feeding double precision,
    night_feeding double precision
);


--
-- Name: group_types; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.group_types (
    id uuid NOT NULL,
    name text NOT NULL,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    organization_id uuid
);


--
-- Name: groups; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.groups (
    id uuid NOT NULL,
    organization_id uuid,
    name text NOT NULL,
    description text,
    location text,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    type_id uuid
);


--
-- Name: identification_fields; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.identification_fields (
    id uuid NOT NULL,
    field_name text,
    organization_id uuid
);


--
-- Name: insemination; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.insemination (
    id uuid NOT NULL,
    cow_id uuid NOT NULL,
    date date NOT NULL,
    insemination_type text NOT NULL,
    sperm_batch text,
    sperm_manufacturer text,
    bull_id jsonb,
    embryo_id text,
    embryo_manufacturer text,
    technician text,
    notes text,
    bull_name text,
    bull_id_jsonb_backup jsonb
);


--
-- Name: medicine; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.medicine (
    id uuid NOT NULL,
    organization_id uuid NOT NULL,
    name text NOT NULL,
    substance text,
    drug_elimination_period text,
    shelf_life text,
    factory text
);


--
-- Name: organizations; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.organizations (
    id uuid NOT NULL,
    name text NOT NULL,
    description text,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    inn character varying,
    ogrn character varying
);


--
-- Name: permissions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.permissions (
    id uuid NOT NULL,
    permission text NOT NULL,
    description text
);


--
-- Name: pregnancy; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.pregnancy (
    id uuid NOT NULL,
    cow_id uuid NOT NULL,
    date date NOT NULL,
    status text NOT NULL,
    expected_calving_date date,
    insemination_id uuid
);


--
-- Name: rations; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.rations (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    name text NOT NULL,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    organization_id uuid NOT NULL,
    description text
);


--
-- Name: rations_components; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.rations_components (
    ration_id uuid NOT NULL,
    component_id uuid NOT NULL,
    kg double precision NOT NULL,
    cost double precision,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT rations_components_count_check CHECK ((kg > (0)::double precision))
);


--
-- Name: research; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.research (
    id uuid NOT NULL,
    organization_id uuid NOT NULL,
    animal_id uuid NOT NULL,
    research_name text NOT NULL,
    material_type text NOT NULL,
    collection_date date NOT NULL,
    collected_by text,
    result text,
    notes text,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);


--
-- Name: roles; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.roles (
    id uuid NOT NULL,
    role text NOT NULL,
    description text
);


--
-- Name: roles_permissions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.roles_permissions (
    role_id uuid NOT NULL,
    permission_id uuid NOT NULL
);


--
-- Name: user_actions_audit; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.user_actions_audit (
    id uuid NOT NULL,
    user_id uuid NOT NULL,
    action_type character varying(50) NOT NULL,
    table_name character varying(100),
    record_id uuid,
    old_values jsonb,
    new_values jsonb,
    session_id character varying(100),
    action_timestamp timestamp with time zone DEFAULT now() NOT NULL,
    status character varying(20) DEFAULT 'success'::character varying,
    error_message text,
    additional_info jsonb
);


--
-- Name: users; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.users (
    id uuid NOT NULL,
    organization_id uuid,
    username text NOT NULL,
    password text NOT NULL,
    role_id uuid,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    name text,
    phone character varying,
    tg_id character varying
);


--
-- Name: weights; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.weights (
    id uuid NOT NULL,
    animal_id uuid NOT NULL,
    date date NOT NULL,
    weight double precision NOT NULL,
    method text,
    notes text
);


--
-- Name: action_types action_types_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.action_types
    ADD CONSTRAINT action_types_pkey PRIMARY KEY (action_code);


--
-- Name: animal_identification animal_identification_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.animal_identification
    ADD CONSTRAINT animal_identification_pkey PRIMARY KEY (id);


--
-- Name: animals animals_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.animals
    ADD CONSTRAINT animals_pkey PRIMARY KEY (id);


--
-- Name: barren barren_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.barren
    ADD CONSTRAINT barren_pkey PRIMARY KEY (animal_id);


--
-- Name: breeds breeds_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.breeds
    ADD CONSTRAINT breeds_pkey PRIMARY KEY (id);


--
-- Name: calvings calvings_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.calvings
    ADD CONSTRAINT calvings_pkey PRIMARY KEY (id);


--
-- Name: components components_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.components
    ADD CONSTRAINT components_pkey PRIMARY KEY (id);


--
-- Name: daily_actions daily_actions_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.daily_actions
    ADD CONSTRAINT daily_actions_pkey PRIMARY KEY (id);


--
-- Name: debug_animals_bk debug_animals_bk_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.debug_animals_bk
    ADD CONSTRAINT debug_animals_bk_pkey PRIMARY KEY (id);


--
-- Name: feeding_record feeding_record_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.feeding_record
    ADD CONSTRAINT feeding_record_pkey PRIMARY KEY (id);


--
-- Name: group_rations group_rations_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.group_rations
    ADD CONSTRAINT group_rations_pkey PRIMARY KEY (id);


--
-- Name: group_types group_types_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.group_types
    ADD CONSTRAINT group_types_pkey PRIMARY KEY (id);


--
-- Name: groups groups_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.groups
    ADD CONSTRAINT groups_pkey PRIMARY KEY (id);


--
-- Name: identification_fields identification_fields_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.identification_fields
    ADD CONSTRAINT identification_fields_pkey PRIMARY KEY (id);


--
-- Name: insemination insemination_pk; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.insemination
    ADD CONSTRAINT insemination_pk PRIMARY KEY (id);


--
-- Name: medicine medicine_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.medicine
    ADD CONSTRAINT medicine_pkey PRIMARY KEY (id);


--
-- Name: organizations organizations_name_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organizations
    ADD CONSTRAINT organizations_name_key UNIQUE (name);


--
-- Name: organizations organizations_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organizations
    ADD CONSTRAINT organizations_pkey PRIMARY KEY (id);


--
-- Name: permissions permissions_permission_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.permissions
    ADD CONSTRAINT permissions_permission_key UNIQUE (permission);


--
-- Name: permissions permissions_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.permissions
    ADD CONSTRAINT permissions_pkey PRIMARY KEY (id);


--
-- Name: rations_components pk_rations_components; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.rations_components
    ADD CONSTRAINT pk_rations_components PRIMARY KEY (ration_id, component_id);


--
-- Name: pregnancy pregnancy_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pregnancy
    ADD CONSTRAINT pregnancy_pkey PRIMARY KEY (id);


--
-- Name: rations rations_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.rations
    ADD CONSTRAINT rations_pkey PRIMARY KEY (id);


--
-- Name: research research_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.research
    ADD CONSTRAINT research_pkey PRIMARY KEY (id);


--
-- Name: roles roles_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.roles
    ADD CONSTRAINT roles_pkey PRIMARY KEY (id);


--
-- Name: roles roles_role_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.roles
    ADD CONSTRAINT roles_role_key UNIQUE (role);


--
-- Name: user_actions_audit user_actions_audit_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_actions_audit
    ADD CONSTRAINT user_actions_audit_pkey PRIMARY KEY (id);


--
-- Name: users users_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_pkey PRIMARY KEY (id);


--
-- Name: users users_unique; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_unique UNIQUE (username);


--
-- Name: weights weights_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.weights
    ADD CONSTRAINT weights_pkey PRIMARY KEY (id);


--
-- Name: idx_components_organization_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_components_organization_id ON public.components USING btree (organization_id);


--
-- Name: idx_feeding_record_date; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_feeding_record_date ON public.feeding_record USING btree (event_date);


--
-- Name: idx_feeding_record_group; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_feeding_record_group ON public.feeding_record USING btree (group_id);


--
-- Name: idx_feeding_record_org; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_feeding_record_org ON public.feeding_record USING btree (organization_id);


--
-- Name: idx_rations_components_component_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_rations_components_component_id ON public.rations_components USING btree (component_id);


--
-- Name: animal_identification animal_identification_animal_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.animal_identification
    ADD CONSTRAINT animal_identification_animal_id_fkey FOREIGN KEY (animal_id) REFERENCES public.animals(id);


--
-- Name: animal_identification animal_identification_field_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.animal_identification
    ADD CONSTRAINT animal_identification_field_id_fkey FOREIGN KEY (field_id) REFERENCES public.identification_fields(id);


--
-- Name: animals animals_group_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.animals
    ADD CONSTRAINT animals_group_id_fkey FOREIGN KEY (group_id) REFERENCES public.groups(id);


--
-- Name: animals animals_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.animals
    ADD CONSTRAINT animals_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id);


--
-- Name: calvings calvings_cow_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.calvings
    ADD CONSTRAINT calvings_cow_id_fkey FOREIGN KEY (cow_id) REFERENCES public.animals(id);


--
-- Name: daily_actions daily_actions_animal_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.daily_actions
    ADD CONSTRAINT daily_actions_animal_id_fkey FOREIGN KEY (animal_id) REFERENCES public.animals(id);


--
-- Name: daily_actions daily_actions_new_group_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.daily_actions
    ADD CONSTRAINT daily_actions_new_group_id_fkey FOREIGN KEY (new_group_id) REFERENCES public.groups(id);


--
-- Name: daily_actions daily_actions_old_group_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.daily_actions
    ADD CONSTRAINT daily_actions_old_group_id_fkey FOREIGN KEY (old_group_id) REFERENCES public.groups(id);


--
-- Name: user_actions_audit fk_action_type; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_actions_audit
    ADD CONSTRAINT fk_action_type FOREIGN KEY (action_type) REFERENCES public.action_types(action_code);


--
-- Name: barren fk_barren_animal; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.barren
    ADD CONSTRAINT fk_barren_animal FOREIGN KEY (animal_id) REFERENCES public.animals(id) ON UPDATE CASCADE ON DELETE CASCADE;


--
-- Name: calvings fk_calvings_insemination; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.calvings
    ADD CONSTRAINT fk_calvings_insemination FOREIGN KEY (insemination_id) REFERENCES public.insemination(id);


--
-- Name: rations_components fk_component; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.rations_components
    ADD CONSTRAINT fk_component FOREIGN KEY (component_id) REFERENCES public.components(id) ON DELETE RESTRICT;


--
-- Name: feeding_record fk_group; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.feeding_record
    ADD CONSTRAINT fk_group FOREIGN KEY (group_id) REFERENCES public.groups(id);


--
-- Name: groups fk_group_type; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.groups
    ADD CONSTRAINT fk_group_type FOREIGN KEY (type_id) REFERENCES public.group_types(id);


--
-- Name: medicine fk_medicine_organization; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.medicine
    ADD CONSTRAINT fk_medicine_organization FOREIGN KEY (organization_id) REFERENCES public.organizations(id) ON UPDATE CASCADE ON DELETE RESTRICT;


--
-- Name: components fk_organization; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.components
    ADD CONSTRAINT fk_organization FOREIGN KEY (organization_id) REFERENCES public.organizations(id) ON DELETE CASCADE;


--
-- Name: pregnancy fk_pregnancy_insemination; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pregnancy
    ADD CONSTRAINT fk_pregnancy_insemination FOREIGN KEY (insemination_id) REFERENCES public.insemination(id);


--
-- Name: feeding_record fk_ration; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.feeding_record
    ADD CONSTRAINT fk_ration FOREIGN KEY (group_ration_id) REFERENCES public.rations(id);


--
-- Name: rations_components fk_ration; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.rations_components
    ADD CONSTRAINT fk_ration FOREIGN KEY (ration_id) REFERENCES public.rations(id) ON DELETE CASCADE;


--
-- Name: rations fk_ration_organization; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.rations
    ADD CONSTRAINT fk_ration_organization FOREIGN KEY (organization_id) REFERENCES public.organizations(id) ON DELETE CASCADE;


--
-- Name: user_actions_audit fk_user_id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_actions_audit
    ADD CONSTRAINT fk_user_id FOREIGN KEY (user_id) REFERENCES public.users(id);


--
-- Name: group_rations group_rations_group_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.group_rations
    ADD CONSTRAINT group_rations_group_id_fkey FOREIGN KEY (group_id) REFERENCES public.groups(id) ON DELETE CASCADE;


--
-- Name: group_rations group_rations_ration_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.group_rations
    ADD CONSTRAINT group_rations_ration_id_fkey FOREIGN KEY (ration_id) REFERENCES public.rations(id) ON DELETE CASCADE;


--
-- Name: groups groups_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.groups
    ADD CONSTRAINT groups_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id);


--
-- Name: identification_fields identification_fields_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.identification_fields
    ADD CONSTRAINT identification_fields_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id);


--
-- Name: insemination insemination_cow_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.insemination
    ADD CONSTRAINT insemination_cow_id_fkey FOREIGN KEY (cow_id) REFERENCES public.animals(id);


--
-- Name: pregnancy pregnancy_cow_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pregnancy
    ADD CONSTRAINT pregnancy_cow_id_fkey FOREIGN KEY (cow_id) REFERENCES public.animals(id);


--
-- Name: research research_animal_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.research
    ADD CONSTRAINT research_animal_id_fkey FOREIGN KEY (animal_id) REFERENCES public.animals(id);


--
-- Name: research research_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.research
    ADD CONSTRAINT research_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id);


--
-- Name: roles_permissions roles_permissions_permission_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.roles_permissions
    ADD CONSTRAINT roles_permissions_permission_id_fkey FOREIGN KEY (permission_id) REFERENCES public.permissions(id);


--
-- Name: roles_permissions roles_permissions_role_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.roles_permissions
    ADD CONSTRAINT roles_permissions_role_id_fkey FOREIGN KEY (role_id) REFERENCES public.roles(id);


--
-- Name: users users_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id);


--
-- Name: users users_role_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_role_id_fkey FOREIGN KEY (role_id) REFERENCES public.roles(id);


--
-- Name: weights weights_animal_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.weights
    ADD CONSTRAINT weights_animal_id_fkey FOREIGN KEY (animal_id) REFERENCES public.animals(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict dt0XcqbFtIseFV8GRvBhDGfeImd6lxrBjIf0RjRdEbdl5g0QaeTga9t1dqBbz9O
