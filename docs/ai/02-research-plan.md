# Программа повторных исследований AI-компонентов

Версия: 2026-07-13.

Этот документ задаёт новый протокол выбора ASR и LLM для CattleTrack. Предыдущие исследования были полезны как quality baseline, но недостаточно учитывали production latency и совместное размещение моделей. Поэтому `Whisper-large-v3` и `T-pro-it-2.1` больше не считаются выбранными production-моделями: они остаются контрольными точками качества, а финальное решение принимается заново на целевом сервере.

Тулы и MVP-подмножество описаны в `00-tool-catalog.md`. Сторонние бенчмарки используются только для формирования shortlist. Выбор делается на проверенных CattleTrack-датасетах и end-to-end сценариях приложения.

## 1. Целевая среда и причина пересмотра

Целевой сервер, проверено 2026-07-13:

| Компонент | Конфигурация |
|---|---|
| CPU | AMD Ryzen 7 7700, 8 ядер / 16 потоков |
| RAM | 61 GiB |
| GPU | NVIDIA GeForce RTX 5070, 12 227 MiB VRAM |
| Текущая LLM | `T-pro-it-2.1` Q4, около 10.5 GiB VRAM в Ollama |
| Текущая проблема | LLM занимает почти всю VRAM; ASR и LLM не могут нормально работать одновременно, появляются выгрузка моделей, cold start и ответы длиннее 2 минут |

Новый принцип: качество оценивается только вместе со скоростью и ресурсами. Модель, которая немного точнее, но не обеспечивает интерактивный диалог на целевом сервере, не может быть выбрана для production.

## 2. Общие критерии выбора

### 2.1 Обязательные ограничения

- обе production-модели должны одновременно помещаться на RTX 5070 без постоянной выгрузки из VRAM;
- LLM работает в non-thinking режиме и выдаёт короткий tool call или короткий ответ;
- контекст agent loop ограничен фактически нужной историей, целевой runtime context — 8K, увеличение только по измеренной необходимости;
- ASR и LLM запускаются на одном сервере в том же режиме, который будет использовать приложение;
- замеры делаются после warmup и отдельно с cold start;
- ошибки обязательных полей, entity resolution и write-бизнес-правил не перекладываются на размер модели: их обрабатывает детерминированный backend.

### 2.2 Latency-бюджет

| Участок | Цель p50 | Максимум p95 для кандидата |
|---|---:|---:|
| ASR после остановки записи | ≤0.8 с | ≤1.5 с |
| LLM time to first token/tool call | ≤1.0 с | ≤2.0 с |
| Полный LLM tool-call | ≤2.5 с | ≤5.0 с |
| Текстовый запрос до preview/read-answer | ≤3.5 с | ≤6.0 с |
| Голосовой запрос после stop до preview/read-answer | ≤5.0 с | ≤8.0 с |

Дополнительно измеряются RTF, tokens/s, VRAM peak, RAM peak, время загрузки, доля запросов с timeout и деградация при 2 последовательных/параллельных запросах.

## 3. Повторное исследование ASR

### 3.1 Shortlist

| Кандидат | Роль в исследовании | Почему включён |
|---|---|---|
| `GigaAM-v3-e2e-rnnt` и при необходимости `e2e-ctc` | основной RU speed-кандидат | Русскоязычная модель 220–240M параметров. В прошлом CattleTrack-прогоне p95 был 0.616 с, но качество бирок было ниже Whisper; нужно повторить с уже готовым postprocessor и domain dictionary. |
| `openai/whisper-large-v3-turbo` | основной multilingual balance-кандидат | Это pruned/fine-tuned вариант `large-v3` с 4 decoder layers вместо 32: существенно быстрее при заявленной небольшой потере качества, поддерживает русский язык. |
| `nvidia/canary-1b-v2` | независимый multilingual-кандидат | Поддерживает русский среди 25 языков, имеет 1B параметров, punctuation/timestamps и заявлен как быстрый ASR своего класса. Нужен, чтобы shortlist не состоял только из GigaAM/Whisper. |
| `openai/whisper-large-v3` | quality ceiling, не production-кандидат | Сохраняется только для сравнения качества с предыдущим экспериментом. Если более лёгкая модель статистически не хуже на критичных сущностях, выбирается более лёгкая. |

