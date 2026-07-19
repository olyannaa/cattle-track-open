export interface IDailyActionTable {
    key: string;
    id: string;
    animalId: string;
    actionType: string;
    actionSubtype: string;
    actor: string;
    result: string;
    medicine: string | null;
    dose: string | null;
    notes: string | null;
    oldGroupId: string | null;
    oldGroupName: string | null;
    newGroupId: string | null;
    newGroupName: string | null;
    date: string;
    nextDate: string;
    createdAt: string;
    organizationId: string;
    name: string;
    materialType: string;
    collectionDate: string;
    collectedBy: string;
    oldType: string;
    newType: string;
}
