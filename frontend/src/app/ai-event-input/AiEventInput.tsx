import { AudioOutlined, LoadingOutlined, SendOutlined } from '@ant-design/icons';
import {
    Alert,
    Button,
    Card,
    Flex,
    Input,
    Modal,
    Space,
    Tag,
    Typography,
    message,
} from 'antd';
import { useEffect, useMemo, useRef, useState } from 'react';
import {
    AiAssistantConfirmResponse,
    AiAssistantToolName,
    AiDisambiguationCandidate,
    AiAssistantError,
    AiAssistantResponse,
    AiAssistantVoiceResponse,
    AiWriteCommitReport,
    AiWriteItemPreview,
    AiWriteItemStatus,
    AiWritePreview,
    getAiAssistantVoiceStreamUrl,
    normalizeAiAssistantError,
    useCancelAiAssistantDraftMutation,
    useConfirmAiAssistantDraftMutation,
    useSendAiAssistantTextMutation,
    useSelectAiAssistantDraftCandidateMutation,
    useSelectAiAssistantReadCandidateMutation,
} from '../../app-service/services/aiAssistant';
import {
    AiExistingFormValues,
    mapAiPreviewToExistingFormValues,
} from './aiFormMapping';
import styles from './AiEventInput.module.css';

const { TextArea } = Input;

type AiDialogStatus =
    | 'loading'
    | 'final_answer'
    | 'clarification'
    | 'preview'
    | 'confirmed'
    | 'canceled'
    | 'expired'
    | 'partial_success'
    | 'error';

type AiDialogTurn = {
    id: string;
    userText: string;
    status: AiDialogStatus;
    message: string;
    draftId?: string;
    expiresAtUtc?: string;
    preview?: AiWritePreview;
    readResolution?: AiReadResolution;
    readToolName?: AiAssistantToolName;
    commitReport?: AiWriteCommitReport;
    error?: AiAssistantError;
    createdAt: string;
};

type AiReadResolution = {
    state?: string;
    entity?: string;
    input?: {
        tag?: string;
    };
    candidates?: AiDisambiguationCandidate[];
    totalMatches?: number;
    message?: string;
};

const createId = () =>
    typeof crypto !== 'undefined' && 'randomUUID' in crypto
        ? crypto.randomUUID()
        : `${Date.now()}-${Math.random().toString(16).slice(2)}`;

const itemStatusLabels: Partial<Record<AiWriteItemStatus, string>> = {
    resolved: 'найдено',
    ambiguous: 'нужно выбрать',
    not_found: 'не найдено',
    invalid: 'ошибка',
    committed: 'сохранено',
    failed: 'ошибка',
    skipped: 'пропущено',
};

const isRecord = (value: unknown): value is Record<string, unknown> =>
    typeof value === 'object' && value !== null;

const getErrorDraftResponse = (
    error: AiAssistantError
): Pick<AiAssistantConfirmResponse, 'draftId' | 'status' | 'message'> | undefined => {
    if (!isRecord(error.data)) {
        return undefined;
    }

    const { draftId, status, message: responseMessage } = error.data;
    if (typeof draftId !== 'string' || typeof status !== 'string' || typeof responseMessage !== 'string') {
        return undefined;
    }

    return { draftId, status: status as AiAssistantConfirmResponse['status'], message: responseMessage };
};

const isAiWritePreview = (value: unknown): value is AiWritePreview =>
    isRecord(value) &&
    typeof value.toolName === 'string' &&
    typeof value.commitReady === 'number' &&
    Array.isArray(value.items);

const getWritePreviewFromResponse = (
    response: AiAssistantResponse | AiAssistantVoiceResponse
): AiWritePreview | undefined => {
    if (response.writePreview) {
        return response.writePreview;
    }

    return isAiWritePreview(response.preview?.arguments)
        ? response.preview.arguments
        : undefined;
};

const getTurnStatusFromResponse = (response: AiAssistantResponse | AiAssistantVoiceResponse): AiDialogStatus => {
    if (response.kind === 'clarification') {
        return 'clarification';
    }
    if (response.kind === 'preview') {
        return 'preview';
    }
    const writePreview = getWritePreviewFromResponse(response);
    if (response.status === 'preview' && writePreview?.commitReady === 0) {
        return 'clarification';
    }
    if (response.status === 'preview') {
        return 'preview';
    }
    return 'final_answer';
};

const getReadResolution = (value: unknown): AiReadResolution | undefined => {
    const candidate = isRecord(value) && isRecord(value.resolution)
        ? value.resolution
        : value;

    if (!isRecord(candidate) || candidate.state !== 'ambiguous' || !Array.isArray(candidate.candidates)) {
        return undefined;
    }

    return candidate as AiReadResolution;
};