Не включаем T-one в новый основной прогон: предыдущий CattleTrack-тест дал WER 0.887 и `animal_tag_exact_match = 0` до отдельной сложной нормализации. Не включаем `whisper-medium`: при близком размере `large-v3-turbo` является более прямым кандидатом на сохранение качества `large-v3` и уменьшение decoder latency.

Источники для shortlist:

- GigaAM-v3: https://github.com/salute-developers/GigaAM
- Whisper large-v3-turbo: https://huggingface.co/openai/whisper-large-v3-turbo
- Canary-1B-v2: https://huggingface.co/nvidia/canary-1b-v2

### 3.2 Данные и режимы

Использовать все 72 вручную проверенных аудио из приватного ASR manifest. До финального production-решения добавить отдельный pilot split из 20–40 реальных записей разных пользователей; синтетические аудио не заменяют этот split.

Каждый кандидат прогоняется:

1. raw transcript;
2. transcript + детерминированный ASR postprocessor;
3. transcript + postprocessor + словарь хозяйства;
4. в тишине и с шумом отдельно;
5. cold start, warm single-request и совместно с загруженной LLM.

LLM-коррекция не включается в основной hot path по умолчанию. Она исследуется отдельно только для неоднозначных сущностей, если даёт значимый прирост и укладывается в общий latency-бюджет.

### 3.3 Метрики и решение

- corpus WER и 95% bootstrap CI;
- entity WER по `number`, `proper_noun`, `other`;
- exact-match бирки и идентификатора целиком;
- false positive rate postprocessor/domain correction;
- latency p50/p95/max, RTF, load time;
- VRAM/RAM peak;
- end-to-end voice-to-preview latency.

Приоритет решения:

1. `animal_tag_exact_match` и entity WER не выходят за заранее установленный допустимый коридор относительно `Whisper-large-v3 + postprocessor`;
2. p95 ASR ≤1.5 с при одновременно загруженной LLM;
3. при пересечении доверительных интервалов выбирается модель с меньшими VRAM и p95;
4. если ни один кандидат не проходит оба барьера, оптимизируется runtime/quantization, а не автоматически возвращается самая тяжёлая модель.

### 3.4 Финальный результат 2026-07-13

На всех 72 вручную одобренных аудио выбран `openai/whisper-large-v3-turbo` через `faster-whisper 1.2.1`, CUDA FP16, beam size 1.

| Model/runtime | WER | Animal tag exact | p95 |
|---|---:|---:|---:|
| GigaAM-v3 e2e RNNT | 21.38% | 80.85% | 0.233 s |
| Whisper turbo / Transformers | 9.34% | 97.87% | 1.888 s |
| Whisper turbo / faster-whisper | **9.09%** | **100.00% (47/47)** | **0.149 s** |

GigaAM исключён по качеству идентификаторов. Transformers-runtime не прошёл p95 ≤1.5 s. Faster-whisper сохранил качество модели и прошёл latency/resource gate.

## 4. Повторное исследование LLM tool-calling

### 4.1 Shortlist

| Кандидат | Рекомендуемая конфигурация первого прогона | Роль |
|---|---|---|
| `t-tech/T-lite-it-2.1` | GGUF `Q5_K_M`, 5.9 GB, context 8K | главный специализированный RU-кандидат; возраст модели учитывается отдельно, ценность проверяется только нашим benchmark. |
| `Qwen/Qwen3.5-4B` | Q5/Q4, text-only runtime, context 8K | актуальный speed floor семейства Qwen, выпущенный 2026-03-02; официальный model card описывает tool calling через `qwen3_coder` parser. |
| `Qwen/Qwen3.5-9B` | Q5/Q4, text-only runtime, context 8K | актуальный Qwen-контроль большего размера; проверяет прирост качества русского tool-calling относительно 4B при сохранении co-residency с ASR. |
| `mistralai/Ministral-3-8B-Instruct-2512` | только если runtime стабильно запускается на RTX 5070 в ≤7 GiB | независимый 8B-кандидат с native function calling/JSON. Второй приоритет: русский язык нужно отдельно подтвердить нашим датасетом. |

