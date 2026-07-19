export type FiltersAnimalsType = {
    groupId?: string;
    type?: string;
    tagNumber?: string;
    isActive?: boolean;
    identificationFieldId?: string;
    identificationFieldValue?: string;
    [key: string]: string | undefined | boolean;
};
