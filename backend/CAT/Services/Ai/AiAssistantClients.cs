using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace CAT.Services.Ai;

public sealed class AiAssistantOptions
{
    public int DraftTtlMinutes { get; set; } = 10;

    public AiLlmClientOptions Llm { get; set; } = new();

    public AiAsrClientOptions Asr { get; set; } = new();
}

public sealed class AiLlmClientOptions
{
    public string Provider { get; set; } = "disabled";

    public string Model { get; set; } = string.Empty;

    public string? Endpoint { get; set; }

    public string OutputMode { get; set; } = "constrained-json";

    public double Temperature { get; set; } = 0;

    // Tool calls are intentionally short. Bounding completion prevents a model
    // from spending minutes on an answer that the deterministic layer will handle.
    public int MaxTokens { get; set; } = 256;

    public int TimeoutSeconds { get; set; } = 45;
}

public sealed class AiAsrClientOptions
{
    public string Provider { get; set; } = "disabled";

    public string Model { get; set; } = string.Empty;

    public string? Endpoint { get; set; }

    public int TimeoutSeconds { get; set; } = 30;
}

public interface IAiAsrClient
{
    string Provider { get; }

    Task<AiAsrTranscription> TranscribeAsync(
        Stream audio,
        string fileName,
        string? contentType,
        CancellationToken cancellationToken = default);
}

public sealed record AiAsrTranscription(
    string Text,
    string NormalizedText,
    string Model,
    double? LatencySeconds);

public sealed class DisabledAiAsrClient : IAiAsrClient
{
    private readonly AiAssistantOptions _options;

    public DisabledAiAsrClient(IOptions<AiAssistantOptions> options)
    {
        _options = options.Value;
    }

    public string Provider => _options.Asr.Provider;

    public Task<AiAsrTranscription> TranscribeAsync(
        Stream audio,
        string fileName,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("ASR provider отключён. Укажите AiAssistant:Asr:Provider=local-http и Endpoint локального ASR сервиса.");
    }
}

public sealed class HttpAiAsrClient : IAiAsrClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly AiAssistantOptions _options;

    public HttpAiAsrClient(HttpClient httpClient, IOptions<AiAssistantOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(10, _options.Asr.TimeoutSeconds));
    }

    public string Provider => _options.Asr.Provider;

    public async Task<AiAsrTranscription> TranscribeAsync(
        Stream audio,
        string fileName,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Asr.Endpoint))
            throw new InvalidOperationException("ASR endpoint не настроен.");

        using var form = new MultipartFormDataContent();
        using var audioContent = new StreamContent(audio);
        audioContent.Headers.ContentType = MediaTypeHeaderValue.Parse(
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
        form.Add(audioContent, "file", string.IsNullOrWhiteSpace(fileName) ? "voice.webm" : fileName);

        using var response = await _httpClient.PostAsync(
            $"{_options.Asr.Endpoint.TrimEnd('/')}/transcribe",
            form,
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"ASR сервис вернул HTTP {(int)response.StatusCode}: {body}");

        var payload = JsonSerializer.Deserialize<LocalAsrResponse>(body, JsonOptions)
                      ?? throw new InvalidOperationException("ASR сервис вернул пустой ответ.");

        var text = payload.NormalizedText ?? payload.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("ASR не распознал речь. Попробуйте записать команду ещё раз.");

        return new AiAsrTranscription(
            payload.Text ?? text,
            text.Trim(),
            payload.Model ?? _options.Asr.Model,
            payload.LatencySeconds);
    }

    private sealed class LocalAsrResponse
    {
        public string? Text { get; set; }
        public string? NormalizedText { get; set; }
        public string? Model { get; set; }
        public double? LatencySeconds { get; set; }
    }
}