`T-pro-it-2.1` и `Qwen3-32B` остаются quality/latency baselines, но исключаются из production shortlist: 32B-класс в текущем Q4 занимает почти всю 12 GiB GPU и не оставляет нормального бюджета для ASR и KV cache.

Не включаем старый `T-lite-it-1.0`: его карточка не заявляет поддержку tool use. Нужна именно версия `T-lite-it-2.1`.

Источники для shortlist:

- T-lite-it-2.1 и ruBFCL/tool-calling: https://huggingface.co/t-tech/T-lite-it-2.1
- официальные GGUF-размеры T-lite-it-2.1: https://huggingface.co/t-tech/T-lite-it-2.1-GGUF
- Qwen3.5-4B: https://huggingface.co/Qwen/Qwen3.5-4B
- Qwen3.5-9B: https://huggingface.co/Qwen/Qwen3.5-9B
- текущая линейка Qwen3.6 и даты релизов: https://github.com/QwenLM/Qwen3.6
- Ministral-3-8B-Instruct-2512: https://huggingface.co/mistralai/Ministral-3-8B-Instruct-2512

### 4.2 Данные

Использовать вручную approved CattleTrack tool-calling dataset, сохраняя существующие train/dev/test split. Fault-injection split оценивает catch rate бизнес-валидатора: его golden-аргументы намеренно испорчены и не могут считаться правильным ответом LLM. Перед новым прогоном добавить отдельную multi-turn страту:

- уточнение обязательного поля во втором сообщении;
- ссылка на животное из предыдущей реплики;
- исправление пользователем уже названного значения;
- отмена незавершённого draft;
- выбор одного животного после ambiguity.

Это обязательно: production-модуль использует session history, а старый benchmark в основном оценивал одиночные запросы.

### 4.3 Протокол

1. Зафиксировать одинаковые resolver-aware tool schemas и system prompt.
2. Не просить LLM генерировать UUID, idempotency keys и backend-derived поля.
3. Запускать non-thinking режим, короткий `max_tokens` и одинаковую стратегию декодирования.
4. Сначала smoke и полный dev pass@1; только кандидаты, прошедшие quality/latency gates, идут в pass^3 и test.
5. Для каждого кандидата сравнить как минимум Q4 и Q5/Q6, если обе конфигурации помещаются в VRAM.
6. Замерять модель отдельно и одновременно с выбранными ASR-кандидатами.
7. Сравнить Ollama/llama.cpp с SGLang или vLLM на одном лучшем кандидате. Runtime выбирается измерением; `vLLM + XGrammar` больше не считается безусловно заданным.

### 4.4 Метрики

Качество:

- tool selection accuracy;
- resolver-aware argument match;
- state-based success для write;
- strict/partial batch success;
- multi-turn carry-over success;
- safe clarification accuracy;
- pass@1 и pass^3;
- Wilson 95% CI и error analysis по стратам.

Production:

- TTFT p50/p95;
- complete tool-call latency p50/p95/max;
- tokens/s и input processing speed;
- VRAM/RAM peak, cold load time;
- timeout/error rate;
- end-to-end text-to-preview и voice-to-preview;
- влияние длины session history: 1, 4, 8 и 12 сообщений.

### 4.5 Правило выбора

Модель проходит в production только если одновременно:

- p95 полного tool-call ≤5 с на RTX 5070;
- совместно с ASR не требует unload/reload на обычном запросе;
- state-based write success не хуже лучшего кандидата более чем на заранее установленный non-inferiority margin 5 п.п.;
- ambiguity, missing required fields и no-tool не превращаются в небезопасный write;
- после validator/normalizer итоговое состояние БД соответствует golden.

Если 4B-модель проходит quality margin, выбирается она. Если нет — выбирается лучший 8B-кандидат. 32B возвращается в рассмотрение только при смене оборудования или доказанной невозможности выполнить quality gate малой моделью.

