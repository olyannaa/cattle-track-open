import { api } from "../../../app-service/services/api";

export const inviteApi = api.injectEndpoints({
    endpoints: (builder) => ({
        checkInvite: builder.mutation<unknown,string>({
            query: (token)=> ({
                url: `organizations/invite/${token}`,
                method: 'POST',
            })
        }),
    }),
});

export const {
   useCheckInviteMutation
} = inviteApi;
