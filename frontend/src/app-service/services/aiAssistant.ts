import { FetchBaseQueryError } from '@reduxjs/toolkit/query';
import { api } from './api';

export type AiAssistantToolName =
    | 'unsupported'
    | 'find_animal'
    | 'get_animal_card'
    | 'get_animal_parents'
    | 'get_weight_history'
    | 'get_pregnancies_to_check'
    | 'list_groups'
    | 'create_weight'
    | 'create_daily_action'
    | 'create_insemination';

export type AiAssistantDraftStatus =
    | 'preview'
    | 'final_answer'
    | 'unsupported'
    | 'canceled'
    | 'committed'
    | 'confirm_expired'
    | 'cannot_commit';

export type AiAssistantResponseKind = 'final_answer' | 'clarification' | 'preview';

export type AiAssistantConfirmKind = 'commit_report' | 'clarification' | 'canceled';

export type AiWriteItemStatus =
    | 'resolved'
    | 'ambiguous'
    | 'not_found'
    | 'invalid'
    | 'committed'
    | 'failed'
    | 'skipped';

export type AiDisambiguationCandidate = {
    id: string;
    label?: string;
    display?: string;
    animal?: {
        tag?: string | null;
        tagNumber?: string | null;
        type?: string | null;
        status?: string | null;
        birthDate?: string | null;
        groupName?: string | null;
        breed?: string | null;
        identifiers?: Array<{
            name: string;
            value?: string | null;
        }>;
    };
};

export type AiValidationError = {
    rule: string;
    field: string;
    message: string;
    path?: string | null;
    retryable?: boolean;
};

export type AiWriteItemPreview = {
    index: number;
    idempotencyKey: string;
    tag?: string | null;
    status: AiWriteItemStatus;
    canCommit: boolean;
    message: string;
    candidates: AiDisambiguationCandidate[];
    preview?: unknown;
    validationErrors: AiValidationError[];
};

export type AiWritePreview = {
    schemaVersion: string;
    toolName: AiAssistantToolName;
    batchIdempotencyKey?: string | null;
    total: number;
    commitReady: number;
    ambiguous: number;
    notFound: number;
    invalid: number;
    requiresPartialConfirm: boolean;
    items: AiWriteItemPreview[];
    text: string;
    voice: string;
};

export type AiAssistantPreview = {
    toolName: AiAssistantToolName;
    summary: string;
    arguments?: unknown;
    warnings: string[];
};

export type AiAssistantTextRequest = {
    text: string;
    clientRequestId?: string;
    conversationId?: string;
};

export type AiAssistantRawResponse = {
    draftId: string;
    status: AiAssistantDraftStatus;
    message: string;
    preview?: AiAssistantPreview | null;
    expiresAtUtc: string;
    conversationId: string;
};

export type AiAssistantSelectCandidateRequest = {
    draftId: string;
    itemIndex: number;
    candidateId: string;
};

export type AiAssistantSelectReadCandidateRequest = {
    toolName: AiAssistantToolName;
    candidateId: string;
    conversationId?: string;
};

export type AiAssistantResponse = AiAssistantRawResponse & {
    kind: AiAssistantResponseKind;
    writePreview?: AiWritePreview;
};

export type AiAssistantVoiceResponse = AiAssistantResponse & {
    transcript: string;
    rawTranscript: string;
    asrModel: string;
    asrLatencySeconds?: number | null;
};

export type AiAssistantConfirmRequest = {
    draftId: string;
    idempotencyKey?: string;
    confirmPartial?: boolean;
};

export type AiAssistantCancelRequest = {
    draftId: string;
    idempotencyKey?: string;
};

export type AiWriteCommitItemReport = {
    index: number;
    idempotencyKey: string;
    tag?: string | null;
    status: AiWriteItemStatus;
    message: string;
};

export type AiWriteCommitReport = {
    schemaVersion: string;
    toolName: AiAssistantToolName;
    draftId: string;
    total: number;
    committed: number;
    failed: number;
    skipped: number;
    items: AiWriteCommitItemReport[];
    text: string;
    voice: string;
};

export type AiAssistantRawConfirmResponse = {
    draftId: string;
    status: AiAssistantDraftStatus;
    message: string;
    commitResult?: AiWriteCommitReport | null;
};

export type AiAssistantConfirmResponse = AiAssistantRawConfirmResponse & {
    kind: AiAssistantConfirmKind;
};

export type AiAssistantError = {
    status?: number | string;
    message: string;
    errorText?: string;
    data?: unknown;
};

const isRecord = (value: unknown): value is Record<string, unknown> =>
    typeof value === 'object' && value !== null;

const isAiWritePreview = (value: unknown): value is AiWritePreview =>
    isRecord(value) &&
    typeof value.toolName === 'string' &&
    typeof value.commitReady === 'number' &&
    Array.isArray(value.items);

const getWritePreview = (response: AiAssistantRawResponse): AiWritePreview | undefined => {
    const args = response.preview?.arguments;
    return isAiWritePreview(args) ? args : undefined;
};

const toAssistantKind = (response: AiAssistantRawResponse): AiAssistantResponseKind => {
    const writePreview = getWritePreview(response);
    if (response.status === 'preview' && writePreview?.commitReady === 0) {
        return 'clarification';
    }
    if (response.status === 'preview') {
        return 'preview';
    }
    return 'final_answer';
};

const toConfirmKind = (response: AiAssistantRawConfirmResponse): AiAssistantConfirmKind => {
    if (response.status === 'canceled') {
        return 'canceled';
    }
    if (response.status === 'cannot_commit' || response.status === 'confirm_expired') {
        return 'clarification';
    }
    return 'commit_report';
};

