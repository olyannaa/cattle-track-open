import { api } from '../../../app-service/services/api';

export interface IDrug {
    id: string;
    name: string;
    substance: string;
    drugEliminatior?: string;
    shelfLife?: string;
    factory?: string;
}

export interface IRequestCreateDrug {
    id: string;
    name: string;
    substance: string;
    drugEliminationPeriod?: string;
    shelfLife?: string;
    factory?: string;
}


export const drugsApi = api.injectEndpoints({
    endpoints: (builder) => ({
        getDrugs: builder.query<IDrug[], void>({
            query: () => ({
                url: 'DailyActions/medicine',
                method: 'GET',
            }),
            providesTags: [{ type: 'Drugs', id: 'LIST' }],
        }),
        deleteDrug: builder.mutation<void, string>({
            query: (id) => ({
                url: `DailyActions/medicine/${id}`,
                method: 'DELETE',
            }),
            invalidatesTags: [{ type: 'Drugs', id: 'LIST' }],
        }),
        createDrug: builder.mutation<void, Omit<IRequestCreateDrug, 'id'>>({
            query: (data) => ({
                url: 'DailyActions/medicine',
                method: 'POST',
                body: data,
            }),
            invalidatesTags: [{ type: 'Drugs', id: 'LIST' }],
        }),
        editDrug: builder.mutation<void, IRequestCreateDrug>({
            query: (data) => ({
                url: `DailyActions/medicine`,
                method: 'PUT',
                body: data,
            }),
            invalidatesTags: [{ type: 'Drugs', id: 'LIST' }],
        }),
    }),
});

export const {
    useGetDrugsQuery,
    useDeleteDrugMutation,
    useCreateDrugMutation,
    useEditDrugMutation,
} = drugsApi;
