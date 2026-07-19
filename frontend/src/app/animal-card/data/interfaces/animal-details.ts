export interface AnimalDetail {
    id: string;
    organizationId: string | null;
    tagNumber: string;
    type: string;
    breed: string;
    motherId: string | null;
    motherTagNumber: string | null;
    fatherIds: string[] | null;
    fatherTagNumbers: string[] | null;
    status: string;
    groupId: string | null;
    groupName: string | null;
    origin: string;
    originLocation: string;
    birthDate: string;
    dateOfReceipt: string;
    dateOfDisposal: string | null;
    reasonOfDisposal: string | null;
    identificationData: Record<string, unknown>;
    identificationDataJson: string;
    name?: string;
}
