export interface CostPoints {
    groupName: string;
    records: CostRecordType[];
}

export interface CostRecordType {
    eventDate: string;
    monthYear: string;
    rationCost: number;
    totalRationCost: number;
    rationName: string;
}

export interface FeedingChart<T = FeedingPoint> {
    records: T[];
}

export interface FeedingPoint {
    rationName: string;
    eventDate: string;
    dailyFactKg: number;
}

export interface NutrientsPoint {
    eventDate: string;
    totalSv: number;
    totalSp: number;
    totalCep: number;
    totalNdk: number;
    rationName: string;
}
