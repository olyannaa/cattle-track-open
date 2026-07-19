export type IRequestGetIdentificationValues = {
    groupId?: string;
    type?: string;
    isActive?: boolean;
    identificationId: string;
    [key: string]: string | undefined | boolean;
};
