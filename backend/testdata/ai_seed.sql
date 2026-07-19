-- Reproducible AI/integration fixture data for Cattle Track.
-- Safe to rerun: deletes only rows belonging to the fixed test organization.

BEGIN;

DO $$
DECLARE
    org_id uuid := '90000000-0000-0000-0000-000000000001';
BEGIN
    DELETE FROM pregnancy
    WHERE cow_id IN (SELECT id FROM animals WHERE organization_id = org_id);

    DELETE FROM insemination
    WHERE cow_id IN (SELECT id FROM animals WHERE organization_id = org_id);

    DELETE FROM daily_actions
    WHERE animal_id IN (SELECT id FROM animals WHERE organization_id = org_id);

    DELETE FROM weights
    WHERE animal_id IN (SELECT id FROM animals WHERE organization_id = org_id);

    DELETE FROM medicine WHERE organization_id = org_id;
    DELETE FROM animals WHERE organization_id = org_id;
    DELETE FROM groups WHERE organization_id = org_id;
    DELETE FROM group_types WHERE organization_id = org_id;
    DELETE FROM organizations WHERE id = org_id;
END $$;

INSERT INTO organizations (id, name, description, inn, ogrn, created_at)
VALUES (
    '90000000-0000-0000-0000-000000000001',
    'AI Test Organization',
    'Reproducible fixtures for deterministic agent and ASR tests',
    '0000000000',
    '0000000000000',
    TIMESTAMP '2026-07-10 00:00:00'
);

INSERT INTO group_types (id, organization_id, name, created_at)
VALUES
    ('90000000-0000-0000-0001-000000000001', '90000000-0000-0000-0000-000000000001', 'Тестовый тип: коровы', TIMESTAMP '2026-07-10 00:00:00'),
    ('90000000-0000-0000-0001-000000000002', '90000000-0000-0000-0000-000000000001', 'Тестовый тип: молодняк', TIMESTAMP '2026-07-10 00:00:00');

INSERT INTO groups (id, organization_id, name, type_id, description, location, created_at)
VALUES
    ('90000000-0000-0000-0002-000000000001', '90000000-0000-0000-0000-000000000001', 'Основное стадо', '90000000-0000-0000-0001-000000000001', 'Base group for AI fixtures', 'Корпус 1', TIMESTAMP '2026-07-10 00:00:00'),
    ('90000000-0000-0000-0002-000000000002', '90000000-0000-0000-0000-000000000001', 'Молодняк', '90000000-0000-0000-0001-000000000002', 'Target group for move actions', 'Корпус 2', TIMESTAMP '2026-07-10 00:00:00'),
    ('90000000-0000-0000-0002-000000000003', '90000000-0000-0000-0000-000000000001', 'Карантин', '90000000-0000-0000-0001-000000000001', 'Target group for daily action tests', 'Корпус 3', TIMESTAMP '2026-07-10 00:00:00');