const getReadItemsText = (response: AiAssistantResponse | AiAssistantVoiceResponse): string | undefined => {
    const toolName = response.preview?.toolName;
    const args = response.preview?.arguments;
    if (!isRecord(args) || !Array.isArray(args.items) || args.items.length === 0) {
        return undefined;
    }

    if (toolName === 'get_pregnancies_to_check') {
        return args.items
            .map((item, index) => {
                if (!isRecord(item)) {
                    return null;
                }
                const tag = typeof item.cowTag === 'string' ? item.cowTag : `#${index + 1}`;
                const date = typeof item.inseminationDate === 'string' ? `, осеменение ${item.inseminationDate}` : '';
                const type = typeof item.inseminationType === 'string' ? `, ${item.inseminationType}` : '';
                const bull = typeof item.bullTag === 'string' ? `, бык ${item.bullTag}` : '';
                return `Бирка ${tag}${type}${date}${bull}`;
            })
            .filter(Boolean)
            .join('\n');
    }

    if (toolName === 'list_groups') {
        return args.items
            .map((item) => {
                if (!isRecord(item) || typeof item.name !== 'string') {
                    return null;
                }
                const count = typeof item.animalCount === 'number' ? `: ${item.animalCount}` : '';
                return `${item.name}${count}`;
            })
            .filter(Boolean)
            .join('\n');
    }

    if (toolName === 'get_weight_history') {
        return args.items
            .map((item) => {
                if (!isRecord(item)) {
                    return null;
                }
                const date = typeof item.date === 'string' ? item.date : '';
                const weight = typeof item.weight === 'number' ? `${item.weight} кг` : '';
                return [date, weight].filter(Boolean).join(': ');
            })
            .filter(Boolean)
            .join('\n');
    }

    return undefined;
};

const renderMultiline = (text: string) => {
    const lines = text.split('\n');
    return lines.map((line, index) => (
        <span key={`${index}-${line}`}>
            {line}
            {index < lines.length - 1 && <br />}
        </span>
    ));
};

const getAnimalTag = (candidate: AiDisambiguationCandidate) =>
    candidate.animal?.tag ?? candidate.animal?.tagNumber ?? null;

const getCandidateLabel = (candidate: AiDisambiguationCandidate) =>
    candidate.label ?? candidate.display ?? getAnimalTag(candidate) ?? candidate.id;

const getTurnStatusFromCommit = (response: AiAssistantConfirmResponse): AiDialogStatus => {
    if (response.kind === 'canceled') {
        return 'canceled';
    }

    const report = response.commitResult;
    if (report && (report.failed > 0 || report.skipped > 0)) {
        return 'partial_success';
    }

    return 'confirmed';
};

const getErrorStatus = (error: AiAssistantError): AiDialogStatus => {
    const draftResponse = getErrorDraftResponse(error);
    if (draftResponse?.status === 'confirm_expired') {
        return 'expired';
    }
    if (draftResponse?.status === 'cannot_commit') {
        return 'clarification';
    }
    return 'error';
};

type AiEventInputProps = {
    onApplyToForm?: (values: AiExistingFormValues) => void;
    command?: AiAssistantCommand;
};

export type AiAssistantCommand = {
    type: 'new' | 'history';
    nonce: number;
};

type StoredConversation = {
    id: string;
    title: string;
    updatedAt: string;
    turns: AiDialogTurn[];
};

const getConversationStorageKey = () => {
    const user = JSON.parse(localStorage.getItem('user') || '{}') as { organizationId?: string; id?: string };
    return `cattle-track:ai-conversations:${user.organizationId ?? 'none'}:${user.id ?? 'anonymous'}`;
};