export const normalizeAiAssistantError = (error: unknown): AiAssistantError => {
    if (isRecord(error) && 'status' in error) {
        const queryError = error as FetchBaseQueryError;
        const data = queryError.data;
        const errorText = isRecord(data) && typeof data.errorText === 'string'
            ? data.errorText
            : undefined;
        const message = errorText || ('error' in queryError && typeof queryError.error === 'string'
            ? queryError.error
            : 'Сервис временно недоступен. Попробуйте позже.');

        return {
            status: queryError.status,
            message,
            errorText,
            data,
        };
    }

    if (error instanceof Error) {
        return { message: error.message };
    }

    return { message: 'Сервис временно недоступен. Попробуйте позже.' };
};

export const getAiAssistantVoiceStreamUrl = (
    organizationId: string,
    clientRequestId: string,
    conversationId: string
) => {
    const apiUrl = (import.meta.env.VITE_API_URL || '').replace(/\/$/, '');
    const baseUrl = apiUrl.replace(/\/api$/, '') || window.location.origin;
    const wsBase = baseUrl.startsWith('https://')
        ? baseUrl.replace('https://', 'wss://')
        : baseUrl.replace('http://', 'ws://');
    const params = new URLSearchParams({
        organizationId,
        clientRequestId,
        conversationId,
    });

    return `${wsBase}/api/AiAssistant/voice/stream?${params.toString()}`;
};

export const aiAssistantApi = api.injectEndpoints({
    endpoints: (builder) => ({
        sendAiAssistantText: builder.mutation<AiAssistantResponse, AiAssistantTextRequest>({
            query: (body) => ({
                url: 'AiAssistant/text',
                method: 'POST',
                body,
            }),
            transformResponse: (response: AiAssistantRawResponse): AiAssistantResponse => ({
                ...response,
                kind: toAssistantKind(response),
                writePreview: getWritePreview(response),
            }),
            transformErrorResponse: normalizeAiAssistantError,
        }),
        sendAiAssistantVoice: builder.mutation<AiAssistantVoiceResponse, FormData>({
            query: (body) => ({
                url: 'AiAssistant/voice',
                method: 'POST',
                body,
            }),
            transformResponse: (response: AiAssistantRawResponse & {
                transcript: string;
                rawTranscript: string;
                asrModel: string;
                asrLatencySeconds?: number | null;
            }): AiAssistantVoiceResponse => ({
                ...response,
                kind: toAssistantKind(response),
                writePreview: getWritePreview(response),
            }),
            transformErrorResponse: normalizeAiAssistantError,
        }),
        confirmAiAssistantDraft: builder.mutation<
            AiAssistantConfirmResponse,
            AiAssistantConfirmRequest
        >({
            query: ({ draftId, idempotencyKey, confirmPartial }) => ({
                url: `AiAssistant/drafts/${draftId}/confirm`,
                method: 'POST',
                body: {
                    confirm: true,
                    idempotencyKey,
                    confirmPartial: Boolean(confirmPartial),
                },
            }),
            transformResponse: (
                response: AiAssistantRawConfirmResponse
            ): AiAssistantConfirmResponse => ({
                ...response,
                kind: toConfirmKind(response),
            }),
            transformErrorResponse: normalizeAiAssistantError,
        }),
        cancelAiAssistantDraft: builder.mutation<
            AiAssistantConfirmResponse,
            AiAssistantCancelRequest
        >({
            query: ({ draftId, idempotencyKey }) => ({
                url: `AiAssistant/drafts/${draftId}/confirm`,
                method: 'POST',
                body: {
                    confirm: false,
                    idempotencyKey,
                },
            }),
            transformResponse: (
                response: AiAssistantRawConfirmResponse
            ): AiAssistantConfirmResponse => ({
                ...response,
                kind: toConfirmKind(response),
            }),
            transformErrorResponse: normalizeAiAssistantError,
        }),
        selectAiAssistantDraftCandidate: builder.mutation<
            AiAssistantResponse,
            AiAssistantSelectCandidateRequest
        >({
            query: ({ draftId, itemIndex, candidateId }) => ({
                url: `AiAssistant/drafts/${draftId}/items/${itemIndex}/select`,
                method: 'POST',
                body: { candidateId },
            }),
            transformResponse: (response: AiAssistantRawResponse): AiAssistantResponse => ({
                ...response,
                kind: toAssistantKind(response),
                writePreview: getWritePreview(response),
            }),
            transformErrorResponse: normalizeAiAssistantError,
        }),
        selectAiAssistantReadCandidate: builder.mutation<
            AiAssistantResponse,
            AiAssistantSelectReadCandidateRequest
        >({
            query: (body) => ({
                url: 'AiAssistant/read/select',
                method: 'POST',
                body,
            }),
            transformResponse: (response: AiAssistantRawResponse): AiAssistantResponse => ({
                ...response,
                kind: toAssistantKind(response),
                writePreview: getWritePreview(response),
            }),
            transformErrorResponse: normalizeAiAssistantError,
        }),
    }),
});

export const {
    useSendAiAssistantTextMutation,
    useSendAiAssistantVoiceMutation,
    useConfirmAiAssistantDraftMutation,
    useCancelAiAssistantDraftMutation,
    useSelectAiAssistantDraftCandidateMutation,
    useSelectAiAssistantReadCandidateMutation,
} = aiAssistantApi;
