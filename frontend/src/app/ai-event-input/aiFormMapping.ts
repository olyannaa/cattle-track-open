import dayjs, { Dayjs } from 'dayjs';
import { AiWriteItemPreview, AiWritePreview } from '../../app-service/services/aiAssistant';

type PreviewRecord = Record<string, unknown>;

export type AiWeightFormValues = {
    animalId?: string;
    date?: Dayjs;
    weight?: number;
    method?: string;
};

export type AiTreatmentFormValues = {
    animalIds: string[];
    dateStartTreatment?: Dayjs;
    name?: string;
    subtype?: string;
    medicine?: string;
    diagnosis?: string;
    dose?: string;
    dateNextTreatment?: Dayjs;
};

export type AiInseminationFormValues = {
    cowIds: string[];
    date?: Dayjs;
    inseminationType?: string;
    spermBatch?: string;
    spermManufacturer?: string;
    bullIds?: string[];
    embryoId?: string;
    embryoManufacturer?: string;
    technician?: string;
    bullName?: string;
};

export type AiExistingFormValues = {
    toolName: AiWritePreview['toolName'];
    weight?: AiWeightFormValues;
    treatment?: AiTreatmentFormValues;
    insemination?: AiInseminationFormValues;
    warnings: string[];
};

const isRecord = (value: unknown): value is PreviewRecord =>
    typeof value === 'object' && value !== null;

const getString = (record: PreviewRecord, key: string): string | undefined => {
    const value = record[key];
    return typeof value === 'string' && value.trim() ? value : undefined;
};

const getNumber = (record: PreviewRecord, key: string): number | undefined => {
    const value = record[key];
    return typeof value === 'number' ? value : undefined;
};

const getStringArray = (record: PreviewRecord, key: string): string[] | undefined => {
    const value = record[key];
    return Array.isArray(value) ? value.filter((item): item is string => typeof item === 'string') : undefined;
};

const getDate = (record: PreviewRecord, key: string): Dayjs | undefined => {
    const value = getString(record, key);
    return value ? dayjs(value) : undefined;
};

const getResolvedItems = (preview: AiWritePreview): AiWriteItemPreview[] =>
    preview.items.filter((item) => item.status === 'resolved' && item.canCommit && isRecord(item.preview));

export const mapAiPreviewToExistingFormValues = (
    preview: AiWritePreview
): AiExistingFormValues => {
    const resolvedItems = getResolvedItems(preview);
    const warnings: string[] = [];

    if (preview.ambiguous || preview.notFound || preview.invalid) {
        warnings.push(
            `Часть batch не будет заполнена: неоднозначно ${preview.ambiguous}, не найдено ${preview.notFound}, ошибок ${preview.invalid}.`
        );
    }

    if (!resolvedItems.length) {
        warnings.push('Нет строк, которые можно перенести в форму.');
    }

    if (preview.toolName === 'create_weight') {
        const item = resolvedItems[0];
        const record = item?.preview;
        if (!isRecord(record)) {
            return { toolName: preview.toolName, warnings };
        }

        return {
            toolName: preview.toolName,
            weight: {
                animalId: getString(record, 'animalId'),
                date: getDate(record, 'date'),
                weight: getNumber(record, 'weight'),
                method: getString(record, 'method'),
            },
            warnings,
        };
    }

    if (preview.toolName === 'create_daily_action') {
        const treatmentItems = resolvedItems.filter((item) => {
            const record = item.preview;
            return isRecord(record) && getString(record, 'type') === 'Обработка';
        });
        const first = treatmentItems[0]?.preview;
        if (!isRecord(first)) {
            return { toolName: preview.toolName, warnings };
        }

        return {
            toolName: preview.toolName,
            treatment: {
                animalIds: treatmentItems
                    .map((item) => (isRecord(item.preview) ? getString(item.preview, 'animalId') : undefined))
                    .filter((id): id is string => Boolean(id)),
                dateStartTreatment: getDate(first, 'date'),
                name: getString(first, 'performedBy'),
                subtype: getString(first, 'subtype'),
                medicine: getString(first, 'medicine'),
                diagnosis: getString(first, 'result'),
                dose: getString(first, 'dose'),
                dateNextTreatment: getDate(first, 'nextDate'),
            },
            warnings,
        };
    }

    if (preview.toolName === 'create_insemination') {
        const first = resolvedItems[0]?.preview;
        if (!isRecord(first)) {
            return { toolName: preview.toolName, warnings };
        }

        const cowIds = resolvedItems.flatMap((item) => {
            const record = item.preview;
            return isRecord(record) ? getStringArray(record, 'cowIds') ?? [] : [];
        });

        return {
            toolName: preview.toolName,
            insemination: {
                cowIds,
                date: getDate(first, 'date'),
                inseminationType: getString(first, 'inseminationType'),
                spermBatch: getString(first, 'spermBatch'),
                spermManufacturer: getString(first, 'spermManufacturer'),
                bullIds: getStringArray(first, 'bullIds'),
                embryoId: getString(first, 'embryoId'),
                embryoManufacturer: getString(first, 'embryoManufacturer'),
                technician: getString(first, 'technician'),
                bullName: getString(first, 'bullName'),
            },
            warnings,
        };
    }

    return { toolName: preview.toolName, warnings };
};