export const AiEventInput = ({ onApplyToForm, command }: AiEventInputProps) => {
    const [text, setText] = useState('');
    const [turns, setTurns] = useState<AiDialogTurn[]>([]);
    const [conversationId, setConversationId] = useState(createId);
    const [conversations, setConversations] = useState<StoredConversation[]>([]);
    const [isHistoryOpen, setIsHistoryOpen] = useState(false);
    const [isRecording, setIsRecording] = useState(false);
    const [liveTranscript, setLiveTranscript] = useState('');
    const mediaRecorderRef = useRef<MediaRecorder | null>(null);
    const mediaStreamRef = useRef<MediaStream | null>(null);
    const audioChunksRef = useRef<Blob[]>([]);
    const liveTranscriptRef = useRef('');
    const voiceSocketRef = useRef<WebSocket | null>(null);
    const voiceTurnIdRef = useRef<string | null>(null);
    const voiceChunkSendQueueRef = useRef<Promise<void>[]>([]);
    const [sendText, { isLoading: isSending }] = useSendAiAssistantTextMutation();
    const [isTranscribing, setIsTranscribing] = useState(false);
    const [confirmDraft, { isLoading: isConfirming }] = useConfirmAiAssistantDraftMutation();
    const [cancelDraft, { isLoading: isCanceling }] = useCancelAiAssistantDraftMutation();
    const [selectDraftCandidate, { isLoading: isSelecting }] = useSelectAiAssistantDraftCandidateMutation();
    const [selectReadCandidate, { isLoading: isSelectingRead }] = useSelectAiAssistantReadCandidateMutation();
    const isBusy = isSending || isTranscribing || isConfirming || isCanceling || isSelecting || isSelectingRead;
    const storageKey = useMemo(getConversationStorageKey, []);

    useEffect(() => {
        try {
            const stored = JSON.parse(localStorage.getItem(storageKey) || '[]') as StoredConversation[];
            const valid = stored.filter((conversation) => conversation.id && Array.isArray(conversation.turns));
            setConversations(valid);
            const latest = valid[0];
            if (latest) {
                setConversationId(latest.id);
                setTurns(latest.turns);
            }
        } catch {
            localStorage.removeItem(storageKey);
        }
    }, [storageKey]);

    useEffect(() => {
        if (turns.length === 0) {
            return;
        }

        const updated: StoredConversation = {
            id: conversationId,
            title: turns[0]?.userText || 'Диалог с ассистентом',
            updatedAt: new Date().toISOString(),
            turns,
        };
        setConversations((current) => {
            const next = [updated, ...current.filter((conversation) => conversation.id !== conversationId)]
                .slice(0, 20);
            localStorage.setItem(storageKey, JSON.stringify(next));
            return next;
        });
    }, [conversationId, storageKey, turns]);

    useEffect(() => {
        if (!command) {
            return;
        }

        if (command.type === 'history') {
            setIsHistoryOpen(true);
            return;
        }

        setConversationId(createId());
        setTurns([]);
        setText('');
        setIsHistoryOpen(false);
    }, [command]);

    const updateTurn = (turnId: string, patch: Partial<AiDialogTurn>) => {
        setTurns((current) =>
            current.map((turn) => (turn.id === turnId ? { ...turn, ...patch } : turn))
        );
    };

    const clearLiveTranscript = (options?: { keepDraft?: boolean }) => {
        setLiveTranscript('');
        if (!options?.keepDraft) {
            liveTranscriptRef.current = '';
        }
    };

    const applyAssistantResponse = (
        turnId: string,
        response: AiAssistantResponse | AiAssistantVoiceResponse
    ) => {
        const readResolution = getReadResolution(response.preview?.arguments);
        const writePreview = getWritePreviewFromResponse(response);
        const readItemsText = getReadItemsText(response);
        updateTurn(turnId, {
            status: getTurnStatusFromResponse(response),
            message: readResolution
                ? `Найдено вариантов: ${readResolution.totalMatches ?? readResolution.candidates?.length ?? 0}. Выберите нужное животное.`
                : readItemsText
                    ? `${response.message}\n${readItemsText}`
                : response.message,
            draftId: response.draftId,
            expiresAtUtc: response.expiresAtUtc,
            preview: writePreview,
            readResolution,
            readToolName: readResolution ? response.preview?.toolName : undefined,
            ...('transcript' in response
                ? {
                    userText: response.transcript,
                }
                : {}),
        });
    };

    const handleSend = async (textOverride?: string) => {
        const trimmed = (textOverride ?? text).trim();
        if (!trimmed || isBusy) {
            return;
        }

        const turnId = createId();
        if (!textOverride) {
            setText('');
        }
        setTurns((current) => [
            {
                id: turnId,
                userText: trimmed,
                status: 'loading',
                message: 'Обрабатываю запрос...',
                createdAt: new Date().toISOString(),
            },
            ...current,
        ]);

        try {
            const response = await sendText({
                text: trimmed,
                clientRequestId: createId(),
                conversationId,
            }).unwrap();

            applyAssistantResponse(turnId, response);
        } catch (err) {
            const error = normalizeAiAssistantError(err);
            updateTurn(turnId, {
                status: getErrorStatus(error),
                message: error.message,
                error,
            });
            message.error(error.message);
        }
    };

    const handleSelectAnimalCandidate = async (
        turn: AiDialogTurn,
        candidate: AiDisambiguationCandidate
    ) => {
        if (isBusy) {
            return;
        }

        const toolName = turn.readToolName;
        if (!toolName) {
            message.error('Не удалось определить действие для выбранного животного.');
            return;
        }

        updateTurn(turn.id, {
            status: 'loading',
            message: `Открываю выбранное животное: ${getCandidateLabel(candidate)}...`,
            readResolution: undefined,
            readToolName: undefined,
        });

        try {
            const response = await selectReadCandidate({
                toolName,
                candidateId: candidate.id,
                conversationId,
            }).unwrap();
            applyAssistantResponse(turn.id, response);
        } catch (err) {
            const error = normalizeAiAssistantError(err);
            updateTurn(turn.id, {
                status: getErrorStatus(error),
                message: error.message,
                error,
            });
            message.error(error.message);
        }
    };

    const handleVoiceStreamEvent = (event: MessageEvent<string>) => {
        if (!voiceTurnIdRef.current) {
            return;
        }

        const turnId = voiceTurnIdRef.current;
        const data = JSON.parse(event.data) as { type: string; payload?: unknown };
        const payload = isRecord(data.payload) ? data.payload : {};

        if (data.type === 'partial_transcript' && typeof payload.text === 'string') {
            const transcript = payload.text.trim();
            liveTranscriptRef.current = transcript;
            setLiveTranscript(transcript);
            updateTurn(turnId, {
                userText: transcript || 'Голосовой запрос',
                message: 'Слушаю...',
            });
            return;
        }

        if (data.type === 'asr_started') {
            setIsTranscribing(true);
            clearLiveTranscript({ keepDraft: true });
            updateTurn(turnId, {
                userText: liveTranscriptRef.current.trim() || 'Голосовой запрос',
                message: 'Распознаю речь...',
            });
            return;
        }

        if (data.type === 'assistant_response') {
            setIsTranscribing(false);
            applyAssistantResponse(turnId, payload as AiAssistantVoiceResponse);
            voiceTurnIdRef.current = null;
            clearLiveTranscript();
            return;
        }

        if (data.type === 'error' && typeof payload.message === 'string') {
            setIsTranscribing(false);
            updateTurn(turnId, {
                status: 'error',
                message: payload.message,
            });
            voiceTurnIdRef.current = null;
            clearLiveTranscript();
            message.error(payload.message);
        }
    };

    const createVoiceTurn = () => {
        const turnId = createId();
        setTurns((current) => [
            {
                id: turnId,
                userText: 'Голосовой запрос',
                status: 'loading',
                message: 'Слушаю...',
                createdAt: new Date().toISOString(),
            },
            ...current,
        ]);
        voiceTurnIdRef.current = turnId;
    };

    const startVoiceSocket = (mimeType: string) => {
        const user = JSON.parse(localStorage.getItem('user') || '{}') as { organizationId?: string };
        if (!user.organizationId || user.organizationId === 'Нет организации') {
            throw new Error('Для голосового запроса нужна выбранная организация.');
        }

        createVoiceTurn();
        const socket = new WebSocket(getAiAssistantVoiceStreamUrl(user.organizationId, createId(), conversationId));
        voiceSocketRef.current = socket;
        socket.onmessage = handleVoiceStreamEvent;
        socket.onclose = () => {
            voiceSocketRef.current = null;
        };

        return new Promise<WebSocket>((resolve, reject) => {
            socket.onopen = () => {
                socket.send(JSON.stringify({
                    type: 'start',
                    contentType: mimeType,
                    fileName: 'voice.webm',
                }));
                resolve(socket);
            };
            socket.onerror = () => {
                voiceSocketRef.current = null;
                reject(new Error('Не удалось открыть поток голосового ввода.'));
            };
        });
    };

    const stopRecording = () => {
        if (mediaRecorderRef.current?.state === 'recording') {
            mediaRecorderRef.current.stop();
        }
        mediaStreamRef.current?.getTracks().forEach((track) => track.stop());
        mediaStreamRef.current = null;
        setIsRecording(false);
        clearLiveTranscript({ keepDraft: true });
    };

    const startRecording = async () => {
        if (!navigator.mediaDevices?.getUserMedia || typeof MediaRecorder === 'undefined') {
            message.error('Браузер не поддерживает запись голоса.');
            return;
        }

        try {
            const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
            mediaStreamRef.current = stream;
            audioChunksRef.current = [];
            voiceChunkSendQueueRef.current = [];
            setLiveTranscript('');
            liveTranscriptRef.current = '';

            const mimeType = MediaRecorder.isTypeSupported('audio/webm;codecs=opus')
                ? 'audio/webm;codecs=opus'
                : 'audio/webm';
            const socket = await startVoiceSocket(mimeType);
            const recorder = new MediaRecorder(stream, { mimeType });
            mediaRecorderRef.current = recorder;

            recorder.ondataavailable = (event) => {
                if (event.data.size > 0) {
                    audioChunksRef.current.push(event.data);
                    const sendChunk = event.data.arrayBuffer().then((chunk) => {
                        if (socket.readyState === WebSocket.OPEN) {
                            socket.send(chunk);
                        }
                    });
                    voiceChunkSendQueueRef.current.push(sendChunk);
                }
            };
            recorder.onstop = () => {
                const pendingSends = [...voiceChunkSendQueueRef.current];
                void Promise.allSettled(pendingSends).then(() => {
                    audioChunksRef.current = [];
                    voiceChunkSendQueueRef.current = [];
                    if (socket.readyState === WebSocket.OPEN) {
                        socket.send(JSON.stringify({ type: 'stop' }));
                    }
                });
            };
            recorder.start(500);
            setIsRecording(true);
        } catch (err) {
            const error = err instanceof Error ? err.message : 'Не удалось получить доступ к микрофону.';
            voiceTurnIdRef.current = null;
            voiceChunkSendQueueRef.current = [];
            voiceSocketRef.current?.close();
            voiceSocketRef.current = null;
            mediaStreamRef.current?.getTracks().forEach((track) => track.stop());
            mediaStreamRef.current = null;
            setIsRecording(false);
            clearLiveTranscript();
            message.error(error);
        }
    };

    const handleVoiceClick = () => {
        if (isRecording) {
            stopRecording();
            return;
        }

        if (isBusy) {
            return;
        }

        void startRecording();
    };

    const handleConfirm = async (turn: AiDialogTurn, confirmPartial = false) => {
        if (!turn.draftId || isBusy) {
            return;
        }

        try {
            const response = await confirmDraft({
                draftId: turn.draftId,
                idempotencyKey: `${turn.id}:${turn.draftId}:confirm`,
                confirmPartial,
            }).unwrap();

            updateTurn(turn.id, {
                status: getTurnStatusFromCommit(response),
                message: response.message,
                commitReport: response.commitResult ?? undefined,
            });
            message.success(response.message);
        } catch (err) {
            const error = normalizeAiAssistantError(err);
            const draftError = getErrorDraftResponse(error);
            updateTurn(turn.id, {
                status: getErrorStatus(error),
                message: draftError?.message ?? error.message,
                error,
            });
            message.error(draftError?.message ?? error.message);
        }
    };

    const handleCancel = async (turn: AiDialogTurn) => {
        if (!turn.draftId || isBusy) {
            return;
        }

        try {
            const response = await cancelDraft({
                draftId: turn.draftId,
                idempotencyKey: createId(),
            }).unwrap();

            updateTurn(turn.id, {
                status: 'canceled',
                message: response.message,
            });
        } catch (err) {
            const error = normalizeAiAssistantError(err);
            updateTurn(turn.id, {
                status: getErrorStatus(error),
                message: error.message,
                error,
            });
            message.error(error.message);
        }
    };

    const handleSelectWriteCandidate = async (
        turn: AiDialogTurn,
        item: AiWriteItemPreview,
        candidate: AiDisambiguationCandidate
    ) => {
        if (!turn.draftId || isBusy) {
            return;
        }

        updateTurn(turn.id, {
            status: 'loading',
            message: `Выбрано: ${getCandidateLabel(candidate)}. Готовлю черновик...`,
        });

        try {
            const response = await selectDraftCandidate({
                draftId: turn.draftId,
                itemIndex: item.index,
                candidateId: candidate.id,
            }).unwrap();
            applyAssistantResponse(turn.id, response);
        } catch (err) {
            const error = normalizeAiAssistantError(err);
            updateTurn(turn.id, {
                status: getErrorStatus(error),
                message: error.message,
                error,
            });
            message.error(error.message);
        }
    };

    const selectConversation = (conversation: StoredConversation) => {
        setConversationId(conversation.id);
        setTurns(conversation.turns);
        setText('');
        setIsHistoryOpen(false);
    };

    const orderedTurns = [...turns].reverse();
    const voiceStateText = isRecording
        ? 'Слушаю'
        : isTranscribing
            ? 'Обрабатываю'
            : isSending || isConfirming || isCanceling
                ? 'Обрабатываю'
                : 'Нажмите, чтобы говорить';

    return (
        <section className={styles.aiEventInput}>
            <div className={styles.thread}>
                <AssistantWelcome />
                {orderedTurns.map((turn) => (
                    <DialogTurnContent
                        key={turn.id}
                        turn={turn}
                        isBusy={isBusy}
                        canApplyToForm={Boolean(onApplyToForm)}
                        onConfirm={() => handleConfirm(turn)}
                        onConfirmPartial={() => handleConfirm(turn, true)}
                        onCancel={() => handleCancel(turn)}
                        onApplyToForm={() => {
                            if (turn.preview && onApplyToForm) {
                                onApplyToForm(mapAiPreviewToExistingFormValues(turn.preview));
                            }
                        }}
                        onSelectAnimalCandidate={(candidate) =>
                            void handleSelectAnimalCandidate(turn, candidate)
                        }
                        onSelectWriteCandidate={(item, candidate) =>
                            void handleSelectWriteCandidate(turn, item, candidate)
                        }
                    />
                ))}
            </div>

            {(isRecording || liveTranscript) && (
                <div className={styles.livePanel}>
                    <div className={styles.equalizer} aria-hidden='true'>
                        <span />
                        <span />
                        <span />
                        <span />
                    </div>
                    <Typography.Text className={styles.liveTranscript}>
                        {liveTranscript || 'Говорите...'}
                    </Typography.Text>
                </div>
            )}

            <div className={styles.composerShell}>
                <TextArea
                    className={styles.textArea}
                    value={text}
                    placeholder='Напишите или нажмите микрофон...'
                    autoSize={{ minRows: 1, maxRows: 4 }}
                    disabled={isBusy}
                    onChange={(event) => setText(event.target.value)}
                    onPressEnter={(event) => {
                        if (!event.shiftKey) {
                            event.preventDefault();
                            void handleSend();
                        }
                    }}
                />
                <Button
                    className={styles.sendButton}
                    type='primary'
                    icon={<SendOutlined />}
                    loading={isSending}
                    disabled={!text.trim() || isBusy}
                    onClick={() => void handleSend()}
                />
            </div>

            <button
                className={`${styles.voiceDock} ${isRecording ? styles.voiceDock_recording : ''}`}
                type='button'
                disabled={!isRecording && isBusy}
                onClick={handleVoiceClick}
                aria-label={isRecording ? 'Остановить запись' : 'Начать голосовой запрос'}
            >
                <span className={styles.voiceButtonShell}>
                    <span className={styles.primaryMicButton}>
                        {isBusy && !isRecording ? <LoadingOutlined /> : <AudioOutlined />}
                    </span>
                </span>
                <span className={styles.voiceState}>{voiceStateText}</span>
            </button>

            <Typography.Text className={styles.disclaimer}>
                AI может ошибаться. Проверяйте важную информацию.
            </Typography.Text>

            <Modal
                title='История диалогов'
                open={isHistoryOpen}
                footer={null}
                onCancel={() => setIsHistoryOpen(false)}
            >
                <div className={styles.historyList}>
                    {conversations.length === 0 && (
                        <Typography.Text type='secondary'>Диалогов пока нет.</Typography.Text>
                    )}
                    {conversations.map((conversation) => (
                        <button
                            className={`${styles.historyItem} ${conversation.id === conversationId ? styles.historyItem_active : ''}`}
                            key={conversation.id}
                            type='button'
                            onClick={() => selectConversation(conversation)}
                        >
                            <Typography.Text strong>{conversation.title}</Typography.Text>
                            <Typography.Text type='secondary'>
                                {new Date(conversation.updatedAt).toLocaleString('ru-RU', {
                                    day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit',
                                })}
                            </Typography.Text>
                        </button>
                    ))}
                </div>
            </Modal>
        </section>
    );
};

