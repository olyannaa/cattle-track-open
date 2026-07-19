export interface AnimalAction {
    actionId: string;
    animalId: string;
    eventType: string;
    fields: {
        [key: string]: string | null;
    };
    eventDate: string;
    performedBy: string | null;
    bullId?: string;
    calfId?: string;
}
