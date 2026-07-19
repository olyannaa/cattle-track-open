import { api } from './api';

export type IdentificationFieldName = {
    id: string;
    name: string;
};

export type IAnimalGroup = {
    id: string;
    name: string;
};

export type IUser = {
    id: string;
    name: string;
};

type IResponseGetUserInfo = {
  id: string,
  name: string,
  login: string,
  orgId: string,
  orgName: string,
  roleId: string,
  roleName: string,
  permissions: string[]
}

export const generalApi = api.injectEndpoints({
    endpoints: (builder) => ({
        getIdentificationsFields: builder.query<IdentificationFieldName[], void>({
            query: () => ({
                url: 'groups/identification',
                method: 'GET',
            }),
        }),
        getGroup: builder.query<IAnimalGroup[], void>({
            query: () => ({
                url: 'groups',
                method: 'GET',
            }),
        }),
        getUsers: builder.query<IUser[], void>({
            query: () => ({
                url: 'users',
                method: 'GET',
            }),
        }),
        getUserInfo: builder.query<IResponseGetUserInfo, void>({
            query: ()=> ({
                url: 'users/self',
                method: 'GET'
            })
        })
    }),
});

export const { useGetIdentificationsFieldsQuery, useGetGroupQuery, useGetUsersQuery, useGetUserInfoQuery, useLazyGetUserInfoQuery } =
    generalApi;