INSERT INTO animals (
    id, organization_id, tag_number, type, breed, mother_id, father_id, status,
    group_id, origin, origin_location, birth_date, date_of_receipt,
    date_of_disposal, reason_of_disposal, consumption, live_weight_at_disposal,
    last_weigh_date, last_weight_weight
)
VALUES
    -- 1 exact match fixture.
    ('90000000-0000-0000-0003-000000000001', '90000000-0000-0000-0000-000000000001', '1432', 'Корова', 'Голштинская', NULL, NULL, 'Активное', '90000000-0000-0000-0002-000000000001', 'Рождение', 'AI seed', DATE '2024-01-15', DATE '2024-01-15', NULL, NULL, 'Молочное', NULL, DATE '2026-07-09', '420'),
    ('90000000-0000-0000-0003-000000000002', '90000000-0000-0000-0000-000000000001', '77', 'Бык', 'Голштинская', NULL, NULL, 'Активное', '90000000-0000-0000-0002-000000000001', 'Покупка', 'AI seed', DATE '2023-05-20', DATE '2024-02-01', NULL, NULL, 'Племенное', NULL, DATE '2026-07-09', '690'),
    ('90000000-0000-0000-0003-000000000003', '90000000-0000-0000-0000-000000000001', '981', 'Корова', 'Айрширская', NULL, NULL, 'Активное', '90000000-0000-0000-0002-000000000002', 'Рождение', 'AI seed', DATE '2024-03-03', DATE '2024-03-03', NULL, NULL, 'Молочное', NULL, DATE '2026-07-02', '301'),
    ('90000000-0000-0000-0003-000000000004', '90000000-0000-0000-0000-000000000001', 'A-17', 'Корова', 'Джерсейская', NULL, NULL, 'Активное', '90000000-0000-0000-0002-000000000002', 'Рождение', 'AI seed', DATE '2024-09-01', DATE '2024-09-01', NULL, NULL, 'Молочное', NULL, DATE '2026-07-01', '255'),

    -- 2-5 matches fixture: tag 523 appears three times in the same organization.
    ('90000000-0000-0000-0003-000000000101', '90000000-0000-0000-0000-000000000001', '523', 'Корова', 'Голштинская', NULL, NULL, 'Активное', '90000000-0000-0000-0002-000000000001', 'Рождение', 'AI seed', DATE '2022-02-10', DATE '2022-02-10', NULL, NULL, 'Молочное', NULL, DATE '2026-07-09', '518.5'),
    ('90000000-0000-0000-0003-000000000102', '90000000-0000-0000-0000-000000000001', '523', 'Корова', 'Голштинская', NULL, NULL, 'Активное', '90000000-0000-0000-0002-000000000002', 'Покупка', 'AI seed', DATE '2023-08-11', DATE '2024-01-12', NULL, NULL, 'Молочное', NULL, DATE '2026-07-09', '412'),
    ('90000000-0000-0000-0003-000000000103', '90000000-0000-0000-0000-000000000001', '523', 'Бык', 'Голштинская', NULL, NULL, 'Активное', '90000000-0000-0000-0002-000000000003', 'Покупка', 'AI seed', DATE '2021-06-01', DATE '2024-01-12', NULL, NULL, 'Племенное', NULL, DATE '2026-07-09', '730'),

    -- >5 matches fixture: tag 777 appears six times.
    ('90000000-0000-0000-0003-000000000201', '90000000-0000-0000-0000-000000000001', '777', 'Корова', 'Голштинская', NULL, NULL, 'Активное', '90000000-0000-0000-0002-000000000001', 'Рождение', 'AI seed', DATE '2020-01-01', DATE '2020-01-01', NULL, NULL, 'Молочное', NULL, NULL, NULL),
    ('90000000-0000-0000-0003-000000000202', '90000000-0000-0000-0000-000000000001', '777', 'Корова', 'Голштинская', NULL, NULL, 'Активное', '90000000-0000-0000-0002-000000000001', 'Рождение', 'AI seed', DATE '2020-02-01', DATE '2020-02-01', NULL, NULL, 'Молочное', NULL, NULL, NULL),
    ('90000000-0000-0000-0003-000000000203', '90000000-0000-0000-0000-000000000001', '777', 'Корова', 'Голштинская', NULL, NULL, 'Активное', '90000000-0000-0000-0002-000000000002', 'Рождение', 'AI seed', DATE '2020-03-01', DATE '2020-03-01', NULL, NULL, 'Молочное', NULL, NULL, NULL),
    ('90000000-0000-0000-0003-000000000204', '90000000-0000-0000-0000-000000000001', '777', 'Бык', 'Голштинская', NULL, NULL, 'Активное', '90000000-0000-0000-0002-000000000002', 'Покупка', 'AI seed', DATE '2020-04-01', DATE '2021-01-01', NULL, NULL, 'Племенное', NULL, NULL, NULL),
    ('90000000-0000-0000-0003-000000000205', '90000000-0000-0000-0000-000000000001', '777', 'Корова', 'Айрширская', NULL, NULL, 'Активное', '90000000-0000-0000-0002-000000000003', 'Рождение', 'AI seed', DATE '2020-05-01', DATE '2020-05-01', NULL, NULL, 'Молочное', NULL, NULL, NULL),
    ('90000000-0000-0000-0003-000000000206', '90000000-0000-0000-0000-000000000001', '777', 'Корова', 'Джерсейская', NULL, NULL, 'Активное', '90000000-0000-0000-0002-000000000003', 'Рождение', 'AI seed', DATE '2020-06-01', DATE '2020-06-01', NULL, NULL, 'Молочное', NULL, NULL, NULL);

