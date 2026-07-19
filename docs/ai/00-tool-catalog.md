# Каталог инструментов агента (единый источник истины)

Все остальные документы ссылаются на этот. Здесь перечислены ВСЕ инструменты агента, покрывающие функционал системы. Администрирование (организации, сотрудники, роли, приглашения, аутентификация) в тулы НЕ выносится.

Актуально для текущей реализации `cattle-track-fullstack` после правок безопасности и производительности. Мультитенантность: каждый тул работает строго в рамках `OrganizationId` из контекста сессии.

## Обозначения
- **R** — чтение (выполняется сразу, без подтверждения)
- **W** — запись (только через propose-then-commit, см. `01-architecture-spec.md`)
- **[MVP]** — реализуется и бенчмаркается в первую очередь (2 недели)
- **⚠** — есть скрытое правило/ловушка, обязательная к воспроизведению

---

## 1. Животные: поиск, карточка, справочники

| Тул | R/W | Аргументы | Backend | Примечание |
|---|---|---|---|---|
| `find_animal` [MVP] | R | `tag` | `GET /api/animals` (фильтр по tagNumber) | ⚠ точное совпадение бирки в рамках организации; 0/2+ → disambiguation |
| `get_animal_card` [MVP] | R | `animal_id` | `GET /api/AnimalCard/animal/detail` | тип, статус, группа, `MotherId`, `FatherJson`, даты |
| `get_animal_parents` [MVP] | R | `tag` | `.../animal/parent` | ⚠ многошаговый: бирка → родители → бирки родителей |
| `get_animal_events` | R | `animal_id, date_from?, date_to?` | `.../animal/actions` | вся история событий животного |
| `list_animals` | R | `filters{group?, type?, status?, breed?, birth_from?, birth_to?, origin?}` | `GET /api/animals` | для голоса резюмировать список |
| `count_animals` | R | те же `filters` | `.../pagination-info` | одно число |
| `get_livestock_summary` | R | — | `.../main-info` | общее активное поголовье + разбивка по типам (бык/корова/бычок/тёлка) |
| `list_barren_cows` | R | — | `.../barren/ids` | список яловых |
| `list_breeds` / `list_origins` / `list_origin_places` | R | — | `.../breed`, `/origins`, `/places-of-origin` | справочники-резолверы |
| `register_animal` | W | `tag, type, breed, birth_date?, origin?, group_id?, mother_id?, father_ids?` | `POST /api/animals/registration` | регистрация нового животного |
| `update_animal` | W | `animal_id, {tag?, type?, breed?, group_id?, status?, ...}` | `PUT /api/animals` / `PATCH /api/AnimalCard/animal/update-animal-card` | правка карточки (кроме родословной) |

## 2. Взвешивания

| Тул | R/W | Аргументы | Backend | Примечание |
|---|---|---|---|---|
| `get_weight_history` [MVP] | R | `animal_id` | `GET /api/Weights/{id}` | список взвешиваний + СУП |
| `get_last_weight` | R | `animal_id` | `.../{id}/last` | одно число |
| `get_weight_statistics` | R | `animal_id` | `.../{id}/statistics` | `MeanSUP/MinSUP/MaxSUP` — voice-friendly |
| `create_weight` [MVP] | W | `tag, weight, date, method` | `POST /api/Weights` | ⚠ backend НЕ проверяет вес>0 — sanity-check добавляем в валидаторе |

## 3. Ежедневные действия: ветеринария, движение поголовья

Один эндпоинт `POST /api/DailyActions` (принимает массив DTO в одной транзакции) покрывает несколько типов через поле `Type`. Это и есть механизм батча.

| Тул | R/W | Аргументы | Backend | Примечание |
|---|---|---|---|---|
| `create_daily_action` [MVP] | W | `tags[], type, subtype?, medicine?, dose?, date, next_date?, old_group_id?, new_group_id?` | `POST /api/DailyActions` | ⚠ `type` — закрытый enum (§ ниже); зависящие от типа поля |
| `create_daily_action_with_medicine` | W | `tags[], type, medicine, dose, withdrawal_period?, date` | `.../with-medicine/batch` | декартово произведение животные×действия |
| `list_daily_actions` | R | `filters, page` | `GET /api/DailyActions` | история действий |
| `count_daily_actions` | R | `filters` | `.../pagination-info` | одно число |
| `delete_daily_action` | W | `action_id` | `DELETE /api/DailyActions` | |
| `list_medicines` | R | — | `.../medicine` | справочник препаратов |
| `create_medicine` | W | `name, substance?, withdrawal_period?, shelf_life?, factory?` | `POST /api/DailyActions/medicine` | ⚠ дубль имени → понятная ошибка контроллера |
| `update_medicine` | W | `id, {...}` | `PUT .../medicine` | |
| `delete_medicine` | W | `medicine_id` | `DELETE .../medicine/{id}` | |

