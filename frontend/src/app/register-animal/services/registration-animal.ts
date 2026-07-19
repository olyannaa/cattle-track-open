import { api } from '../../../app-service/services/api';
import { AnimalFilters } from '../../../utils/animals';

export type IdentificationOrg = {
    organization_id: string;
};

export type SelectResponseType = {
    id: string;
    name: string;
};

export const registrationAnimalApi = api.injectEndpoints({
    endpoints: (builder) => ({
        getAnimalIdentifications: builder.query<SelectResponseType[], void>({
            query: () => ({
                url: `animals/identifications`,
                method: 'GET',
            }),
        }),
        getAnimalGroups: builder.query<SelectResponseType[], void>({
            query: () => ({
                url: `animals/groups`,
                method: 'GET',
            }),
        }),
        getBreed: builder.query<SelectResponseType[], void>({
            query: () => ({
                url: `animals/breed`,
                method: 'GET',
            }),
        }),
        registrationAnimal: builder.mutation<void, FormData>({
            query: (body) => ({
                url: 'animals/registration',
                method: 'POST',
                body: body,
            }),
        }),
        registrationAnimalFromCSV: builder.mutation<void, FormData>({
            query: (data) => ({
                url: `animals/registration/import/csv`,
                method: 'POST',
                body: data,
            }),
        }),
        getParentsAnimals: builder.query<AnimalFilters[], string>({
            query: (type) => ({
                url: `DailyActions/animals?Filter.Type=${type}&Filter.Active=true`,
                method: 'GET',
            }),
        }),
    }),
});

export const {
    useGetAnimalIdentificationsQuery,
    useGetAnimalGroupsQuery,
    useRegistrationAnimalMutation,
    useRegistrationAnimalFromCSVMutation,
    useGetBreedQuery,
    useGetParentsAnimalsQuery,
} = registrationAnimalApi;
export const {
    endpoints: { getAnimalGroups, getAnimalIdentifications, registrationAnimal, registrationAnimalFromCSV },
} = registrationAnimalApi;