public sealed class OpenAiCompatibleLlmClient : IAiAgentLlmClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string ConstrainedJsonOutputMode = "constrained-json";

    private readonly HttpClient _httpClient;
    private readonly AiAssistantOptions _options;

    public OpenAiCompatibleLlmClient(HttpClient httpClient, IOptions<AiAssistantOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(10, _options.Llm.TimeoutSeconds));
    }

    public async Task<AiAgentLlmOutput> GetNextAsync(
        AiAgentSession session,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Llm.Endpoint))
            throw new InvalidOperationException("LLM endpoint не настроен.");
        if (string.IsNullOrWhiteSpace(_options.Llm.Model))
            throw new InvalidOperationException("LLM model не настроена.");

        var constrainedJson = string.Equals(
            _options.Llm.OutputMode,
            ConstrainedJsonOutputMode,
            StringComparison.OrdinalIgnoreCase);

        var payload = new Dictionary<string, object?>
        {
            ["model"] = _options.Llm.Model,
            ["temperature"] = _options.Llm.Temperature,
            ["stream"] = false,
            ["max_tokens"] = Math.Clamp(_options.Llm.MaxTokens, 32, 512),
            ["reasoning_effort"] = "none",
            ["messages"] = BuildMessages(session, constrainedJson)
        };

        if (constrainedJson)
        {
            payload["response_format"] = AiToolDefinitions.ConstrainedResponseFormat;
        }
        else
        {
            payload["tools"] = AiToolDefinitions.Tools;
            payload["tool_choice"] = "auto";
        }

        using var content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync(
            $"{_options.Llm.Endpoint.TrimEnd('/')}/chat/completions",
            content,
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"LLM endpoint вернул HTTP {(int)response.StatusCode}: {body}");

        using var document = JsonDocument.Parse(body);
        var message = document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message");

        if (TryReadNativeToolCall(message, out var nativeToolCall))
            return nativeToolCall;

        var answer = message.TryGetProperty("content", out var contentElement)
            ? contentElement.GetString()
            : null;

        if (TryReadJsonOutput(answer, out var constrainedOutput))
            return constrainedOutput;

        if (TryReadJsonToolCall(answer, out var jsonToolCall))
            return jsonToolCall;

        return AiAgentLlmOutput.Final(string.IsNullOrWhiteSpace(answer)
            ? "Не удалось выбрать действие. Сформулируйте запрос подробнее."
            : answer.Trim());
    }

    private static object[] BuildMessages(AiAgentSession session, bool constrainedJson)
    {
        var today = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd");
        var schemaInstruction = constrainedJson
            ? "\nВерни строго один JSON-объект по response_format: либо final_answer, либо tool_call. Не добавляй markdown и пояснения вне JSON. Доступные tools и их аргументы:\n" +
              JsonSerializer.Serialize(AiToolDefinitions.ToolSpecsForPrompt, JsonOptions)
            : string.Empty;
        var messages = new List<object>
        {
            new
            {
                role = "system",
                content = $"""
/no_think
Ты AI-помощник CattleTrack для русскоязычного пользователя фермы.
Сегодня: {today}.
Выбирай только доступные tools. Не придумывай функций, заметок, заказов лекарств, экономики или действий вне MVP.
Для животных всегда извлекай бирку/tag, если пользователь назвал бирку. Не выдумывай UUID.
В tag пиши только значение бирки без слов "корова", "бык", "cow", "animal" и без префиксов: "корова 25" -> tag "25".
animal_id используй только если backend уже вернул UUID животного. Число вроде 25, 523, 1432 — это бирка, а не animal_id.
Если пользователь просит посмотреть карточку животного по бирке, вызывай get_animal_card с tag.
Если пользователь просит историю веса по бирке, вызывай get_weight_history с tag.
Если пользователь говорит вес, взвесили, килограммы или кг, всегда вызывай create_weight. Не используй create_daily_action для веса.
Для write-команд заполняй только явно сказанные поля. Если обязательного поля нет, всё равно вызови ближайший write-tool с извлечёнными данными: backend validator покажет человеку уточнение.
Продолжай незавершённый диалог: используй уже названные пользователем бирки и данные из предыдущих сообщений, когда пользователь присылает уточнение. Не повторяй вопрос о бирке, если она уже однозначно названа в контексте.
Даты возвращай в формате yyyy-MM-dd. "Сегодня" = {today}.
Если подходящего MVP-tool нет, ответь коротко текстом без tool call.

Примеры выбора tool:
- "добавь вес 422 кг корове 25 за 2 января ручное взвешивание" -> create_weight с tag "25", weight 422, date yyyy-MM-dd, method "Ручное взвешивание".
- "внеси осеменение корове 25 сегодня естественное" -> create_insemination с cow_tags ["25"], insemination_type "Естественное".
- "покажи карточку коровы 25" -> get_animal_card с tag "25".
- "переведи 25 в другую группу" -> create_daily_action.
{schemaInstruction}
"""
            }
        };

        messages.AddRange(session.History
            .Where(message => message.Role is AiAgentMessageRole.User or AiAgentMessageRole.Assistant)
            .Where(message => !string.IsNullOrWhiteSpace(message.Content))
            .TakeLast(12)
            .Select(message => new
            {
                role = message.Role,
                content = message.Role == AiAgentMessageRole.User
                    ? AppendNoThink(message.Content!)
                    : message.Content!
            }));

        return messages.ToArray();
    }

    private static string AppendNoThink(string content)
        => content.Contains("/no_think", StringComparison.OrdinalIgnoreCase)
            ? content
            : $"{content.Trim()}\n/no_think";

    private static bool TryReadNativeToolCall(JsonElement message, out AiAgentLlmOutput output)
    {
        output = null!;
        if (!message.TryGetProperty("tool_calls", out var toolCalls) ||
            toolCalls.ValueKind != JsonValueKind.Array ||
            toolCalls.GetArrayLength() == 0)
        {
            if (message.TryGetProperty("function_call", out var functionCall) &&
                functionCall.ValueKind == JsonValueKind.Object)
            {
                return TryBuildToolCall(functionCall, out output);
            }

            return false;
        }

        var first = toolCalls[0];
        if (!first.TryGetProperty("function", out var function) || function.ValueKind != JsonValueKind.Object)
            return false;

        return TryBuildToolCall(function, out output);
    }

    private static bool TryReadJsonToolCall(string? content, out AiAgentLlmOutput output)
    {
        output = null!;
        if (string.IsNullOrWhiteSpace(content))
            return false;

        var trimmed = content.Trim();
        var firstBrace = trimmed.IndexOf('{');
        var lastBrace = trimmed.LastIndexOf('}');
        if (firstBrace < 0 || lastBrace <= firstBrace)
            return false;

        try
        {
            using var document = JsonDocument.Parse(trimmed[firstBrace..(lastBrace + 1)]);
            if (document.RootElement.TryGetProperty("tool_call", out var toolCall))
                return TryBuildToolCall(toolCall, out output);
            if (document.RootElement.TryGetProperty("name", out _))
                return TryBuildToolCall(document.RootElement, out output);
        }
        catch (JsonException)
        {
            return false;
        }

        return false;
    }

    private static bool TryReadJsonOutput(string? content, out AiAgentLlmOutput output)
    {
        output = null!;
        if (string.IsNullOrWhiteSpace(content))
            return false;

        try
        {
            using var document = JsonDocument.Parse(content.Trim());
            var root = document.RootElement;
            if (!root.TryGetProperty("type", out var typeElement))
                return false;

            var type = typeElement.GetString();
            if (type == AiAgentOutputType.FinalAnswer)
            {
                var finalAnswer = root.TryGetProperty("final_answer", out var answerElement)
                    ? answerElement.GetString()
                    : null;

                output = AiAgentLlmOutput.Final(finalAnswer ?? string.Empty);
                return true;
            }

            if (type == AiAgentOutputType.ToolCall &&
                root.TryGetProperty("tool_call", out var toolCallElement))
            {
                return TryBuildToolCall(toolCallElement, out output);
            }
        }
        catch (JsonException)
        {
            return false;
        }

        return false;
    }

    private static bool TryBuildToolCall(JsonElement function, out AiAgentLlmOutput output)
    {
        output = null!;
        if (!function.TryGetProperty("name", out var nameElement))
            return false;

        var name = nameElement.GetString();
        if (string.IsNullOrWhiteSpace(name))
            return false;

        JsonElement? arguments = null;
        if (function.TryGetProperty("arguments", out var argumentsElement))
        {
            arguments = argumentsElement.ValueKind switch
            {
                JsonValueKind.String when !string.IsNullOrWhiteSpace(argumentsElement.GetString())
                    => JsonDocument.Parse(argumentsElement.GetString()!).RootElement.Clone(),
                JsonValueKind.Object => argumentsElement.Clone(),
                _ => null
            };
        }

        output = AiAgentLlmOutput.Call(name.Trim(), arguments);
        return true;
    }
}