**Закрытый enum `DailyAction.Type` и его каскадные эффекты (обязательно воспроизвести в валидаторе):**
- `"Осмотры"`, `"Обработка"`, `"Вакцинации и обработки"`, `"Лечение"` → обычная запись.
- `"Перевод"` → дополнительно двигает животное: `UpdateAnimal(group = new_group_id)`.
- `"Выбытие"` → `status="Выбывшее"`, `subtype` = причина выбытия.
- `"Присвоение номеров"` → `subtype` = ИМЯ поля идентификации, `identification_value` = значение.
- `"Изменение половозрастной группы"` → использует `old_type/new_type` и меняет `animal.type`.
- `"Исследования"` → пишет в ОТДЕЛЬНУЮ таблицу research, не в daily_actions.
- Поле `medicine` — либо свободный текст, либо строковый Guid препарата (двойное назначение).

## 4. Репродукция

| Тул | R/W | Аргументы | Backend | Примечание |
|---|---|---|---|---|
| `list_cows` / `list_bulls` | R | — | `GET /api/Reproductive/cow`, `/bull` | |
| `get_pregnancies_to_check` [MVP] | R | — | `.../pregnancy` | коровы, ожидающие диагностики стельности |
| `get_cows_to_calve` | R | — | `.../calving` | стельные, ожидающие отёла |
| `create_insemination` [MVP] | W | `cow_tags[], type, bull_tags?, sperm_batch?, technician?, date` | `POST .../insemination` (или `/inseminations/batch`) | ⚠ каскадно создаёт `Pregnancy` со статусом "Подлежит проверке"; `type` выбирает SQL-функцию |
| `create_pregnancy_diagnosis` | W | `cow_tag, status, date` | `POST .../pregnancy` | ⚠ обновляет существующую стельность (нужен `InseminationId`); `ExpectedCalvingDate` пересчитывается сервером (+285); `status`∈{Стельная,Яловая}; Яловая→пометка яловой; Стельная+Тёлка→Нетель |
| `create_calving` | W | `cow_tag, type, calf_tag?, weight?, method?` | `POST .../calving` | ⚠ `type`∈{Живой,Мертворожденный}; каскад: создаёт телёнка + взвешивание + меняет статус/тип матери; `method` = тип нового телёнка |

## 5. Кормление

⚠ Общая ловушка модуля: конверсии процент↔доля раскиданы по коду в разные стороны — тул всегда работает в процентах 0–100, конверсия внутри реализации.

| Тул | R/W | Аргументы | Backend | Примечание |
|---|---|---|---|---|
| `list_feed_components` | R | — | `GET /api/feeding/component` | флаг `InRation` важен для удаления |
| `list_rations` / `get_rations_with_components` | R | — | `.../ration`, `/rations-with-components` | состав + стоимость + группы |
| `get_group_feeding_stats` | R | — | `.../group-stats` | голов, рацион, стоимость/голову, нутриенты |
| `get_groups_with_rations` | R | — | `.../group-rations` | у кого нет рациона |
| `get_feeding_plan_for_date` | R | `date?` | `.../main/plan-to-date` | ⚠ смешивает факт и план; группы без рациона отфильтровываются |
| `get_group_feeding_cost` | R | `group_id, period(month\|year)` | `.../group-ration-cost[-yearly]/graph` | сумма руб. за период |
| `get_group_consumption` | R | `group_id` | `.../group-analysis/graph` | кг за 30 дней |
| `get_group_nutrition` | R | `group_id` | `.../group-ration-nutrition/graph` | СВ/СП/ЧЭП/НДК |
| `create_feed_component` | W | `name, cost?, sv?, sp?, cep?, ndk?` | `POST .../component` | ⚠ дубль имени → 400 |
| `update_feed_component` | W | `id, {...}` | `PATCH .../component` | |
| `delete_feed_component` | W | `component_id` | `DELETE .../component` | ⚠ если в рационе — БД вернёт "не найден" (обманчиво); тул сам проверяет `InRation` заранее |
| `create_ration` | W | `name, description?, components[], group_id?` | `POST .../ration` | ⚠ с `group_id`: откат если у группы уже есть рацион; проценты не задаёт → нужен отдельный `assign_ration_to_group` |
| `update_ration_components` | W | `ration_id, components[]` | `PUT .../ration/{id}` | ⚠ имя/описание рациона не меняет (переименовать нельзя) |
| `assign_ration_to_group` | W | `group_id, ration_id, morning%, day%, night%` | `POST .../assign-ration-to-group` | ⚠ проценты 0–100; upsert без ошибки; сумма=100% backend НЕ проверяет — валидируем сами |
| `record_feeding` | W | `group_id, date, feeding_time, fact_kg, coefficient%, mark` | `POST .../record-feeding` | ⚠ сначала брать контекст из `get_feeding_plan_for_date`, не собирать поля "от себя"; батч не транзакционный |

