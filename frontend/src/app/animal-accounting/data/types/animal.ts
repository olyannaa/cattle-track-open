export type IRequestGetAnimals = {
    page: number;
    type: string;
    active: boolean;
    column: string | null;
    descending: boolean;
    search: string;
};

export type IRequestGetPaginationInfo = {
    type: string;
    active: boolean;
    search: string;
};

export type IAnimal = {
    birthDate: string;
    breed: string;
    fatherTagNumbers: string;
    groupName: string;
    id: string;
    motherTagNumber: string;
    origin: string;
    originLocation: string;
    status: string;
    tagNumber: string;
    lastVaccinationDate: string;
    identificationFields: IdentificationField[];
    [key: string]: string | IdentificationField[];
};

export type IdentificationField = {
    name: string;
    value: string | null;
};

export type IResponseGetAnimals = IAnimal[];

export type IResponsePaginationInfo = {
    count: number;
    entriesPerPage: number;
};

export type IChangedAnimal = {
    id: string;
    tagNumber: string | null;
    birthDate: string | null;
    breed: string | null;
    groupID: string | null;
    status: string | null;
    origin: string | null;
    originLocation: string | null;
    motherTagNumber: string | null;
    fatherTagNumbers: string | null;
    identificationFields: IdentificationField[];
    [key: string]: string | null | IdentificationField[];
};

export type IdentificationFieldName = {
    id: string;
    name: string;
};

export type dataIndexTypes =
    | 'tagNumber'
    | 'groupName'
    | 'birthDate'
    | 'status'
    | 'breed'
    | 'origin'
    | 'originLocation'
    | 'motherTagNumber'
    | 'type'
    | 'fatherTagNumbers'
    | 'lastVaccinationDate';

export type dataIndexTypesChanged =
    | 'tagNumber'
    | 'birthDate'
    | 'status'
    | 'breed'
    | 'origin'
    | 'originLocation'
    | 'motherTagNumber'
    | 'fatherTagNumbers'
    | 'groupID'
    | 'type'
    | 'lastVaccinationDate';