const AssistantWelcome = () => (
    <div className={styles.welcomeCard}>
        <div className={styles.assistantMeta}>
            <span className={styles.assistantAvatar}>
                <AudioOutlined />
            </span>
            <Typography.Text>Ассистент · {new Date().toLocaleTimeString('ru-RU', {
                hour: '2-digit',
                minute: '2-digit',
            })}</Typography.Text>
        </div>
        <div className={styles.assistantMessage}>
            <Typography.Paragraph className={styles.welcomeText}>
                Здравствуйте!
                <br />
                Я AI ассистент Cattle Track. Могу найти животное, открыть карточку,
                показать родителей, историю веса, группы и коров для проверки
                стельности. Для записей подготовлю черновик и сохраню только после
                подтверждения.
            </Typography.Paragraph>
        </div>
    </div>
);

type DialogTurnContentProps = {
    turn: AiDialogTurn;
    isBusy: boolean;
    canApplyToForm: boolean;
    onConfirm: () => void;
    onConfirmPartial: () => void;
    onCancel: () => void;
    onApplyToForm: () => void;
    onSelectAnimalCandidate: (candidate: AiDisambiguationCandidate) => void;
    onSelectWriteCandidate: (item: AiWriteItemPreview, candidate: AiDisambiguationCandidate) => void;
};

