import { api } from './api';
import {
    getUrlFilterAnimals,
    getUrlIdentificationValues,
    getUrlPaginationInfoFilterAnimals,
} from '../../functions/getUrl';
import { AnimalFilters } from '../../utils/animals';
import { FiltersAnimalsType } from '../../utils/filtersAnimals';
import { IRequestGetIdentificationValues } from '../../utils/requestType';
import { SortersAnimalsType } from '../../utils/sortersAnimals';

export type IRequestGetDailyActions = {
    type: string;
    page: number;
    column: string;
    descending: boolean;
};

export type newDailyAction = {
    animalId: string;
    type: string;
    subtype?: string;
    performedBy?: string;
    result?: string;
    medicine?: string;
    dose?: string;
    notes?: string;
    oldGroupId?: string;
    newGroupId?: string;
    date?: string;
    nextDate?: string | null;
    researchName?: string;
    materialType?: string;
    identificationValue?: string;
    oldType?: string;
    newType?: string;
    drugEliminationPeriod?: string;
};

export type IRequestCreateActionsWithMedicine = {
    animalIds: string[];
    actions: Omit<newDailyAction, 'animalId'>[];
};

export interface IDailyAction {
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
    drugEliminator: string;
}

export type IResponsePaginationInfoDailyActions = {
    count: number;
    entriesPerPage: number;
};

export type IRequestGetFilterAnimals = {
    filters: FiltersAnimalsType;
    sorters?: SortersAnimalsType;
};

export type IAnimal = {
    birthDate: string;
    breed: string;
    fatherTagNumber: string;
    groupName: string;
    id: string;
    motherTagNumber: string;
    origin: string;
    originLocation: string;
    status: string;
    tagNumber: string;
    identificationFields: IdentificationField[];
    type: string;
    [key: string]: string | IdentificationField[];
};

export type IdentificationField = {
    name: string;
    value: string | null;
};

export const dailyActionsApi = api.injectEndpoints({
    endpoints: (builder) => ({
        getPaginationInfoDailyActions: builder.query<
            IResponsePaginationInfoDailyActions,
            string
        >({
            query: (type) => ({
                url: `DailyActions/pagination-info?type=${type}`,
                method: 'GET',
            }),
        }),
        getDailyActions: builder.query<IDailyAction[], IRequestGetDailyActions>({
            query: (data) => ({
                url: `DailyActions?type=${data.type}&page=${data.page}&SortInfo.Column=${
                    data.column ||
                    (data.type === 'Исследования' ? 'CollectionDate' : 'Date')
                }&SortInfo.Descending=${data.descending}`,
                method: 'GET',
            }),
        }),
        getFilterAnimals: builder.query<AnimalFilters[], IRequestGetFilterAnimals>({
            query: (data) => ({
                url: getUrlFilterAnimals(data.filters, data.sorters),
                method: 'GET',
            }),
        }),
        getPaginationInfoFilterAnimals: builder.query<
            IResponsePaginationInfoDailyActions,
            FiltersAnimalsType
        >({
            query: (data) => ({
                url: getUrlPaginationInfoFilterAnimals(data),
                method: 'GET',
            }),
        }),
        createDailyActions: builder.mutation<void, newDailyAction[]>({
            query: (data) => ({
                url: 'DailyActions',
                method: 'POST',
                body: data,
            }),
        }),
        createDailyActionsWithoutResetFilters: builder.mutation<void, newDailyAction[]>({
            query: (data) => ({
                url: 'DailyActions',
                method: 'POST',
                body: data,
            }),
        }),
        createDailyActionsWithMedicine: builder.mutation<
            void,
            IRequestCreateActionsWithMedicine
        >({
            query: (data) => ({
                url: 'DailyActions/with-medicine/batch',
                method: 'POST',
                body: data,
            }),
        }),
        createDailyActionsWithoutResetFiltersWithMedicine: builder.mutation<
            void,
            IRequestCreateActionsWithMedicine
        >({
            query: (data) => ({
                url: 'DailyActions/with-medicine/batch',
                method: 'POST',
                body: data,
            }),
        }),
        deleteDailyActions: builder.mutation<void, string[]>({
            query: (data) => ({
                url: 'DailyActions',
                method: 'DELETE',
                body: data,
            }),
        }),
        deleteDailyActionsResearch: builder.mutation<void, string[]>({
            query: (data) => ({
                url: 'DailyActions/researches',
                method: 'DELETE',
                body: data,
            }),
        }),
        getIdentificationValues: builder.query<string[], IRequestGetIdentificationValues>(
            {
                query: (data) => ({
                    url: getUrlIdentificationValues(data),
                    method: 'GET',
                }),
            }
        ),
        getAnimalById: builder.query<IAnimal, string>({
            query: (data) => ({
                url: `animals/${data}`,
                method: 'GET',
            }),
        }),
        getAllAnimalsId: builder.query<string[], IRequestGetFilterAnimals>({
            query: (data) => ({
                url: getUrlFilterAnimals(data.filters, data.sorters, true),
                method: 'GET',
            }),
        }),
        getAllActionsId: builder.query<string[], IRequestGetDailyActions>({
            query: (data) => ({
                url: `DailyActions/ids?type=${data.type}&SortInfo.Column=${
                    data.column ||
                    (data.type === 'Исследования' ? 'CollectionDate' : 'Date')
                }&SortInfo.Descending=${data.descending}`,
                method: 'GET',
            }),
        }),
    }),
});

export const {
    useLazyGetPaginationInfoDailyActionsQuery,
    useLazyGetDailyActionsQuery,
    useLazyGetFilterAnimalsQuery,
    useGetFilterAnimalsQuery,
    useCreateDailyActionsMutation,
    useDeleteDailyActionsMutation,
    useLazyGetIdentificationValuesQuery,
    useLazyGetPaginationInfoFilterAnimalsQuery,
    useDeleteDailyActionsResearchMutation,
    useLazyGetAnimalByIdQuery,
    useCreateDailyActionsWithoutResetFiltersMutation,
    useLazyGetAllAnimalsIdQuery,
    useLazyGetAllActionsIdQuery,
    useCreateDailyActionsWithMedicineMutation,
    useCreateDailyActionsWithoutResetFiltersWithMedicineMutation,
} = dailyActionsApi;