INSERT INTO weights (id, animal_id, date, weight, method, notes)
VALUES
    ('90000000-0000-0000-0004-000000000001', '90000000-0000-0000-0003-000000000001', DATE '2026-07-09', '420', 'Ручное взвешивание', 'Duplicate fixture for create_weight'),
    ('90000000-0000-0000-0004-000000000002', '90000000-0000-0000-0003-000000000002', DATE '2026-07-09', '690', 'Весовая станция', 'Bull weight fixture'),
    ('90000000-0000-0000-0004-000000000003', '90000000-0000-0000-0003-000000000003', DATE '2026-07-02', '301', 'Расчетный вес', 'Date fixture'),
    ('90000000-0000-0000-0004-000000000004', '90000000-0000-0000-0003-000000000101', DATE '2026-07-09', '518.5', 'Весовая станция', 'Ambiguous tag fixture');

INSERT INTO medicine (id, organization_id, name, substance, drug_elimination_period, shelf_life, factory)
VALUES
    ('90000000-0000-0000-0005-000000000001', '90000000-0000-0000-0000-000000000001', 'Ивермек', 'Ивермектин', '28 дней', '24 месяца', 'AI seed factory'),
    ('90000000-0000-0000-0005-000000000002', '90000000-0000-0000-0000-000000000001', 'Вакцина Б19', 'Brucella abortus S19', '0 дней', '12 месяцев', 'AI seed factory');

INSERT INTO daily_actions (
    id, animal_id, action_type, action_subtype, performed_by, result, medicine, dose,
    notes, old_group_id, new_group_id, date, next_action_date, created_at
)
VALUES
    ('90000000-0000-0000-0006-000000000001', '90000000-0000-0000-0003-000000000001', 'Лечение', 'Лечение', 'AI Seed', 'ОК', 'Ивермек', '10 мл', 'Duplicate daily action fixture', NULL, NULL, DATE '2026-07-09', NULL, TIMESTAMP '2026-07-10 00:00:00'),
    ('90000000-0000-0000-0006-000000000002', '90000000-0000-0000-0003-000000000003', 'Перевод', 'Перевод', 'AI Seed', 'ОК', NULL, NULL, 'Move fixture', '90000000-0000-0000-0002-000000000002', '90000000-0000-0000-0002-000000000001', DATE '2026-07-08', NULL, TIMESTAMP '2026-07-10 00:00:00');

INSERT INTO insemination (
    id, cow_id, bull_id, date, insemination_type, sperm_batch, sperm_manufacturer,
    embryo_id, embryo_manufacturer, technician, notes
)
VALUES
    ('90000000-0000-0000-0007-000000000001', '90000000-0000-0000-0003-000000000001', '["90000000-0000-0000-0003-000000000002"]'::jsonb, TIMESTAMP '2026-07-09 00:00:00', 'Естественное', NULL, NULL, NULL, NULL, 'AI Seed', 'Duplicate insemination fixture'),
    ('90000000-0000-0000-0007-000000000002', '90000000-0000-0000-0003-000000000004', NULL, TIMESTAMP '2026-07-01 00:00:00', 'Искусственное', 'B12', 'AI seed manufacturer', NULL, NULL, 'Иванов', 'Artificial insemination fixture');

INSERT INTO pregnancy (id, cow_id, date, status, expected_calving_date, insemination_id)
VALUES
    ('90000000-0000-0000-0008-000000000001', '90000000-0000-0000-0003-000000000001', TIMESTAMP '2026-07-20 00:00:00', 'Стельная', TIMESTAMP '2027-04-15 00:00:00', '90000000-0000-0000-0007-000000000001');

COMMIT;
