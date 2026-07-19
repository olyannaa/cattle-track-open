export interface CommonConsumptionGraph {
    rationName: string;
    groupId: string;
    groupName: string;
    dailyFactKg: number;
    events: ConsumptionPoint[];
}

export interface ConsumptionPoint {
    eventDate: string;
    totalFactKg: number;
}