Не выносится в тулы / roadmap: `ration-summary` endpoint есть в API, но помечен в коде как нерабочий; удаление/отвязка рациона от группы; переименование рациона; ручной пересчёт плана.

## 6. Группы / инфраструктура (операционная, не админская)

| Тул | R/W | Аргументы | Backend | Примечание |
|---|---|---|---|---|
| `list_groups` [MVP] | R | — | `GET /api/groups` | название, тип, локация |
| `list_group_types` | R | — | `.../type` | |
| `list_identification_fields` / `list_identification_values` | R | `identification_id?, filters?` | `.../identification[/values]` | |
| `create_group` | W | `name, type_id, location?, description?` | `POST /api/groups` | имя уникально в организации |
| `edit_group` | W | `id, {name?, type_id?, location?, description?}` | `PUT /api/groups` | |
| `delete_group` | W | `group_id` | `DELETE /api/groups` | org-принадлежность проверяется backend; блокируется если в группе есть животные |
| `create_group_type` / `delete_group_type` | W | `name` / `type_id` | `.../type` | delete проверяет org-принадлежность backend; блокируется если есть группы с данным типом |
| `create_identification_field` / `delete_identification_field` | W | `name` / `id` | `.../identification` | макс. 6 полей; delete проверяет org-принадлежность backend |

## 7. Аналитика / KPI

⚠ `StatisticsChartsController` фильтрует ТОЛЬКО по датам, НЕ по группе/типу. Групповой разбивки нет — либо отвечать "по всему хозяйству", либо расширять backend (roadmap). Групповой СУП/привес нигде не считается.

| Тул | R/W | Аргументы | Backend | Voice? |
|---|---|---|---|---|
| `get_calvings_stats` | R | `date_from, date_to` | `.../charts/calvings` | да (сумма за период) |
| `get_pregnancy_stats` | R | `date_from, date_to` | `.../charts/pregnancy` | да |
| `get_vaccinations_stats` | R | `date_from, date_to` | `.../charts/vaccinations` | да |
| `get_blood_tests_stats` | R | `date_from, date_to` | `.../charts/blood-tests` | да (в т.ч. % положительных по диагнозу) |
| `get_birth_weight_stats` | R | `date_from, date_to` | `.../charts/birth-weight` | да (Avg/Max по полу) |
| `get_daily_weight_gain` | R | `date_from, date_to` | `.../charts/daily-weight-gain` | нет — динамика, возвращать ссылку на график |
| `get_weight_at_12_months` | R | `date_from, date_to` | `.../charts/weight-at-12-months` | нет — график |

Правило: вопрос со словами "сколько всего / средний / максимум" → агрегировать в число; "динамика / как менялось / по дням" → вернуть ссылку на раздел с графиком, не диктовать ряд.

---

## Сводка
Всего инструментов: **~55** (≈35 read + ≈20 write). Исключено из тулов: аутентификация, организации, сотрудники, роли, приглашения (администрирование).

**MVP-подмножество для сборки и бенчмарка (2 недели):** отмеченные `[MVP]` — 6 read + 3 write. Остальное реализуется по мере наличия времени в том же паттерне; в бенчмарк защиты входит MVP-подмножество (см. `02-research-plan.md`). Это осознанное разделение «полный функциональный охват задокументирован» vs «доведено и измерено», а не урезание амбиции.
