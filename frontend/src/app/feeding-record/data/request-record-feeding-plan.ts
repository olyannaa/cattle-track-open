export interface RequestRecordFeedingPlan {
    eventDate: string;
    groupId: string;
    animalCount: number;
    groupRationId: string;
    totalKg: number;
    totalKgForGroup: number;
    feedingTime: string;
    feedingCoefficient: number;
    factKg: number | null;
    mark: number | null;
    feedingMark: number | null;
}