const DialogTurnContent = ({
    turn,
    isBusy,
    canApplyToForm,
    onConfirm,
    onConfirmPartial,
    onCancel,
    onApplyToForm,
    onSelectAnimalCandidate,
    onSelectWriteCandidate,
}: DialogTurnContentProps) => {
    const showConfirm = turn.status === 'preview' &&
        Boolean(turn.preview?.commitReady) &&
        !turn.preview?.requiresPartialConfirm;
    const showPartialConfirm = turn.status === 'preview' &&
        Boolean(turn.preview?.commitReady) &&
        Boolean(turn.preview?.requiresPartialConfirm);
    const showApplyToForm = canApplyToForm && Boolean(turn.preview?.commitReady);

    return (
        <div className={styles.turnGroup}>
            <div className={styles.userMessage}>
                <div className={styles.userBubble}>
                    <Typography.Text>{turn.userText}</Typography.Text>
                </div>
                <Typography.Text className={styles.userTime}>
                    {new Date(turn.createdAt).toLocaleTimeString('ru-RU', {
                        hour: '2-digit',
                        minute: '2-digit',
                    })}
                </Typography.Text>
            </div>

            <div className={styles.assistantMeta}>
                <span className={styles.assistantAvatar}>
                    <AudioOutlined />
                </span>
                <Typography.Text>Ассистент · {new Date(turn.createdAt).toLocaleTimeString('ru-RU', {
                    hour: '2-digit',
                    minute: '2-digit',
                })}</Typography.Text>
            </div>

            <div className={styles.assistantBubble}>
                {turn.status === 'expired' && (
                    <Alert
                        type='warning'
                        showIcon
                        message='Срок подтверждения истёк'
                    />
                )}

                {turn.status === 'error' && (
                    <Alert
                        type='error'
                        showIcon
                        message='Ошибка'
                        description={turn.message}
                    />
                )}

                {!turn.readResolution && !['error', 'expired'].includes(turn.status) && (
                    <Typography.Paragraph className={styles.message}>
                        {renderMultiline(turn.message)}
                    </Typography.Paragraph>
                )}

                {turn.readResolution && (
                    <AnimalDisambiguation
                        resolution={turn.readResolution}
                        isBusy={isBusy}
                        onSelect={onSelectAnimalCandidate}
                    />
                )}

                {turn.preview && (
                    <WritePreview
                        preview={turn.preview}
                        isBusy={isBusy}
                        onSelectCandidate={onSelectWriteCandidate}
                    />
                )}
                {turn.commitReport && <CommitReport report={turn.commitReport} />}

                {(showConfirm || showPartialConfirm || showApplyToForm) && (
                    <div className={styles.actions}>
                        {showApplyToForm && (
                            <Button disabled={isBusy} onClick={onApplyToForm}>
                                Заполнить форму
                            </Button>
                        )}
                        {showConfirm && (
                            <>
                                <Button disabled={isBusy} onClick={onCancel}>
                                    Отменить
                                </Button>
                                <Button type='primary' disabled={isBusy} onClick={onConfirm}>
                                    Подтвердить
                                </Button>
                            </>
                        )}
                        {showPartialConfirm && (
                            <>
                                <Button disabled={isBusy} onClick={onCancel}>
                                    Отменить
                                </Button>
                                <Button type='primary' disabled={isBusy} onClick={onConfirmPartial}>
                                    Сохранить готовые
                                </Button>
                            </>
                        )}
                    </div>
                )}
            </div>
        </div>
    );
};

