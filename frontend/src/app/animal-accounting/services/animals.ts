import { api } from '../../../app-service/services/api';
import { getFilterParameters } from '../../../utils/create-parameters';
import { AnimalFilterParams } from '../data/interfaces/animal-filters-params';
import { IChangedAnimal, IdentificationFieldName, IResponseGetAnimals, IResponsePaginationInfo } from '../data/types/animal';
import { IAnimalGroup } from './animalsSlice';

export const animalsApi = api.injectEndpoints({
    endpoints: (builder) => ({
        getPaginationInfo: builder.query<IResponsePaginationInfo, AnimalFilterParams>({
            query: (params) => ({
                url: `animals/pagination-info`,
                params: getFilterParameters(params),
                method: 'GET',
            }),
        }),
        getAnimals: builder.query<IResponseGetAnimals, AnimalFilterParams>({
            query: (params) => {
                return {
                    url: '/animals',
                    params: getFilterParameters(params),
                };
            },
        }),
        updateAnimals: builder.mutation<void, IChangedAnimal[]>({
            query: (data) => ({
                url: `animals`,
                method: 'PUT',
                body: data,
            }),
        }),
        getAnimalsGroups: builder.query<IAnimalGroup[], void>({
            query: () => ({
                url: `animals/groups`,
                method: 'GET',
            }),
        }),
        getIdentificationFieldsNames: builder.query<IdentificationFieldName[], void>({
            query: () => ({
                url: 'groups/identification',
                method: 'GET',
            }),
        }),
        getAllAnimalIds: builder.query<string[], boolean>({
            query: (data) => ({
                url: `animals/barren/ids?IsActive=${data}`,
                method: 'GET',
            }),
        }),
        deleteAnimals: builder.mutation<void, string[]>({
            query: (data) => ({
                url: 'animals/barren',
                method: 'DELETE',
                body: data,
            }),
        }),
        getCommonStat: builder.query<Record<string, number>, void>({
            query: () => ({
                url: `animals/main-info`,
            }),
        }),
        getPlacesOrigin: builder.query<string[], void>({
            query: () => ({
                url: `animals/places-of-origin`,
            }),
        }),
        getOrigins: builder.query<string[], void>({
            query: () => ({
                url: `animals/origins`,
            }),
        }),
    }),
});

export const {
    useLazyGetAnimalsQuery,
    useUpdateAnimalsMutation,
    useGetAnimalsGroupsQuery,
    useLazyGetPaginationInfoQuery,
    useGetIdentificationFieldsNamesQuery,
    useLazyGetAllAnimalIdsQuery,
    useDeleteAnimalsMutation,
    useGetCommonStatQuery,
    useGetOriginsQuery,
    useGetPlacesOriginQuery,
} = animalsApi;
