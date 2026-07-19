import { api } from '../../../app-service/services/api';

export type IdentificationOrg = {
    organization_id: string;
};

export type NewInfrastructure = {
    Name: string;
};

export interface IGroup {
    id?: string;
    name: string;
    typeId: string;
    location: string;
    description: string;
}

export type InfrastructureDataItem = {
    id: string;
    name: string;
    typeId: string;
    typeName: string;
    description: string;
    location: string;
};

export const infrastructureApi = api.injectEndpoints({
    endpoints: (builder) => ({
        getGroupsTypes: builder.query<InfrastructureDataItem[], void>({
            query: () => ({
                url: `groups/type`,
            }),
            providesTags: [{ type: 'TypesGroups', id: 'LIST1' }],
        }),
        addNewGroupType: builder.mutation<void, NewInfrastructure>({
            query: (body) => ({
                url: 'groups/type',
                method: 'POST',
                body: body,
            }),
            invalidatesTags: ['TypesGroups'],
        }),
        deleteGroupType: builder.mutation<void, string>({
            query: (id) => ({
                url: `groups/type?typeId=${id}`,
                method: 'DELETE',
            }),
            invalidatesTags: ['TypesGroups'],
        }),
        getGroups: builder.query<InfrastructureDataItem[], void>({
            query: () => ({
                url: `/groups`,
            }),
            providesTags: [{ type: 'Groups', id: 'LIST' }],
        }),
        addGroup: builder.mutation<void, IGroup>({
            query: (body) => ({
                url: `/groups`,
                method: 'POST',
                body: body,
            }),
            invalidatesTags: ['Groups'],
        }),
        deleteGroup: builder.mutation<void, string>({
            query: (id) => ({
                url: `/groups?groupId=${id}`,
                method: 'DELETE',
            }),
            invalidatesTags: ['Groups'],
        }),
        editGroup: builder.mutation<void, IGroup>({
            query: (body) => ({
                url: `/groups`,
                method: 'PUT',
                body: body,
            }),
            invalidatesTags: ['Groups'],
        }),
        getIdentificationFields: builder.query<InfrastructureDataItem[], void>({
            query: () => ({
                url: `/groups/identification`,
            }),
            providesTags: [{ type: 'IdentificationFields', id: 'LIST2' }],
        }),
        addIdentificationField: builder.mutation<void, NewInfrastructure>({
            query: (body) => ({
                url: `/groups/identification`,
                method: 'POST',
                body: body,
            }),
            invalidatesTags: ['IdentificationFields'],
        }),
        deleteIdentificationField: builder.mutation<void, string>({
            query: (id) => ({
                url: `/groups/identification?identificationId=${id}`,
                method: 'DELETE',
            }),
            invalidatesTags: ['IdentificationFields'],
        }),
    }),
});

export const {
    useGetGroupsTypesQuery,
    useAddNewGroupTypeMutation,
    useDeleteGroupTypeMutation,
    useGetGroupsQuery,
    useAddGroupMutation,
    useEditGroupMutation,
    useDeleteGroupMutation,
    useGetIdentificationFieldsQuery,
    useAddIdentificationFieldMutation,
    useDeleteIdentificationFieldMutation,
} = infrastructureApi;
export const {
    endpoints: {
        getGroups,
        getGroupsTypes,
        getIdentificationFields,
        addGroup,
        addIdentificationField,
        addNewGroupType,
        editGroup,
        deleteGroup,
        deleteGroupType,
        deleteIdentificationField,
    },
} = infrastructureApi;
