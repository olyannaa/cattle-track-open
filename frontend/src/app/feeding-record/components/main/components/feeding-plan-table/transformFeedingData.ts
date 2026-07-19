import { FeedingTime } from '../../../../data/feeding-plan-row';

export interface RawGroup {
    groupId: string;
    groupName: string;
    groupRationName: string;
    groupRationId: string;
    animalCount: number;
    morningFeeding: number;
    dayFeeding: number;
    nightFeeding: number;
    rationCostPerHead: number;
    totalRationCost: number;
    totalKgForGroup: number;
    totalKg: number;
    factKg: number | null;
    mark: number | null;
    feedingCoefficient: number | null;
    feedingMark: number | null;
    realKg: number;
    realKgForGroup: number;
}

export interface TableRow extends RawGroup {
    key: string;
    time: FeedingTime;
    coefficient: number;
}

const feedingTimeMap: Record<FeedingTime, keyof RawGroup> = {
    Утро: 'morningFeeding',
    День: 'dayFeeding',
    Вечер: 'nightFeeding',
};

export enum correspondenceTimeOfDay {
    'Утро' = 'morning',
    'День' = 'day',
    'Вечер' = 'night',
}

export const transformRationPlan = (data: RawGroup[]): TableRow[] => {
    const result: TableRow[] = [];

    data.forEach((group) => {
        (Object.keys(feedingTimeMap) as FeedingTime[]).forEach((time) => {
            const feedingValue = group[feedingTimeMap[time]];
            if (feedingValue === null || feedingValue === undefined || typeof feedingValue !== 'number') return;

            result.push({
                ...group,
                key: `${time}-${group.groupId}`,
                time,
                coefficient: feedingValue,
                totalKg: group.totalKg * feedingValue,
                totalKgForGroup: group.totalKgForGroup * feedingValue,
            });
        });
    });

    const timeOrder: Record<FeedingTime, number> = { Утро: 1, День: 2, Вечер: 3 };
    return result.sort((a, b) => timeOrder[a.time] - timeOrder[b.time]);
};
