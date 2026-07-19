export interface IBaseInfo {
    id: string;
    orgId: string | null;
    tagNumber?: string | null;
    type?: string | null;
    breed?: string | null;
    motherId?: string | null;
    motherTagNumber?: string | null;
    fatherTagNumber?: string | null;
    status?: string | null;
    groupId?: string | null;
    groupName?: string | null;
    origin?: string | null;
    originLocation?: string | null;
    birthDate?: string | null;
    dateOfReceipt?: string | null;
    dateOfDisposal?: string | null;
    reasonOfDisposal?: string | null;
    identificationData?: Record<string, string | null>;
    fatherIds?: string[] | null;
    [key: string]: unknown;
}