internal static class AiToolDefinitions
{
    private static readonly string[] ToolNames =
    {
        "find_animal",
        "get_animal_card",
        "get_animal_parents",
        "get_weight_history",
        "get_pregnancies_to_check",
        "list_groups",
        "create_weight",
        "create_daily_action",
        "create_insemination"
    };

    public static readonly object[] Tools =
    {
        Tool("find_animal", "Найти животное по точной бирке в организации.", new
        {
            type = "object",
            required = new[] { "schema_version", "tag" },
            properties = new
            {
                schema_version = ConstV1(),
                tag = Tag(),
                include_inactive = new { type = "boolean" }
            }
        }),
        Tool("get_animal_card", "Показать карточку животного. Предпочитай tag, если пользователь назвал бирку.", new
        {
            type = "object",
            required = new[] { "schema_version", "tag" },
            properties = new
            {
                schema_version = ConstV1(),
                tag = Tag(),
                animal_id = new { type = "string", description = "UUID только если он уже известен из backend tool result." }
            }
        }),
        Tool("get_animal_parents", "Показать родителей животного по бирке.", new
        {
            type = "object",
            required = new[] { "schema_version", "tag" },
            properties = new { schema_version = ConstV1(), tag = Tag() }
        }),
        Tool("get_weight_history", "Показать историю веса животного по бирке или UUID.", new
        {
            type = "object",
            required = new[] { "schema_version", "tag" },
            properties = new
            {
                schema_version = ConstV1(),
                tag = Tag(),
                animal_id = new { type = "string" },
                date_from = Date(),
                date_to = Date(),
                limit = new { type = "integer", minimum = 1, maximum = 100 }
            }
        }),
        Tool("get_pregnancies_to_check", "Список коров для диагностики стельности.", new
        {
            type = "object",
            required = new[] { "schema_version" },
            properties = new { schema_version = ConstV1(), due_before = Date() }
        }),
        Tool("list_groups", "Список групп животных.", new
        {
            type = "object",
            required = new[] { "schema_version" },
            properties = new { schema_version = ConstV1(), include_empty = new { type = "boolean" } }
        }),
        Tool("create_weight", "Создать preview внесения веса. Commit выполняется только после подтверждения человеком.", new
        {
            type = "object",
            required = new[] { "schema_version", "idempotency_key", "tag", "weight", "date", "method" },
            properties = new
            {
                schema_version = ConstV1(),
                idempotency_key = IdempotencyKey(),
                tag = Tag(),
                weight = new { type = "number", exclusiveMinimum = 0, maximum = 3000 },
                date = Date(),
                method = new { type = "string", @enum = new[] { "Автоматическая весовая станция", "Ручное взвешивание", "Расчетный метод", "__unknown" } },
                method_raw = new { type = "string" }
            }
        }),
        Tool("create_daily_action", "Создать preview daily action batch.", new
        {
            type = "object",
            required = new[] { "schema_version", "batch_idempotency_key", "items" },
            properties = new
            {
                schema_version = ConstV1(),
                batch_idempotency_key = IdempotencyKey(),
                items = new
                {
                    type = "array",
                    minItems = 1,
                    items = new
                    {
                        type = "object",
                        required = new[] { "idempotency_key", "tag", "type", "date" },
                        properties = new
                        {
                            idempotency_key = IdempotencyKey(),
                            tag = Tag(),
                            type = new { type = "string", @enum = new[] { "Осмотры", "Обработка", "Вакцинации и обработки", "Лечение", "Перевод", "Выбытие", "Исследования", "Присвоение номеров", "Изменение половозрастной группы", "__unknown" } },
                            type_raw = new { type = "string" },
                            subtype = new { type = "string" },
                            performed_by = new { type = "string" },
                            result = new { type = "string" },
                            medicine = new { type = "string" },
                            dose = new { type = "string" },
                            date = Date(),
                            next_date = Date(),
                            old_group_id = new { type = "string" },
                            new_group_id = new { type = "string" },
                            new_group_name = new { type = "string" },
                            group_name = new { type = "string" },
                            old_type = new { type = "string" },
                            new_type = new { type = "string" },
                            research_name = new { type = "string" },
                            material_type = new { type = "string" },
                            identification_value = new { type = "string" }
                        }
                    }
                }
            }
        }),
        Tool("create_insemination", "Создать preview осеменения. Не использовать для беременности/стельности.", new
        {
            type = "object",
            required = new[] { "schema_version", "batch_idempotency_key", "items" },
            properties = new
            {
                schema_version = ConstV1(),
                batch_idempotency_key = IdempotencyKey(),
                items = new
                {
                    type = "array",
                    minItems = 1,
                    items = new
                    {
                        type = "object",
                        required = new[] { "idempotency_key", "cow_tags", "date", "insemination_type" },
                        properties = new
                        {
                            idempotency_key = IdempotencyKey(),
                            cow_tags = new { type = "array", minItems = 1, items = Tag() },
                            date = Date(),
                            insemination_type = new { type = "string", @enum = new[] { "Искусственное", "Естественное", "Эмбрион", "__unknown" } },
                            insemination_type_raw = new { type = "string" },
                            sperm_batch = new { type = "string" },
                            sperm_manufacturer = new { type = "string" },
                            bull_tags = new { type = "array", items = Tag() },
                            embryo_id = new { type = "string" },
                            embryo_manufacturer = new { type = "string" },
                            technician = new { type = "string" },
                            bull_name = new { type = "string" }
                        }
                    }
                }
            }
        })
    };

