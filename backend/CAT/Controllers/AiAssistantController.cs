using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using CAT.Controllers.DTO;
using CAT.Controllers.DTO.AiAssistant;
using CAT.Filters;
using CAT.Services;
using CAT.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CAT.Controllers;

[Route("api/[controller]")]
[Authorize]
[ApiController]
public sealed class AiAssistantController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const int VoiceStreamMaxBytes = 25_000_000;

    private readonly IAiAssistantService _aiAssistantService;

    public AiAssistantController(IAiAssistantService aiAssistantService)
    {
        _aiAssistantService = aiAssistantService;
    }

    /// <summary>
    /// Создаёт AI draft по текстовому запросу пользователя.
    /// </summary>
    [HttpPost("text")]
    [OrgValidationTypeFilter(checkOrg: true)]
    public async Task<IActionResult> CreateTextDraft(
        [FromHeader] Guid organizationId,
        [FromBody] AiAssistantTextRequestDTO request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _aiAssistantService.CreateTextDraftAsync(organizationId, request, cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorDTO(ex.Message));
        }
    }

    /// <summary>
    /// Принимает голосовой запрос чанками через WebSocket и создаёт AI draft после финального stop.
    /// </summary>
    [HttpGet("voice/stream")]
    public async Task StreamVoiceDraft(
        [FromQuery] Guid organizationId,
        [FromQuery] string? clientRequestId,
        [FromQuery] string? conversationId,
        CancellationToken cancellationToken)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await HttpContext.Response.WriteAsync("WebSocket request expected.", cancellationToken);
            return;
        }

        var authOrgId = User.Claims.FirstOrDefault(x => x.Type == "Organization")?.Value;
        if (!Guid.TryParse(authOrgId, out var claimOrgId) || claimOrgId != organizationId)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            await HttpContext.Response.WriteAsync("Нет доступа к информации чужой организации.", cancellationToken);
            return;
        }

        using var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        await HandleVoiceStreamSocket(socket, organizationId, clientRequestId, conversationId, cancellationToken);
    }

    /// <summary>
    /// Распознаёт голосовой запрос через локальный ASR и создаёт AI draft.
    /// </summary>
    [HttpPost("voice")]
    [RequestSizeLimit(25_000_000)]
    [OrgValidationTypeFilter(checkOrg: true)]
    public async Task<IActionResult> CreateVoiceDraft(
        [FromHeader] Guid organizationId,
        [FromForm] IFormFile audio,
        [FromForm] string? clientRequestId,
        [FromForm] string? conversationId,
        CancellationToken cancellationToken)
    {
        if (audio == null || audio.Length == 0)
            return BadRequest(new ErrorDTO("Аудиофайл обязателен."));

        try
        {
            await using var stream = audio.OpenReadStream();
            var response = await _aiAssistantService.CreateVoiceDraftAsync(
                organizationId,
                stream,
                audio.FileName,
                audio.ContentType,
                clientRequestId,
                conversationId,
                cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorDTO(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ErrorDTO(ex.Message));
        }
    }

    /// <summary>
    /// Выбирает животное из вариантов для read-сценария без повторного LLM-вызова.
    /// </summary>
    [HttpPost("read/select")]
    [OrgValidationTypeFilter(checkOrg: true)]
    public async Task<IActionResult> SelectReadCandidate(
        [FromHeader] Guid organizationId,
        [FromBody] AiAssistantSelectReadCandidateRequestDTO request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _aiAssistantService.SelectReadCandidateAsync(
                organizationId, request, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorDTO(ex.Message));
        }
    }

    /// <summary>
    /// Выбирает животное из вариантов в существующем write-черновике.
    /// </summary>
    [HttpPost("drafts/{draftId:guid}/items/{itemIndex:int}/select")]
    [OrgValidationTypeFilter(checkOrg: true)]
    public async Task<IActionResult> SelectDraftCandidate(
        [FromHeader] Guid organizationId,
        [FromRoute] Guid draftId,
        [FromRoute] int itemIndex,
        [FromBody] AiAssistantSelectCandidateRequestDTO request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _aiAssistantService.SelectDraftCandidateAsync(
                organizationId, draftId, itemIndex, request, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorDTO(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorDTO(ex.Message));
        }
    }

    /// <summary>
    /// Подтверждает или отменяет AI draft.
    /// </summary>
    [HttpPost("drafts/{draftId:guid}/confirm")]
    [OrgValidationTypeFilter(checkOrg: true)]
    public async Task<IActionResult> ConfirmDraft(
        [FromHeader] Guid organizationId,
        [FromRoute] Guid draftId,
        [FromBody] AiAssistantConfirmRequestDTO request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _aiAssistantService.ConfirmDraftAsync(
                organizationId,
                draftId,
                request,
                cancellationToken);

            return response.Status switch
            {
                AiAssistantDraftStatus.ConfirmExpired => BadRequest(response),
                AiAssistantDraftStatus.CannotCommit => BadRequest(response),
                _ => Ok(response)
            };
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorDTO(ex.Message));
        }
    }

    private async Task HandleVoiceStreamSocket(
        WebSocket socket,
        Guid organizationId,
        string? clientRequestId,
        string? conversationId,
        CancellationToken cancellationToken)
    {
        await using var audio = new MemoryStream();
        var buffer = new byte[64 * 1024];
        var contentType = "audio/webm";
        var fileName = "voice.webm";
        await SendStreamEvent(socket, "ready", new { maxBytes = VoiceStreamMaxBytes }, cancellationToken);

        while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            var message = await ReceiveWebSocketMessage(socket, buffer, cancellationToken);
            if (message == null)
                break;

            if (message.Value.MessageType == WebSocketMessageType.Close)
                break;

            if (message.Value.MessageType == WebSocketMessageType.Binary)
            {
                if (audio.Length + message.Value.Payload.Length > VoiceStreamMaxBytes)
                {
                    await SendStreamEvent(socket, "error", new { message = "Аудио слишком большое." }, cancellationToken);
                    break;
                }

                await audio.WriteAsync(message.Value.Payload, cancellationToken);
                await SendStreamEvent(socket, "chunk_received", new { bytes = audio.Length }, cancellationToken);
                continue;
            }

            var command = DeserializeStreamCommand(message.Value.Payload);
            switch (command.Type)
            {
                case "start":
                    contentType = string.IsNullOrWhiteSpace(command.ContentType) ? contentType : command.ContentType!;
                    fileName = string.IsNullOrWhiteSpace(command.FileName) ? fileName : command.FileName!;
                    await SendStreamEvent(socket, "recording_started", new { contentType, fileName }, cancellationToken);
                    break;
                case "client_transcript":
                    // Browser ASR is intentionally ignored. The assistant must use only the backend ASR transcript.
                    break;
                case "stop":
                    await FinalizeVoiceStream(socket, organizationId, audio, fileName, contentType, clientRequestId, conversationId, cancellationToken);
                    return;
                case "cancel":
                    await SendStreamEvent(socket, "canceled", new { }, cancellationToken);
                    return;
                default:
                    await SendStreamEvent(socket, "error", new { message = $"Неизвестная команда stream: {command.Type}" }, cancellationToken);
                    break;
            }
        }

        if (socket.State == WebSocketState.Open)
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "voice stream closed", cancellationToken);
    }

    private async Task FinalizeVoiceStream(
        WebSocket socket,
        Guid organizationId,
        MemoryStream audio,
        string fileName,
        string contentType,
        string? clientRequestId,
        string? conversationId,
        CancellationToken cancellationToken)
    {
        if (audio.Length == 0)
        {
            await SendStreamEvent(socket, "error", new { message = "Аудио не получено." }, cancellationToken);
            return;
        }

        await SendStreamEvent(socket, "asr_started", new { }, cancellationToken);
        audio.Position = 0;

        try
        {
            var response = await _aiAssistantService.CreateVoiceDraftAsync(
                organizationId,
                audio,
                fileName,
                contentType,
                clientRequestId,
                conversationId,
                cancellationToken);
            await SendStreamEvent(socket, "assistant_response", response, cancellationToken);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            await SendStreamEvent(socket, "error", new { message = ex.Message }, cancellationToken);
        }
    }

    private static async Task SendStreamEvent(
        WebSocket socket,
        string type,
        object payload,
        CancellationToken cancellationToken)
    {
        if (socket.State != WebSocketState.Open)
            return;

        var json = JsonSerializer.Serialize(new { type, payload }, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        await socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
    }

    private static async Task<VoiceStreamSocketMessage?> ReceiveWebSocketMessage(
        WebSocket socket,
        byte[] buffer,
        CancellationToken cancellationToken)
    {
        using var payload = new MemoryStream();
        WebSocketReceiveResult result;
        do
        {
            result = await socket.ReceiveAsync(buffer, cancellationToken);
            if (result.Count > 0)
                payload.Write(buffer, 0, result.Count);
        }
        while (!result.EndOfMessage);

        return new VoiceStreamSocketMessage(result.MessageType, payload.ToArray());
    }

    private static VoiceStreamCommand DeserializeStreamCommand(byte[] payload)
    {
        var command = JsonSerializer.Deserialize<VoiceStreamCommand>(payload, JsonOptions);
        return command ?? new VoiceStreamCommand { Type = "unknown" };
    }

    private readonly record struct VoiceStreamSocketMessage(WebSocketMessageType MessageType, byte[] Payload);

    private sealed class VoiceStreamCommand
    {
        public string Type { get; set; } = string.Empty;

        public string? ContentType { get; set; }

        public string? FileName { get; set; }

        public string? Text { get; set; }
    }
}