### 4.6 Результат прогона 2026-07-13

На целевом сервере одинаковым resolver-aware bounded agent loop проверены `Qwen3.5-4B Q4_K_M`, `Qwen3.5-9B Q4_K_M` и `T-lite-it-2.1 Q5_K_M`. Approved tool-calling корпус: 48 dev/test строк; test pass^3: 23 строки; multi-turn 10 строк оставлен диагностическим до ручного подтверждения.

| Model | Semantic pass@1 | test pass^3 | Full loop p95 | LLM + ASR peak |
|---|---:|---:|---:|---:|
| Qwen3.5-4B | 50.00% | 69.57% | 4.40 s | 8,767 MiB |
| Qwen3.5-9B | **62.50%** | **73.91%** | **3.10 s** | 11,365 MiB |
| T-lite-it-2.1 | 39.58% | 47.83% | 2.74 s | 10,599 MiB |

Исследовательский победитель — `Qwen3.5-9B Q4_K_M`; `Qwen3.5-4B Q4_K_M` остается fast/resource fallback. T-lite исключается из MVP shortlist из-за 0/11 single-write и 0/4 no-tool, несмотря на лучший TTFT.

Полный кандидатный отчёт хранится во внутреннем experiment archive. Финальная constrained/end-to-end приёмка описана ниже.

## 5. Structured output и validator

Повторить ablation на победившей LLM и её runner:

1. native/free tool output;
2. grammar/constrained JSON через фактически выбранный runtime;
3. constrained output + deterministic business validator;
4. validator + resolver + post-LLM normalizer.

Метрики: schema-valid rate, catch rate, false positive rate, confusion matrix, добавленная latency p50/p95. Constrained decoding остаётся только если даёт измеримую пользу и не ломает latency budget.

### 5.1 Финальный результат 2026-07-13

- constrained JSON parse: `48/48` против `41/48` free generation;
- structural success: `87.50%` против `66.67%`;
- Qwen3.5-9B constrained agent semantic pass@1: `68.75%`, p95 `2.256 s`, runtime `48/48`;
- schema + business validator: catch `25/25`, false positive `0/19`;
- constrained text control на 72 строках: `69.44%`;
- полный ASR -> LLM: `59.72%`, runtime `72/72`, p95 LLM loop `2.366 s`;
- совместный VRAM peak: `9,186 / 12,227 MiB`.

Native Ollama tool API исключён: он дал 2-3 HTTP 500 на 72 запросах. Полный отчёт хранится во внутреннем experiment archive.

## 6. TTS

Silero v5 не требует нового model search. Предыдущий локальный тест дал p95 0.0455 с и RTF p95 0.0093 после warmup. Нужно только повторить operational smoke на Ryzen 7 7700 и держать TTS на CPU, чтобы не занимать VRAM ASR/LLM.

## 7. Порядок повторной работы

1. Подготовить multi-turn дополнение к LLM test split и зафиксировать неизменяемый evaluation manifest.
2. Установить ASR/LLM-кандидатов на целевом сервере, не заменяя текущие модели до окончания сравнения.
3. Снять короткий resource/latency smoke для всех кандидатов.
4. Отсеять кандидатов, не проходящих VRAM или p95 budget.
5. Провести полный ASR benchmark оставшихся моделей.
6. Провести LLM dev pass@1, затем pass^3/test только для финалистов.
7. Провести совместный ASR+LLM и end-to-end benchmark через приложение.
8. Повторить constrained/validator ablation.
9. Зафиксировать решение, версии, quantization, runner, параметры запуска и rollback baseline.

## 8. Формат отчёта

Для каждого исследования: гипотеза → точная версия модели/runtime → hardware → параметры → quality-метрики с CI → latency/resources → error analysis → решение → ограничения.

Нельзя переносить latency с Apple M3 Max на RTX 5070 или сравнивать модели, запущенные разными prompt/schema/runtime без отдельной оговорки. Старые результаты сохраняются как исторический baseline, новые складываются в отдельный каталог с датой повторного исследования.
