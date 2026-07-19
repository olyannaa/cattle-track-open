import { RawGroup } from '../components/main/components/feeding-plan-table/transformFeedingData';

export type FeedingTime = 'Утро' | 'День' | 'Вечер';

export interface FeedingPlanRow {
    time: FeedingTime;
    groupId: string;
    group: string;
    heads: number;
    diet: string | null;
    coefficient: number;
    weightPerHead: number;
    weightPerGroup: number;
    actualWeight?: number;
    score?: number;
    consumptionRate?: number;
}
export interface PlanItem extends RawGroup {
    groupId: string;
    groupName: string;
    animalCount: number;
    groupRationId: string;
    groupRationName: string;
    morningFeeding: number;
    dayFeeding: number;
    nightFeeding: number;
    totalKg: number;
    totalKgForGroup: number;
    feedingCoefficient: number | null;
    factKg: number | null;
    mark: number | null;
}
