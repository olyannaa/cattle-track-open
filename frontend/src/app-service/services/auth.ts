/* eslint-disable @typescript-eslint/no-explicit-any */
import { IUser } from '../../utils/userType';
import { api } from './api';

export type LoginData = {
    login: string;
    password: string;
};

export const authApi = api.injectEndpoints({
    endpoints: (builder) => ({
        login: builder.mutation<IUser, LoginData>({
            query: (userData) => ({
                url: '/auth/login',
                method: 'POST',
                body: userData,
            }),
        }),
        logout: builder.mutation<any, void>({
            query: () => ({
                url: '/auth/logout',
                method: 'POST',
            }),
        }),
        loginUpdate: builder.mutation<any, void>({
            query: () => ({
                url: 'auth/login/update',
                method: 'POST'
            })
        })
    }),
});

export const { useLoginMutation, useLogoutMutation, useLoginUpdateMutation } = authApi;