const AnimalDisambiguation = ({
    resolution,
    isBusy,
    onSelect,
}: {
    resolution: AiReadResolution;
    isBusy: boolean;
    onSelect: (candidate: AiDisambiguationCandidate) => void;
}) => {
    const candidates = resolution.candidates ?? [];

    return (
        <div className={styles.disambiguation}>
            <Alert
                type='info'
                showIcon
                message={`Нашлось несколько животных: ${resolution.totalMatches ?? candidates.length}`}
                description='Выберите нужную карточку по дате рождения, группе или дополнительным идентификаторам.'
            />
            <div className={styles.candidateGrid}>
                {candidates.map((candidate) => {
                    const animal = candidate.animal;
                    const tag = getAnimalTag(candidate);
                    const details = [
                        animal?.type,
                        animal?.status,
                        animal?.birthDate ? `рожд. ${animal.birthDate}` : null,
                        animal?.groupName ? `группа ${animal.groupName}` : null,
                        animal?.breed,
                    ].filter(Boolean);

                    return (
                        <Card
                            key={candidate.id}
                            className={styles.candidateCard}
                            size='small'
                        >
                            <Flex vertical gap={8}>
                                <Flex justify='space-between' align='flex-start' gap={8}>
                                    <div>
                                        <Typography.Text strong>
                                            {tag ? `Бирка ${tag}` : getCandidateLabel(candidate)}
                                        </Typography.Text>
                                        {details.length > 0 && (
                                            <Typography.Paragraph className={styles.candidateDetails}>
                                                {details.join(', ')}
                                            </Typography.Paragraph>
                                        )}
                                    </div>
                                    <Button
                                        type='primary'
                                        size='small'
                                        disabled={isBusy}
                                        loading={isBusy}
                                        onClick={() => onSelect(candidate)}
                                    >
                                        Выбрать
                                    </Button>
                                </Flex>
                                {animal?.identifiers && animal.identifiers.length > 0 && (
                                    <Space wrap>
                                        {animal.identifiers.map((identifier) => (
                                            <Tag key={`${identifier.name}-${identifier.value}`}>
                                                {identifier.name}: {identifier.value}
                                            </Tag>
                                        ))}
                                    </Space>
                                )}
                            </Flex>
                        </Card>
                    );
                })}
            </div>
        </div>
    );
};

