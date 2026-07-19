import { IdentificationField } from '../types/animal';

export interface IAnimalTableBasic {
    key: string;
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
}

export interface IAnimalTable extends IAnimalTableBasic {
    identificationFields: IdentificationField[];
}
