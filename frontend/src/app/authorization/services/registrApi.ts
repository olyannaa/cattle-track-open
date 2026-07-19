import { api } from "../../../app-service/services/api";
import { IUser } from "../../../utils/userType";

type IRequestRegistrUser={
    name: string,
    phoneNumber: string,
    login: string,
    password: string,
    isOrgAdmin: boolean
}

type IResponseGetConfrimNumber = {
    tgId:string,
    phoneNumber:string
}

type IRequestLoginWithTelegram = {
    authDate: string,
    firstName: string,
    hash: string,
    id: string,
    lastName: string,
    photoUrl: string,
    userName: string
}

export const registrApi = api.injectEndpoints({
    endpoints: (builder) => ({
        getConfirmNumber: builder.query<IResponseGetConfrimNumber, string>({
            query: (token)=> ({
                url:`auth/bot/${token}`,
                method: 'GET'
            })
        }),
        registrUser: builder.mutation<IUser,IRequestRegistrUser>({
            query: (data)=> ({
                url: 'users',
                method: 'POST',
                body: data
            })
        }),
        checkLogin: builder.query<void, string>({
            query: (login)=>({
                url: `auth/login?login=${login}`,
                method: 'GET'
            })
        }),
        loginWithTelegram: builder.mutation<IUser, IRequestLoginWithTelegram>({
            query: (data)=> ({
                url: 'auth/login/telegram',
                method: 'POST',
                body: data
            })
        })
    }),
});

export const {
    useLazyGetConfirmNumberQuery,
    useRegistrUserMutation,
    useLazyCheckLoginQuery,
    useLoginWithTelegramMutation
} = registrApi;
