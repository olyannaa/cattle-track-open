import { createSlice } from '@reduxjs/toolkit';
import { RootState } from '../../../app-service/store';
import { apiModeration } from './apiModeration';
import { generalApi } from '../../../app-service/services/general';

export type UserModeration = {
    login: string,
    roleId: string,
    id: string,
    name: string
}

export type UserSelf = {
    id: string,
    name: string,
    login: string,
    orgId: string,
    orgName: string,
    roleId: string,
    roleName: string,
    permissions: string[]
}

type InitialState = {
    users: UserModeration[],
    userSelf: UserSelf
};

const initialState: InitialState = {
    users: [],
    userSelf: {
        id: '',
        name: '',
        login: '',
        orgId: '',
        orgName: '',
        roleId: '',
        roleName: '',
        permissions: []
    }
};

const slice = createSlice({
    name: 'moderation',
    initialState: initialState,
    reducers: {
        
    },
    extraReducers: (builder) => {
        builder.addMatcher(
            generalApi.endpoints.getUserInfo.matchFulfilled,
            (state, action) => {
                state.userSelf = {...action.payload};
            }
        );
        builder.addMatcher(apiModeration.endpoints.getUsersModeration.matchFulfilled, (state, action) => {
            state.users = [...action.payload];
        });
    },
});

export default slice.reducer;
export const selectUserInfo = (state: RootState) => state.moderation.userSelf;
export const selectUsersModeration = (state: RootState) => state.moderation.users;
