using CAT.Controllers.DTO.AiAssistant;

namespace CAT.Services.Interfaces;

public interface IAiAssistantService
{
    Task<AiAssistantResponseDTO> CreateTextDraftAsync(
        Guid organizationId,
        AiAssistantTextRequestDTO request,
        CancellationToken cancellationToken = default);

    Task<AiAssistantVoiceResponseDTO> CreateVoiceDraftAsync(
        Guid organizationId,
        Stream audio,
        string fileName,
        string? contentType,
        string? clientRequestId,
        string? conversationId,
        CancellationToken cancellationToken = default);

    Task<AiAssistantResponseDTO> SelectDraftCandidateAsync(
        Guid organizationId,
        Guid draftId,
        int itemIndex,
        AiAssistantSelectCandidateRequestDTO request,
        CancellationToken cancellationToken = default);

    Task<AiAssistantResponseDTO> SelectReadCandidateAsync(
        Guid organizationId,
        AiAssistantSelectReadCandidateRequestDTO request,
        CancellationToken cancellationToken = default);

    Task<AiAssistantConfirmResponseDTO> ConfirmDraftAsync(
        Guid organizationId,
        Guid draftId,
        AiAssistantConfirmRequestDTO request,
        CancellationToken cancellationToken = default);
}
