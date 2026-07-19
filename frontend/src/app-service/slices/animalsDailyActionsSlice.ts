import { createSlice, PayloadAction } from '@reduxjs/toolkit';
import { dailyActionsApi } from '../services/dailyActions';
import { FiltersAnimalsType } from '../../utils/filtersAnimals';
import { SortersAnimalsType } from '../../utils/sortersAnimals';
import { AnimalFilters } from '../../utils/animals';
import { RootState } from '../store';

type InitialState = {
    filterAnimals: AnimalFilters[];
    selectedAnimals: string[];
    filtersAnimals: FiltersAnimalsType;
    sortersAnimals: SortersAnimalsType;
    allAnimalsIds: string[];
    isGroup: boolean;
    keyTab: string;
};

const initialState: InitialState = {
    filterAnimals: [],
    selectedAnimals: [],
    filtersAnimals: {
        groupId: '',
        type: '',
        isActive: true,
        tagNumber: '',
        identificationFieldId: '',
        identificationFieldValue: '',
    },
    sortersAnimals: {
        column: '',
        descending: false,
        page: 0,
    },
    allAnimalsIds: [],
    isGroup: false,
    keyTab: '1',
};

const slice = createSlice({
    name: 'animalsDailyActions',
    initialState: initialState,
    reducers: {
        addSelectedAnimal: (state, action) => {
            state.selectedAnimals = [action.payload];
        },
        addSelectedAnimals: (state, action) => {
            const isAnimal = !!state.selectedAnimals.find(
                (animal) => animal === action.payload
            );
            if (!isAnimal) {
                state.selectedAnimals.push(action.payload);
            }
        },
        setSelectedAnimal: (state, action: PayloadAction<string>) => {
            state.selectedAnimals = [action.payload];
        },
        addAllAnimals: (state, action) => {
            state.selectedAnimals = [...action.payload];
        },
        deleteAllAnimals: (state) => {
            state.selectedAnimals = [];
        },
        deleteSelectedAnimals: (state, action) => {
            state.selectedAnimals = state.selectedAnimals.filter(
                (animal) => animal !== action.payload
            );
        },
        changeFiltersAnimals: (state, action) => {
            state.filtersAnimals = {
                ...state.filtersAnimals,
                [action.payload.name]: action.payload.value,
            };
        },
        resetFiltersAnimals: (state) => {
            state.filtersAnimals = {
                ...initialState.filtersAnimals,
                type: state.keyTab === '8' ? 'Телка' : '',
            };
        },
        changeSortersAnimals: (state, action) => {
            state.sortersAnimals = {
                ...action.payload,
            };
        },
        resetSortersAnimals: (state) => {
            state.sortersAnimals = {
                ...initialState.sortersAnimals,
            };
        },
        changeIsGroup: (state, action) => {
            state.isGroup = action.payload;
        },
        changeKeyTab: (state, action) => {
            state.keyTab = action.payload;
        },
        reset: () => initialState,
    },
    extraReducers: (builder) => {
        builder.addMatcher(
            dailyActionsApi.endpoints.getFilterAnimals.matchFulfilled,
            (state, action) => {
                state.filterAnimals = [...action.payload];
                if (action.payload.length === 1 && !state.isGroup) {
                    state.selectedAnimals = [action.payload[0].id];
                }
            }
        );
        builder.addMatcher(
            dailyActionsApi.endpoints.createDailyActions.matchFulfilled,
            (state) => {
                state.filtersAnimals = {
                    ...initialState.filtersAnimals,
                    type: state.keyTab === '8' ? 'Телка' : '',
                };
            }
        );
        builder.addMatcher(
            dailyActionsApi.endpoints.createDailyActionsWithMedicine.matchFulfilled,
            (state) => {
                state.filtersAnimals = {
                    ...initialState.filtersAnimals,
                    type: state.keyTab === '8' ? 'Телка' : '',
                };
            }
        );
        builder.addMatcher(
            dailyActionsApi.endpoints.getAllAnimalsId.matchFulfilled,
            (state, action) => {
                state.allAnimalsIds = [...action.payload];
            }
        );
    },
});

export default slice.reducer;

export const selectAnimals = (state: RootState) =>
    state.animalsDailyActions.filterAnimals;

export const selectSelectedAnimals = (state: RootState) =>
    state.animalsDailyActions.selectedAnimals;

export const selectFiltersAnimals = (state: RootState) =>
    state.animalsDailyActions.filtersAnimals;

export const selectSortersAnimals = (state: RootState) =>
    state.animalsDailyActions.sortersAnimals;

export const selectAnimalsId = (state: RootState) =>
    state.animalsDailyActions.allAnimalsIds;

export const selectIsGroup = (state: RootState) => state.animalsDailyActions.isGroup;

export const selectKeyTab = (state: RootState) => state.animalsDailyActions.keyTab;

export const {
    addSelectedAnimal,
    addSelectedAnimals,
    deleteSelectedAnimals,
    resetFiltersAnimals,
    changeFiltersAnimals,
    addAllAnimals,
    deleteAllAnimals,
    changeSortersAnimals,
    resetSortersAnimals,
    changeIsGroup,
    changeKeyTab,
    setSelectedAnimal,
    reset,
} = slice.actions;
