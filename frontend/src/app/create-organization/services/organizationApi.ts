import { api } from "../../../app-service/services/api";

type IRequestRegistrOrganization = {
    name: string,
    inn?: string,
    ogrn?: string
}

export const organizationApi = api.injectEndpoints({
    endpoints: (builder) => ({
        registrOrganization: builder.mutation<unknown,IRequestRegistrOrganization>({
            query: (data)=> ({
                url: 'organizations',
                method: 'POST',
                body: data
            })
        })
    }),
});

export const {
    useRegistrOrganizationMutation
} = organizationApi;
