import { createSlice } from '@reduxjs/toolkit';
import { RootState } from '../store';
import {
    dailyActionsApi,
    IDailyAction,
    IResponsePaginationInfoDailyActions,
} from '../services/dailyActions';
import { SortersAnimalsType } from '../../utils/sortersAnimals';

type InitialState = {
    selectedDailyActions: string[];
    reset: boolean;
    dailyActions: IDailyAction[];
    paginationInfoDailyActions: IResponsePaginationInfoDailyActions;
    sorters: SortersAnimalsType;
    allActionsId: string[];
};

const initialState: InitialState = {
    selectedDailyActions: [],
    reset: false,
    dailyActions: [],
    paginationInfoDailyActions: {
        count: 0,
        entriesPerPage: 0,
    },
    sorters: {
        column: '',
        descending: true,
        page: 1,
    },
    allActionsId: [],
};

const slice = createSlice({
    name: 'dailyActions',
    initialState: initialState,
    reducers: {
        addAction: (state, action) => {
            const isDailyAction = !!state.selectedDailyActions.find(
                (dailyAction) => dailyAction === action.payload
            );
            if (!isDailyAction) {
                state.selectedDailyActions.push(action.payload);
            }
        },
        deleteAction: (state, action) => {
            state.selectedDailyActions = state.selectedDailyActions.filter(
                (dailyAction) => dailyAction !== action.payload
            );
        },

        addAllActions: (state, action) => {
            state.selectedDailyActions = [...action.payload];
        },

        deleteAllActions: (state) => {
            state.selectedDailyActions = [];
        },

        changeSortersDailyActions: (state, action) => {
            state.sorters = {
                ...action.payload,
            };
        },
    },
    extraReducers: (builder) => {
        builder.addMatcher(
            dailyActionsApi.endpoints.deleteDailyActions.matchFulfilled,
            (state) => {
                state.selectedDailyActions = [];
            }
        );
        builder.addMatcher(
            dailyActionsApi.endpoints.getDailyActions.matchFulfilled,
            (state, action) => {
                state.dailyActions = [...action.payload];
            }
        );
        builder.addMatcher(
            dailyActionsApi.endpoints.createDailyActions.matchFulfilled,
            (state) => {
                state.reset = !state.reset;
            }
        );
        builder.addMatcher(
            dailyActionsApi.endpoints.createDailyActionsWithMedicine.matchFulfilled,
            (state) => {
                state.reset = !state.reset;
            }
        );
        builder.addMatcher(
            dailyActionsApi.endpoints.getPaginationInfoDailyActions.matchFulfilled,
            (state, action) => {
                state.paginationInfoDailyActions = { ...action.payload };
            }
        );
        builder.addMatcher(
            dailyActionsApi.endpoints.getAllActionsId.matchFulfilled,
            (state, action) => {
                state.allActionsId = [...action.payload];
            }
        );
    },
});

export default slice.reducer;

export const selectSelectedDailyActions = (state: RootState) =>
    state.dailyActions.selectedDailyActions;

export const selectDailyActions = (state: RootState) => state.dailyActions.dailyActions;

export const selectReset = (state: RootState) => state.dailyActions.reset;

export const selectPaginationInfoDailyActions = (state: RootState) =>
    state.dailyActions.paginationInfoDailyActions;

export const selectSortersDailyActions = (state: RootState) => state.dailyActions.sorters;

export const selectAllActionsId = (state: RootState) => state.dailyActions.allActionsId;

export const {
    addAction,
    deleteAction,
    addAllActions,
    deleteAllActions,
    changeSortersDailyActions,
} = slice.actions;
