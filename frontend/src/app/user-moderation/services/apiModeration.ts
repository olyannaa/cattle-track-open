import { api } from "../../../app-service/services/api";
import { UserModeration } from "./moderationSlice";

type IRequestCreateLinkInvite = {
  roleId: string,
  orgId: string,
  usageLimit: number,
  expireTime: string
}

type IResponseCreateLinkInvite = {
    link: string;
}

type IRequestChangeUserRole = {
    roleId: string;
    userId: string;
}

type IRequestResetUserPassword = {
    userId: string;
    password: string;
}

export const apiModeration = api.injectEndpoints({
    endpoints: (builder) => ({
        getUsersModeration: builder.query<UserModeration[], void>({
            query: () => ({
                url: 'organizations/users',
                method: 'GET',
            }),
        }),
        createLinkInvite: builder.mutation<IResponseCreateLinkInvite, IRequestCreateLinkInvite>({
            query: (data) => ({
                url: 'organizations/invite',
                method: 'POST',
                body: data
            })
        }),
        changeUserRole: builder.mutation<unknown, IRequestChangeUserRole>({
            query: ({userId, roleId}) => ({
                url: `organizations/users/${userId}/role`,
                method: 'PATCH',
                body: { roleId }
            })
        }),
        resetUserPassword: builder.mutation<unknown, IRequestResetUserPassword>({
            query: ({userId, password}) => ({
                url: `organizations/users/${userId}/password`,
                method: 'PATCH',
                body: { password }
            })
        }),
        deleteUser: builder.mutation<unknown,string>({
            query: (userId)=> ({
                url: `organizations/users/${userId}`,
                method: 'DELETE',
            })
        }),
    }),
});

export const {
   useGetUsersModerationQuery,
   useChangeUserRoleMutation,
   useResetUserPasswordMutation,
   useCreateLinkInviteMutation,
   useDeleteUserMutation,
   useLazyGetUsersModerationQuery
} = apiModeration;
