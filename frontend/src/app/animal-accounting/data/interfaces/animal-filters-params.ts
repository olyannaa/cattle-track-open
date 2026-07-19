export interface AnimalFilterParams {
    page?: number;
    pageSize?: number;
    filters?: IFilters;
    sortInfo?: {
        active?: boolean;
        column: string | null;
        descending?: boolean;
    };
}

export interface IFilters {
    tagNumber?: string;
    types?: string[];
    birthDateFrom?: string;
    birthDateTo?: string;
    breeds?: string[];
    groupNames?: string[];
    statuses?: string[];
    origins?: string[];
    originLocations?: string[];
    motherTagNumber?: string;
    fatherTagNumber?: string;
}