    public static readonly object[] ToolSpecsForPrompt = Tools;

    public static readonly object ConstrainedResponseFormat = new
    {
        type = "json_schema",
        json_schema = new
        {
            name = "cattletrack_agent_output",
            strict = true,
            schema = new
            {
                type = "object",
                additionalProperties = false,
                required = new[] { "type", "final_answer", "tool_call" },
                properties = new
                {
                    type = new { type = "string", @enum = new[] { AiAgentOutputType.FinalAnswer, AiAgentOutputType.ToolCall } },
                    final_answer = new { type = new[] { "string", "null" } },
                    tool_call = new
                    {
                        anyOf = new object[]
                        {
                            new { type = "null" },
                            new
                            {
                                type = "object",
                                additionalProperties = false,
                                required = new[] { "name", "arguments" },
                                properties = new
                                {
                                    name = new { type = "string", @enum = ToolNames },
                                    arguments = new { type = "object", additionalProperties = true }
                                }
                            }
                        }
                    }
                }
            }
        }
    };

    private static object Tool(string name, string description, object parameters)
        => new { type = "function", function = new { name, description, parameters } };

    private static object ConstV1()
        => new { type = "string", @enum = new[] { "v1" } };

    private static object Tag()
        => new { type = "string", minLength = 1, maxLength = 64 };

    private static object Date()
        => new { type = "string", description = "Дата в формате yyyy-MM-dd." };

    private static object IdempotencyKey()
        => new { type = "string", minLength = 1, maxLength = 255 };
}
