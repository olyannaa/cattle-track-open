#!/usr/bin/env python3
import json
from pathlib import Path
from typing import Any, Optional

ROOT = Path(__file__).resolve().parents[1]
TOOL_DIR = ROOT / "datasets" / "tool_calling"
FAULT_DIR = ROOT / "datasets" / "fault_injection"

TODAY = "2026-07-09"
YESTERDAY = "2026-07-08"
LAST_WEEK = "2026-07-02"

ANIMAL_IDS = {
    "1432": "11111111-1111-4111-8111-111111111432",
    "523": "11111111-1111-4111-8111-111111110523",
    "524": "11111111-1111-4111-8111-111111110524",
    "981": "11111111-1111-4111-8111-111111110981",
    "A-17": "11111111-1111-4111-8111-111111110017",
    "77": "11111111-1111-4111-8111-111111110077",
}

GROUP_IDS = {
    "Основное стадо": "22222222-2222-4222-8222-222222220001",
    "Молодняк": "22222222-2222-4222-8222-222222220002",
    "Производители": "22222222-2222-4222-8222-222222220003",
    "Карантин": "22222222-2222-4222-8222-222222220004",
}


def write_jsonl(path: Path, rows: list[dict[str, Any]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8") as f:
        for row in rows:
            f.write(json.dumps(row, ensure_ascii=False, sort_keys=True) + "\n")


def split_for(i: int, train_until: int, dev_until: int) -> str:
    if i <= train_until:
        return "train"
    if i <= dev_until:
        return "dev"
    return "test"


def call(name: str, arguments: dict[str, Any]) -> dict[str, Any]:
    return {"name": name, "arguments": arguments}


def record(
    idx: str,
    split: str,
    stratum: str,
    utterance: str,
    tool_calls: list[dict[str, Any]],
    expected_result: dict[str, Any],
    expected_db_state_after_commit: Optional[dict[str, Any]] = None,
    extra: Optional[dict[str, Any]] = None,
) -> dict[str, Any]:
    value = {
        "id": idx,
        "split": split,
        "stratum": stratum,
        "language": "ru",
        "utterance": utterance,
        "golden": {
            "tool_calls": tool_calls,
            "expected_result": expected_result,
        },
        "expected_db_state_after_commit": expected_db_state_after_commit,
        "review": {"status": "pending", "notes": ""},
    }
    if extra:
        value.update(extra)
    return value


def result(kind: str, notes: str, **extra: Any) -> dict[str, Any]:
    value = {"kind": kind, "notes": notes}
    value.update(extra)
    return value


def weight_args(tag: str, weight: float, date: str, key: str, method: str = "Ручное взвешивание", notes: Optional[str] = None) -> dict[str, Any]:
    args = {
        "schema_version": "v1",
        "idempotency_key": key,
        "tag": tag,
        "weight": weight,
        "date": date,
        "method": method,
    }
    if notes:
        args["notes"] = notes
    return args


def daily_args(rid: str, items: list[dict[str, Any]]) -> dict[str, Any]:
    return {
        "schema_version": "v1",
        "batch_idempotency_key": f"dataset:{rid}:batch",
        "items": items,
    }


def daily_item(rid: str, n: int, tag: str, action_type: str, **extra: Any) -> dict[str, Any]:
    item = {
        "idempotency_key": f"dataset:{rid}:item-{n}",
        "tag": tag,
        "type": action_type,
        "date": TODAY,
    }
    item.update(extra)
    return item


def insemination_args(rid: str, items: list[dict[str, Any]]) -> dict[str, Any]:
    return {
        "schema_version": "v1",
        "batch_idempotency_key": f"dataset:{rid}:batch",
        "items": items,
    }


def insemination_item(rid: str, n: int, cow_tags: list[str], insemination_type: str, **extra: Any) -> dict[str, Any]:
    item = {
        "idempotency_key": f"dataset:{rid}:item-{n}",
        "cow_tags": cow_tags,
        "date": TODAY,
        "insemination_type": insemination_type,
    }
    item.update(extra)
    return item


def single_read() -> list[dict[str, Any]]:
    examples = [
        ("найди корову с биркой 1432", [call("find_animal", {"schema_version": "v1", "tag": "1432"})], "exact_lookup"),
        ("покажи, есть ли у нас животное 523", [call("find_animal", {"schema_version": "v1", "tag": "523"})], "exact_lookup"),
        ("открой карточку 1432", [call("get_animal_card", {"schema_version": "v1", "animal_id": ANIMAL_IDS["1432"]})], "animal_card"),
        ("посмотри карточку A-17", [call("get_animal_card", {"schema_version": "v1", "animal_id": ANIMAL_IDS["A-17"]})], "animal_card"),
        ("кто родители у 981?", [call("get_animal_parents", {"schema_version": "v1", "tag": "981"})], "parents"),
        ("покажи мать и отца у бирки 1432", [call("get_animal_parents", {"schema_version": "v1", "tag": "1432"})], "parents"),
        ("покажи веса по 1432", [call("get_weight_history", {"schema_version": "v1", "animal_id": ANIMAL_IDS["1432"], "limit": 20})], "weight_history"),
        ("какие последние взвешивания у 523?", [call("get_weight_history", {"schema_version": "v1", "animal_id": ANIMAL_IDS["523"], "limit": 10})], "weight_history"),
        ("кого сегодня нужно проверить на стельность?", [call("get_pregnancies_to_check", {"schema_version": "v1", "due_before": TODAY})], "pregnancy_check"),
        ("покажи список на проверку беременности", [call("get_pregnancies_to_check", {"schema_version": "v1"})], "pregnancy_check"),
        ("какие группы есть в хозяйстве?", [call("list_groups", {"schema_version": "v1", "include_empty": True})], "groups"),
        ("покажи только непустые группы", [call("list_groups", {"schema_version": "v1", "include_empty": False})], "groups"),
        ("найди быка 77", [call("find_animal", {"schema_version": "v1", "tag": "77"})], "exact_lookup"),
        ("найди животное пятьсот двадцать три", [call("find_animal", {"schema_version": "v1", "tag": "523"})], "exact_lookup"),
        ("открой карточку быка с биркой 77", [call("get_animal_card", {"schema_version": "v1", "animal_id": ANIMAL_IDS["77"]})], "animal_card"),
        ("посмотри родителей у 523", [call("get_animal_parents", {"schema_version": "v1", "tag": "523"})], "parents"),
        ("дай историю веса по A-17", [call("get_weight_history", {"schema_version": "v1", "animal_id": ANIMAL_IDS["A-17"], "limit": 20})], "weight_history"),
        ("есть кто просрочен по проверке стельности?", [call("get_pregnancies_to_check", {"schema_version": "v1", "due_before": TODAY})], "pregnancy_check"),
        ("покажи все группы, даже пустые", [call("list_groups", {"schema_version": "v1", "include_empty": True})], "groups"),
        ("какие группы сейчас используются?", [call("list_groups", {"schema_version": "v1", "include_empty": False})], "groups"),
        ("найди бирку 000523", [call("find_animal", {"schema_version": "v1", "tag": "000523"})], "exact_lookup"),
        ("открой карточку животного 524", [call("get_animal_card", {"schema_version": "v1", "animal_id": ANIMAL_IDS["524"]})], "animal_card"),
        ("кто мать у телки 524?", [call("get_animal_parents", {"schema_version": "v1", "tag": "524"})], "parents"),
        ("покажи историю взвешиваний 981", [call("get_weight_history", {"schema_version": "v1", "animal_id": ANIMAL_IDS["981"], "limit": 20})], "weight_history"),
        ("кого надо проверить по беременности?", [call("get_pregnancies_to_check", {"schema_version": "v1"})], "pregnancy_check"),
        ("дай список групп", [call("list_groups", {"schema_version": "v1", "include_empty": True})], "groups"),
        ("найди животное 1432, пожалуйста", [call("find_animal", {"schema_version": "v1", "tag": "1432"})], "exact_lookup"),
        ("карточку по номеру A-17 открой", [call("get_animal_card", {"schema_version": "v1", "animal_id": ANIMAL_IDS["A-17"]})], "animal_card"),
        ("родители у быка 77 есть?", [call("get_animal_parents", {"schema_version": "v1", "tag": "77"})], "parents"),
        ("сколько раньше весила 523?", [call("get_weight_history", {"schema_version": "v1", "animal_id": ANIMAL_IDS["523"], "limit": 20})], "weight_history"),
        ("покажи коров на проверку стельности", [call("get_pregnancies_to_check", {"schema_version": "v1"})], "pregnancy_check"),
        ("группы покажи коротко", [call("list_groups", {"schema_version": "v1", "include_empty": True})], "groups"),
        ("найди бирку 14 32", [call("find_animal", {"schema_version": "v1", "tag": "1432"})], "exact_lookup"),
        ("открой 981", [call("get_animal_card", {"schema_version": "v1", "animal_id": ANIMAL_IDS["981"]})], "animal_card"),
        ("посмотри родителей у A-17", [call("get_animal_parents", {"schema_version": "v1", "tag": "A-17"})], "parents"),
        ("какие веса были у 77?", [call("get_weight_history", {"schema_version": "v1", "animal_id": ANIMAL_IDS["77"], "limit": 20})], "weight_history"),
        ("список стельных на проверку", [call("get_pregnancies_to_check", {"schema_version": "v1"})], "pregnancy_check"),
        ("покажи группы в организации", [call("list_groups", {"schema_version": "v1", "include_empty": True})], "groups"),
    ]
    return [
        record(f"ctai-sr-{i:03d}", split_for(i, 27, 33), "single-read", u, calls, result(kind, "Один read-запрос без commit. Если сущность не найдена или неоднозначна, нужно показать уточнение."))
        for i, (u, calls, kind) in enumerate(examples, 1)
    ]


def no_tool() -> list[dict[str, Any]]:
    examples = [
        ("создай новое животное с биркой 1601", "animal_registration"),
        ("измени кличку у 1432 на Зорька", "animal_update"),
        ("удали вчерашний осмотр у 523", "delete_daily_action"),
        ("выгрузи список животных в таблицу", "animal_export"),
        ("добавь новую группу Сухостой", "group_create"),
        ("измени описание группы Молодняк", "group_update"),
        ("покажи статистику привесов за месяц", "statistics"),
        ("назначь рацион группе Основное стадо", "feeding"),
        ("покажи расход кормов за неделю", "feeding_analysis"),
        ("загрузи фото для животного 1432", "file_upload"),
        ("добавь отел у коровы 523", "calving_write"),
        ("обнови результат проверки беременности у 981", "pregnancy_update"),
    ]
    rows = []
    for i, (utterance, capability) in enumerate(examples, 1):
        rows.append(record(
            f"ctai-nt-{i:03d}",
            split_for(i, 8, 10),
            "no-tool",
            utterance,
            [],
            result("no_tool", "Запрос относится к реальной системе, но для него нет подходящей AI tool schema.", capability=capability),
        ))
    return rows


def multi_hop() -> list[dict[str, Any]]:
    specs = [
        ("найди 1432 и открой карточку", "1432", "get_animal_card"),
        ("сначала найди 523, потом покажи родителей", "523", "get_animal_parents"),
        ("найди 981 и покажи его веса", "981", "get_weight_history"),
        ("открой карточку быка 77", "77", "get_animal_card"),
        ("найди корову 1432, хочу посмотреть родителей", "1432", "get_animal_parents"),
        ("найди A-17 и дай историю веса", "A-17", "get_weight_history"),
        ("посмотри карточку телки 524", "524", "get_animal_card"),
        ("по бирке 981 покажи мать и отца", "981", "get_animal_parents"),
        ("по животному 523 покажи взвешивания", "523", "get_weight_history"),
        ("что за 000523, открой карточку", "000523", "get_animal_card"),
        ("найди 524 и покажи карточку", "524", "get_animal_card"),
        ("родители у бычка 77", "77", "get_animal_parents"),
        ("найди корову 1432 и веса по ней", "1432", "get_weight_history"),
        ("найди A-17, потом карточку", "A-17", "get_animal_card"),
        ("мать и отец у 524 кто?", "524", "get_animal_parents"),
        ("найди 981 и покажи историю веса", "981", "get_weight_history"),
        ("открой карточку пятьсот двадцать три", "523", "get_animal_card"),
        ("посмотри родителей у A 17", "A-17", "get_animal_parents"),
        ("найди 77 и покажи веса", "77", "get_weight_history"),
        ("карточка коровы 1432 нужна", "1432", "get_animal_card"),
        ("по 981 нужны родители", "981", "get_animal_parents"),
        ("найди 524 и покажи весовую динамику", "524", "get_weight_history"),
        ("что за животное 523, покажи карточку", "523", "get_animal_card"),
        ("найти 1432 и кто у нее родители", "1432", "get_animal_parents"),
    ]
    rows = []
    for i, (utterance, tag, second_tool) in enumerate(specs, 1):
        calls = [call("find_animal", {"schema_version": "v1", "tag": tag})]
        animal_id = ANIMAL_IDS.get(tag, ANIMAL_IDS["523"])
        if second_tool == "get_animal_card":
            calls.append(call("get_animal_card", {"schema_version": "v1", "animal_id": animal_id}))
        elif second_tool == "get_weight_history":
            calls.append(call("get_weight_history", {"schema_version": "v1", "animal_id": animal_id, "limit": 20}))
        else:
            calls.append(call("get_animal_parents", {"schema_version": "v1", "tag": tag}))
        rows.append(record(
            f"ctai-mh-{i:03d}",
            split_for(i, 16, 20),
            "multi-hop-read",
            utterance,
            calls,
            result("multi_hop", "Сначала resolve животного по бирке, затем read-запрос."),
        ))
    return rows


def single_write() -> list[dict[str, Any]]:
    rows = []
    weights = [
        ("внеси вес 420 кг для 1432 за сегодня", "1432", 420, TODAY, "preview_then_confirm"),
        ("запиши 523 вчера 518.5 кг с весовой станции", "523", 518.5, YESTERDAY, "preview_then_confirm", "Автоматическая весовая станция"),
        ("добавь расчетный вес 301 кг для 981 за 2 июля", "981", 301, LAST_WEEK, "preview_then_confirm", "Расчетный метод"),
        ("бык 77 сегодня 690 кг, внеси", "77", 690, TODAY, "preview_then_confirm"),
        ("A-17 весит 255, покажи черновик", "A-17", 255, TODAY, "preview_then_confirm"),
        ("524 сегодня 199 кг", "524", 199, TODAY, "preview_then_confirm"),
        ("внеси 421 кг для 1432, но на подтверждении исправь на 420", "1432", 421, TODAY, "preview_then_user_edit_then_confirm"),
        ("создай взвешивание 523 520 кг, сначала покажи мне", "523", 520, TODAY, "preview_then_confirm"),
        ("981 вес триста пять сегодня", "981", 305, TODAY, "preview_then_confirm"),
        ("у 77 вчера весовая показала 688", "77", 688, YESTERDAY, "preview_then_confirm", "Автоматическая весовая станция"),
        ("A-17 запиши вес 257 за сегодня", "A-17", 257, TODAY, "preview_then_confirm"),
        ("для 524 поставь расчетный вес 201 за 2 июля", "524", 201, LAST_WEEK, "preview_then_confirm", "Расчетный метод"),
        ("1432 шестого числа была 419.7 кг", "1432", 419.7, "2026-07-06", "preview_then_confirm"),
        ("внеси вес 519 кг для 523", "523", 519, TODAY, "preview_then_cancel"),
    ]
    for i, item in enumerate(weights, 1):
        utterance, tag, weight, date, flow = item[:5]
        method = item[5] if len(item) > 5 else "Ручное взвешивание"
        rid = f"ctai-sw-{i:03d}"
        final_weight = 420 if flow == "preview_then_user_edit_then_confirm" else weight
        db_state = {"table": "weights", "insert": {"tag": tag, "date": date, "weight": final_weight, "method": method}}
        if flow == "preview_then_cancel":
            db_state = {"table": "weights", "insert": None, "reason": "пользователь отменил черновик до сохранения"}
        rows.append(record(
            rid,
            split_for(i, 27, 33),
            "single-write",
            utterance,
            [call("create_weight", weight_args(tag, weight, date, f"dataset:{rid}", method))],
            result("draft_preview", "Write-тул создает черновик; commit только после подтверждения человеком.", interaction_flow=flow),
            db_state,
        ))

    daily_specs = [
        ("переведи 1432 в основное стадо", "1432", "Перевод", {"new_group_id": GROUP_IDS["Основное стадо"]}),
        ("отметь выбытие 523, причина падеж", "523", "Выбытие", {"subtype": "Падеж"}),
        ("присвой 981 RFID 643001234567890", "981", "Присвоение номеров", {"subtype": "RFID", "identification_value": "643001234567890"}),
        ("измени 77 из бычка в быка", "77", "Изменение половозрастной группы", {"old_type": "Бычок", "new_type": "Бык"}),
        ("запиши A-17 анализ крови отрицательный", "A-17", "Исследования", {"research_name": "Анализ крови", "material_type": "Кровь", "result": "Отрицательный"}),
        ("524 лечение ивермек 10 мл сегодня", "524", "Лечение", {"subtype": "Лечение", "medicine": "Ивермек", "dose": "10 мл"}),
        ("обработка 1432 от паразитов ивермеком", "1432", "Обработка", {"subtype": "Дегельминтизация", "medicine": "Ивермек"}),
        ("осмотр 523 без замечаний", "523", "Осмотры", {"result": "Без замечаний"}),
        ("вакцинация 981 от бруцеллеза", "981", "Вакцинации и обработки", {"subtype": "Вакцинация", "medicine": "Вакцина бруцеллез"}),
        ("быка 77 переведи в производителей", "77", "Перевод", {"new_group_id": GROUP_IDS["Производители"]}),
        ("отметь выбытие A-17, причина продажа", "A-17", "Выбытие", {"subtype": "Продажа"}, "preview_then_cancel"),
        ("запиши лечение 524 ивермек 10 мл, потом поправлю дозу на 8 мл", "524", "Лечение", {"subtype": "Лечение", "medicine": "Ивермек", "dose": "10 мл"}, "preview_then_user_edit_then_confirm"),
    ]
    offset = len(rows)
    for j, spec in enumerate(daily_specs, 1):
        utterance, tag, action_type, extra = spec[:4]
        flow = spec[4] if len(spec) > 4 else "preview_then_confirm"
        i = offset + j
        rid = f"ctai-sw-{i:03d}"
        item = daily_item(rid, 1, tag, action_type, **extra)
        db_state: dict[str, Any] = {"table": "daily_actions_or_research", "insert_or_cascade": dict(item)}
        if flow == "preview_then_cancel":
            db_state = {"insert": None, "reason": "пользователь отменил черновик до сохранения"}
        if flow == "preview_then_user_edit_then_confirm":
            db_state["insert_or_cascade"]["dose"] = "8 мл"
        rows.append(record(
            rid,
            split_for(i, 27, 33),
            "single-write",
            utterance,
            [call("create_daily_action", daily_args(rid, [item]))],
            result("draft_preview", "Черновик daily action; каскадные эффекты показываются в предпросмотре.", interaction_flow=flow),
            db_state,
        ))

    repro_specs = [
        ("осемени корову 1432 искусственно партией A17", ["1432"], "Искусственное", {"sperm_batch": "A17", "bull_name": "Бык 77"}),
        ("запиши естественное осеменение 523 быком 77", ["523"], "Естественное", {"bull_tags": ["77"]}),
        ("внеси эмбрион E-55 для 981", ["981"], "Эмбрион", {"embryo_id": "E-55"}),
        ("1432 осеменить искусственно, техник Иванов", ["1432"], "Искусственное", {"technician": "Иванов"}),
        ("корову 524 осемени естественно быком 77", ["524"], "Естественное", {"bull_tags": ["77"]}),
        ("A-17 искусственное осеменение, производитель Бурый", ["A-17"], "Искусственное", {"bull_name": "Бурый"}),
        ("внеси эмбрион E-77 для 523", ["523"], "Эмбрион", {"embryo_id": "E-77"}, "preview_then_cancel"),
        ("981 искусственное осеменение сегодня", ["981"], "Искусственное", {}),
        ("осемени 524 искусственно партией B12", ["524"], "Искусственное", {"sperm_batch": "B12"}),
        ("естественное осеменение 1432 быком 77", ["1432"], "Естественное", {"bull_tags": ["77"]}),
        ("эмбрион E-99 для A-17 сегодня", ["A-17"], "Эмбрион", {"embryo_id": "E-99"}),
        ("внеси осеменение 981 искусственное", ["981"], "Искусственное", {}),
    ]
    offset = len(rows)
    for j, spec in enumerate(repro_specs, 1):
        utterance, cow_tags, typ, extra = spec[:4]
        flow = spec[4] if len(spec) > 4 else "preview_then_confirm"
        i = offset + j
        rid = f"ctai-sw-{i:03d}"
        item = insemination_item(rid, 1, cow_tags, typ, **extra)
        db_state: dict[str, Any] = {"tables": ["insemination", "pregnancy"], "insert": dict(item), "cascade": "создать беременность"}
        if flow == "preview_then_cancel":
            db_state = {"insert": None, "reason": "пользователь отменил черновик до сохранения"}
        rows.append(record(
            rid,
            split_for(i, 27, 33),
            "single-write",
            utterance,
            [call("create_insemination", insemination_args(rid, [item]))],
            result("draft_preview", "Черновик осеменения; предпросмотр обязан показать каскад создания беременности.", interaction_flow=flow),
            db_state,
        ))
    return rows


def batch_write() -> list[dict[str, Any]]:
    examples = [
        ("переведи 1432, 523 и 981 в основное стадо", "daily", [daily_item("ctai-bw-001", 1, "1432", "Перевод", new_group_id=GROUP_IDS["Основное стадо"]), daily_item("ctai-bw-001", 2, "523", "Перевод", new_group_id=GROUP_IDS["Основное стадо"]), daily_item("ctai-bw-001", 3, "981", "Перевод", new_group_id=GROUP_IDS["Основное стадо"])], "all_resolved"),
        ("запиши лечение ивермек 10 мл для 1432 и 524", "daily", [daily_item("ctai-bw-002", 1, "1432", "Лечение", subtype="Лечение", medicine="Ивермек", dose="10 мл"), daily_item("ctai-bw-002", 2, "524", "Лечение", subtype="Лечение", medicine="Ивермек", dose="10 мл")], "all_resolved"),
        ("выбытие падеж для 77 и 9999", "daily", [daily_item("ctai-bw-003", 1, "77", "Выбытие", subtype="Падеж"), daily_item("ctai-bw-003", 2, "9999", "Выбытие", subtype="Падеж")], "partial_not_found"),
        ("переведи 523 и 1432 в молодняк", "daily", [daily_item("ctai-bw-004", 1, "523", "Перевод", new_group_id=GROUP_IDS["Молодняк"]), daily_item("ctai-bw-004", 2, "1432", "Перевод", new_group_id=GROUP_IDS["Молодняк"])], "ambiguous_duplicate_tag"),
        ("осемени 1432, 524 и 9999 искусственно", "repro", [insemination_item("ctai-bw-005", 1, ["1432", "524", "9999"], "Искусственное")], "partial_not_found"),
        ("естественное осеменение 1432 и 981 быком 77", "repro", [insemination_item("ctai-bw-006", 1, ["1432", "981"], "Естественное", bull_tags=["77"])], "all_resolved"),
        ("присвой RFID 643 всем: 1432, 523, 524", "daily", [daily_item("ctai-bw-007", 1, "1432", "Присвоение номеров", subtype="RFID", identification_value="643"), daily_item("ctai-bw-007", 2, "523", "Присвоение номеров", subtype="RFID", identification_value="643"), daily_item("ctai-bw-007", 3, "524", "Присвоение номеров", subtype="RFID", identification_value="643")], "all_resolved"),
        ("сделай анализ крови для 1432, 981 и A-17", "daily", [daily_item("ctai-bw-008", 1, "1432", "Исследования", research_name="Анализ крови", material_type="Кровь"), daily_item("ctai-bw-008", 2, "981", "Исследования", research_name="Анализ крови", material_type="Кровь"), daily_item("ctai-bw-008", 3, "A-17", "Исследования", research_name="Анализ крови", material_type="Кровь")], "all_resolved"),
        ("переведи 1432 в группу Карантин", "daily", [daily_item("ctai-bw-009", 1, "1432", "Перевод", new_group_id=GROUP_IDS["Карантин"])], "all_resolved"),
        ("выбытие продажа для 1432 и 524", "daily", [daily_item("ctai-bw-010", 1, "1432", "Выбытие", subtype="Продажа"), daily_item("ctai-bw-010", 2, "524", "Выбытие", subtype="Продажа")], "all_resolved"),
        ("осмотр без замечаний для 1432 и 524", "daily", [daily_item("ctai-bw-011", 1, "1432", "Осмотры", result="Без замечаний"), daily_item("ctai-bw-011", 2, "524", "Осмотры", result="Без замечаний")], "all_resolved"),
        ("лечение 1432, 0000 и 981 ивермек 10 мл", "daily", [daily_item("ctai-bw-012", 1, "1432", "Лечение", subtype="Лечение", medicine="Ивермек", dose="10 мл"), daily_item("ctai-bw-012", 2, "0000", "Лечение", subtype="Лечение", medicine="Ивермек", dose="10 мл"), daily_item("ctai-bw-012", 3, "981", "Лечение", subtype="Лечение", medicine="Ивермек", dose="10 мл")], "partial_not_found"),
        ("осемени 523 и 524 естественно быком 77", "repro", [insemination_item("ctai-bw-013", 1, ["523", "524"], "Естественное", bull_tags=["77"])], "ambiguous_duplicate_tag"),
        ("переведи 77, 981 и A-17 в производителей", "daily", [daily_item("ctai-bw-014", 1, "77", "Перевод", new_group_id=GROUP_IDS["Производители"]), daily_item("ctai-bw-014", 2, "981", "Перевод", new_group_id=GROUP_IDS["Производители"]), daily_item("ctai-bw-014", 3, "A-17", "Перевод", new_group_id=GROUP_IDS["Производители"])], "all_resolved"),
        ("запиши вакцинацию Бруцел для 1432, 523, 524", "daily", [daily_item("ctai-bw-015", 1, "1432", "Вакцинации и обработки", subtype="Вакцинация", medicine="Бруцел"), daily_item("ctai-bw-015", 2, "523", "Вакцинации и обработки", subtype="Вакцинация", medicine="Бруцел"), daily_item("ctai-bw-015", 3, "524", "Вакцинации и обработки", subtype="Вакцинация", medicine="Бруцел")], "all_resolved"),
        ("исследование молока для 1432 и 523", "daily", [daily_item("ctai-bw-016", 1, "1432", "Исследования", research_name="Исследование молока", material_type="Молоко"), daily_item("ctai-bw-016", 2, "523", "Исследования", research_name="Исследование молока", material_type="Молоко")], "ambiguous_duplicate_tag"),
        ("выбытие продажа 77 и 981, покажи отчет по каждому", "daily", [daily_item("ctai-bw-017", 1, "77", "Выбытие", subtype="Продажа"), daily_item("ctai-bw-017", 2, "981", "Выбытие", subtype="Продажа")], "all_resolved"),
        ("осемени 1432 и 524 искусственно", "repro", [insemination_item("ctai-bw-018", 1, ["1432", "524"], "Искусственное")], "all_resolved"),
        ("переведи 1432 и 523 в молодняк", "daily", [daily_item("ctai-bw-019", 1, "1432", "Перевод", new_group_id=GROUP_IDS["Молодняк"]), daily_item("ctai-bw-019", 2, "523", "Перевод", new_group_id=GROUP_IDS["Молодняк"])], "ambiguous_duplicate_tag"),
        ("лечение сразу 1432, 523, 981 дозировка 10 мл ивермек", "daily", [daily_item("ctai-bw-020", 1, "1432", "Лечение", subtype="Лечение", medicine="Ивермек", dose="10 мл"), daily_item("ctai-bw-020", 2, "523", "Лечение", subtype="Лечение", medicine="Ивермек", dose="10 мл"), daily_item("ctai-bw-020", 3, "981", "Лечение", subtype="Лечение", medicine="Ивермек", dose="10 мл")], "ambiguous_duplicate_tag"),
        ("эмбрион E-9 для 524 и E-10 для 981", "repro", [insemination_item("ctai-bw-021", 1, ["524"], "Эмбрион", embryo_id="E-9"), insemination_item("ctai-bw-021", 2, ["981"], "Эмбрион", embryo_id="E-10")], "all_resolved"),
        ("переведи 9999 и 8888 в основное стадо", "daily", [daily_item("ctai-bw-022", 1, "9999", "Перевод", new_group_id=GROUP_IDS["Основное стадо"]), daily_item("ctai-bw-022", 2, "8888", "Перевод", new_group_id=GROUP_IDS["Основное стадо"])], "all_not_found"),
        ("проставь осмотр без замечаний для 1432, 523 и 524", "daily", [daily_item("ctai-bw-023", 1, "1432", "Осмотры", result="Без замечаний"), daily_item("ctai-bw-023", 2, "523", "Осмотры", result="Без замечаний"), daily_item("ctai-bw-023", 3, "524", "Осмотры", result="Без замечаний")], "ambiguous_duplicate_tag"),
    ]
    rows = []
    for i, (utterance, kind, payload, expectation) in enumerate(examples, 1):
        rid = f"ctai-bw-{i:03d}"
        if kind == "daily":
            calls = [call("create_daily_action", daily_args(rid, payload))]
        elif kind == "repro":
            calls = [call("create_insemination", insemination_args(rid, payload))]
        else:
            calls = [call("create_weight", weight_args(tag, 420, TODAY, f"dataset:{rid}:{tag}")) for tag in payload]
        rows.append(record(
            rid,
            split_for(i, 15, 19),
            "batch-write",
            utterance,
            calls,
            result("partial_preview", "Batch-предпросмотр показывает, что будет сохранено, что требует уточнения и что не найдено.", expectation=expectation),
            {"mode": "per_item_report", "expectation": expectation},
        ))
    return rows


def adversarial() -> list[dict[str, Any]]:
    examples = [
        ("осемени быка 523", [], "wrong_role_or_ambiguous"),
        ("внеси вес минус 20 для 1432", [], "invalid_weight"),
        ("переведи 523", [], "missing_group"),
        ("покажи карточку 523", [call("find_animal", {"schema_version": "v1", "tag": "523"})], "ambiguous_duplicate_tag"),
        ("запиши 1432 завтра вес 420", [call("create_weight", weight_args("1432", 420, "2026-07-10", "dataset:ctai-adv-005"))], "future_date"),
        ("удали всех выбывших", [], "no_tool_destructive"),
        ("переведи 1432 в 1432", [], "nonsense_group"),
        ("осеменить 523 естественно", [], "missing_bull"),
        ("вес 1432 примерно четыреста", [], "uncertain_number"),
        ("лечи 523 чем-нибудь", [], "missing_medicine_or_subtype"),
        ("карточка Зорьки", [], "nickname_without_resolver"),
        ("переведи корову из основного в молодняк, бирку не помню", [], "missing_animal_ref"),
        ("проставь выбытие 1432", [], "missing_disposal_reason"),
        ("сделай исследование 981", [], "missing_research_name"),
        ("осемени 523, а если их две, выбери старшую", [], "must_not_auto_choose_candidate"),
        ("найди примерно 522 или 523", [], "fuzzy_db_forbidden"),
        ("взвесь 1432 сегодня и сразу сохрани без подтверждения", [call("create_weight", weight_args("1432", 420, TODAY, "dataset:ctai-adv-017"))], "must_require_human_confirm"),
        ("запиши вес 1432 420 без черновика", [call("create_weight", weight_args("1432", 420, TODAY, "dataset:ctai-adv-018"))], "must_require_preview"),
    ]
    rows = []
    for i, (utterance, calls, expectation) in enumerate(examples, 1):
        rows.append(record(
            f"ctai-adv-{i:03d}",
            split_for(i, 12, 15),
            "adversarial-ambiguous",
            utterance,
            calls,
            result("clarification_or_validation_error", "Нельзя commit'ить небезопасный или неоднозначный запрос без уточнения.", expectation=expectation),
        ))
    return rows


def fault_injection() -> list[dict[str, Any]]:
    rows = []
    faults: list[tuple[str, str, dict[str, Any], list[str]]] = []
    weight_texts = [
        "запиши вес 420 кг для 1432",
        "внеси взвешивание 1432 за сегодня",
        "добавь ручной вес 1432",
        "поставь вес по бирке 1432",
    ]
    daily_texts = [
        "переведи 1432 в основное стадо",
        "проставь выбытие 1432",
        "добавь исследование для 1432",
        "присвой номер животному 1432",
    ]
    repro_texts = [
        "осемени 1432 сегодня",
        "запиши естественное осеменение 1432",
        "добавь эмбриональное осеменение 1432",
        "внеси осеменение коровы 1432",
    ]
    for i in range(1, 14):
        args = weight_args(
            "1432",
            [-5, 0, 4000, 420][i % 4],
            "2026-07-10" if i % 3 == 0 else "2024-01-01",
            f"dataset:ctai-fi-{i:03d}",
            "__unknown" if i % 2 == 0 else "Ручное взвешивание",
        )
        faults.append((weight_texts[i % len(weight_texts)], "create_weight", args, ["AI-VAL-WEIGHT-RANGE", "AI-VAL-DATE-FUTURE", "AI-VAL-ENUM-KNOWN"]))
    for i in range(14, 27):
        typ = ["Перевод", "Выбытие", "Исследования", "Присвоение номеров", "Изменение половозрастной группы", "__unknown"][i % 6]
        args = daily_args(f"ctai-fi-{i:03d}", [{"idempotency_key": f"dataset:ctai-fi-{i:03d}:item-1", "tag": "1432", "type": typ, "date": TODAY}])
        faults.append((daily_texts[i % len(daily_texts)], "create_daily_action", args, ["AI-VAL-DAILY-CASCADE", "AI-VAL-ENUM-KNOWN"]))
    for i in range(27, 38):
        typ = ["Естественное", "Эмбрион", "__unknown", "Искусственное"][i % 4]
        args = insemination_args(f"ctai-fi-{i:03d}", [{"idempotency_key": f"dataset:ctai-fi-{i:03d}:item-1", "cow_tags": ["1432"], "date": "2026-07-10" if i % 2 else TODAY, "insemination_type": typ}])
        faults.append((repro_texts[i % len(repro_texts)], "create_insemination", args, ["AI-VAL-DATE-FUTURE", "AI-VAL-DAILY-CASCADE", "AI-VAL-ENUM-KNOWN"]))

    for i, (utterance, tool, args, expected_rules) in enumerate(faults, 1):
        rows.append(record(
            f"ctai-fi-{i:03d}",
            split_for(i, 22, 29),
            "fault-injection",
            utterance,
            [call(tool, args)],
            result("validation_error", "Валидатор должен поймать испорченные аргументы до создания commit-черновика.", expected_rules=expected_rules),
            None,
            {"mutation": {"kind": "invalid_argument_mutation", "description": "намеренно испорченные golden-аргументы для оценки валидатора", "expected_rules": expected_rules}},
        ))
    return rows


def main() -> None:
    tool_rows = single_read() + no_tool() + multi_hop() + single_write() + batch_write() + adversarial()
    fault_rows = fault_injection()

    for split in ["train", "dev", "test"]:
        write_jsonl(TOOL_DIR / f"{split}.jsonl", [r for r in tool_rows if r["split"] == split])
        write_jsonl(FAULT_DIR / f"{split}.jsonl", [r for r in fault_rows if r["split"] == split])

    all_rows = tool_rows + fault_rows
    summary = {
        "total": len(all_rows),
        "tool_calling_total": len(tool_rows),
        "fault_injection_total": len(fault_rows),
        "by_split": {split: sum(1 for r in all_rows if r["split"] == split) for split in ["dev", "test", "train"]},
        "by_stratum": {
            stratum: sum(1 for r in all_rows if r["stratum"] == stratum)
            for stratum in [
                "adversarial-ambiguous",
                "batch-write",
                "fault-injection",
                "multi-hop-read",
                "no-tool",
                "single-read",
                "single-write",
            ]
        },
    }
    (ROOT / "datasets").mkdir(exist_ok=True)
    (ROOT / "datasets" / "summary.json").write_text(json.dumps(summary, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(summary, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