const WritePreview = ({
    preview,
    isBusy,
    onSelectCandidate,
}: {
    preview: AiWritePreview;
    isBusy: boolean;
    onSelectCandidate: (item: AiWriteItemPreview, candidate: AiDisambiguationCandidate) => void;
}) => {
    return (
        <div className={styles.previewList}>
            {preview.items.map((item) => (
                <div className={styles.previewItem} key={item.idempotencyKey}>
                    <Typography.Text strong>
                        {item.tag ? `Бирка ${item.tag}` : `Запись ${item.index + 1}`}
                    </Typography.Text>
                    <div>
                        <Typography.Paragraph className={styles.message}>
                            {item.message}
                        </Typography.Paragraph>
                    </div>
                    {item.status === 'ambiguous' && item.candidates.length > 0 && (
                        <div className={styles.candidateGrid}>
                            {item.candidates.map((candidate) => {
                                const animal = candidate.animal;
                                const details = [
                                    animal?.type,
                                    animal?.birthDate ? `рожд. ${animal.birthDate}` : null,
                                    animal?.groupName ? `группа ${animal.groupName}` : null,
                                ].filter(Boolean).join(', ');

                                return (
                                    <button
                                        className={styles.writeCandidate}
                                        key={candidate.id}
                                        type='button'
                                        disabled={isBusy}
                                        onClick={() => onSelectCandidate(item, candidate)}
                                    >
                                        <Typography.Text strong>
                                            {getAnimalTag(candidate) ? `Бирка ${getAnimalTag(candidate)}` : getCandidateLabel(candidate)}
                                        </Typography.Text>
                                        {details && <Typography.Text type='secondary'>{details}</Typography.Text>}
                                        <Typography.Text className={styles.writeCandidateAction}>Выбрать</Typography.Text>
                                    </button>
                                );
                            })}
                        </div>
                    )}
                </div>
            ))}
        </div>
    );
};

const CommitReport = ({ report }: { report: AiWriteCommitReport }) => (
    <div className={styles.previewList}>
        <Space wrap>
            <Tag color='green'>Сохранено: {report.committed}</Tag>
            <Tag color='red'>Ошибок: {report.failed}</Tag>
            <Tag>Пропущено: {report.skipped}</Tag>
        </Space>

        {report.items.map((item) => (
            <div className={styles.previewItem} key={item.idempotencyKey}>
                <Typography.Text strong>{item.tag ?? `#${item.index + 1}`}</Typography.Text>
                <div>
                    <Tag>{itemStatusLabels[item.status] ?? item.status}</Tag>
                    <Typography.Paragraph className={styles.message}>
                        {item.message}
                    </Typography.Paragraph>
                </div>
            </div>
        ))}
    </div>
);
