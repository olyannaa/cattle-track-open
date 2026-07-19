import { api } from '../../../app-service/services/api';

export type RequestInsemination = {
    cowIds: string[];
    date: string;
    inseminationType: string;
    spermBatch?: string;
    spermManufacturer?: string;
    bullIds?: string[];
    embryoId?: string;
    embryoManufacturer?: string;
    technician?: string;
    notes?: string;
    bullName?: string;
};

export type RequestInseminationBatch = {
    items: RequestInsemination[];
};

export type RequestPregnancy = {
    cowId: string;
    date: string;
    status: string;
    inseminationId: string;
    expectedCalvingDate?: string;
};

export type RequestCalving = {
    cowId: string;
    bullIds?: string[];
    cowTagNumber?: string;
    date: string;
    veterinar?: string;
    treatments?: string;
    /** Тип отёла */
    complication: string;
    type: string;
    pathology?: string;
    calfTagNumber?: string;
    method?: string;
    weight?: number;
    inseminationId: string;
};

export type Animal = {
    id: string;
    animalId: string;
    organizationId: string;
    tagNumber: string;
    type: string;
    birthDate: Date;
    status: string;
    name: string;
    pregnancyId: string;
};

export type FullPregnancyInfo = {
    id: string;
    organizationId: string;
    cowId: string;
    cowTagNumber?: string;
    status: string;
    inseminationType: string;
    inseminationDate: Date;
    bullIds: string[];
    bullTagNumber?: string;
    name: string;
    pregnancyId: string;
    inseminationId: string;
};

export const reproductiveApi = api.injectEndpoints({
    endpoints: (builder) => ({
        getCows: builder.query<Animal[], void>({
            query: () => ({
                url: `reproductive/cow`,
                method: 'GET',
            }),
        }),
        getBulls: builder.query<Animal[], void>({
            query: () => ({
                url: `reproductive/bull`,
                method: 'GET',
            }),
        }),
        getInseminationAnimals: builder.query<Animal[], void>({
            query: () => ({
                url: `reproductive/insemination/animals`,
                method: 'GET',
            }),
        }),
        registrationInsemination: builder.mutation<void, RequestInsemination>({
            query: (body) => ({
                url: 'reproductive/insemination',
                method: 'POST',
                body: body,
            }),
        }),
        registrationInseminationBatch: builder.mutation<void, RequestInseminationBatch>({
            query: (body) => ({
                url: 'reproductive/inseminations/batch',
                method: 'POST',
                body: body,
            }),
        }),
        getPregnancies: builder.query<FullPregnancyInfo[], void>({
            query: () => ({
                url: `reproductive/pregnancy`,
                method: 'GET',
            }),
        }),
        registerPregnancy: builder.mutation<void, RequestPregnancy>({
            query: (body) => ({
                url: 'reproductive/pregnancy',
                method: 'POST',
                body: body,
            }),
        }),
        getCalving: builder.query<FullPregnancyInfo[], void>({
            query: () => ({
                url: `reproductive/calving`,
                method: 'GET',
            }),
        }),
        registerCalving: builder.mutation<void, RequestCalving>({
            query: (body) => ({
                url: 'reproductive/calving',
                method: 'POST',
                body: body,
            }),
        }),
    }),
});

export const {
    useGetBullsQuery,
    useGetCowsQuery,
    useGetCalvingQuery,
    useRegistrationInseminationMutation,
    useGetPregnanciesQuery,
    useRegisterCalvingMutation,
    useRegisterPregnancyMutation,
    useGetInseminationAnimalsQuery,
    useRegistrationInseminationBatchMutation,
} = reproductiveApi;
