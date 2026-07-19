export type EventType = 'Осмотры' | 'Вакцинации и обработки' | 'Выбытие' | 'Поступление' | 'Лечение';

export interface HistoryPoint {
    x: string;
    y: string;
}

export interface AnimalHistoryData {
    points: HistoryPoint[];
    title: string;
    xAxisLabel: string;
    yAxisLabel: string;
}
