import { createSlice } from '@reduxjs/toolkit';
import { FiltersAnimalsType } from '../../../utils/filtersAnimals';
import { RootState } from '../../../app-service/store';
import { AnimalFilters } from '../../../utils/animals';
import { StatisticsAnimal, weightControlApi } from './weightControl';
import { SortersAnimalsType } from '../../../utils/sortersAnimals';

type InitialState = {
    filters: FiltersAnimalsType;
    selectedAnimal: string;
    animals: AnimalFilters[];
    sortersWeight: SortersAnimalsType;
    statisticsAnimal: StatisticsAnimal;
};

const initialState: InitialState = {
    filters: {
        groupId: '',
        type: '',
        isActive: true,
        tagNumber: '',
        identificationFieldId: '',
        identificationFieldValue: '',
    },
    selectedAnimal: '',
    animals: [],
    sortersWeight: {
        column: '',
        descending: false,
        page: 1,
    },
    statisticsAnimal: {
        dataByAge: [],
        dataByDate: [],
        dataBySUP: [],
        maxSUP: 0,
        meanSUP: 0,
        minSUP: 0,
    },
};

const slice = createSlice({
    name: 'weightControl',
    initialState: initialState,
    reducers: {
        changeFiltersWeightControl: (state, action) => {
            state.filters = {
                ...state.filters,
                [action.payload.name]: action.payload.value,
            };
        },
        resetFiltersWeightControl: (state) => {
            state.filters = { ...initialState.filters };
        },
        changeSelectedAnimal: (state, action) => {
            state.selectedAnimal = action.payload;
        },
        changeSortersWeightControls: (state, action) => {
            state.sortersWeight = {
                ...action.payload,
            };
        },
        resetSortersWeightControls: (state) => {
            state.sortersWeight = {
                ...initialState.sortersWeight,
            };
        },
    },
    extraReducers: (builder) => {
        builder.addMatcher(
            weightControlApi.endpoints.getFilterAnimals.matchFulfilled,
            (state, action) => {
                state.animals = [...action.payload];
                if (action.payload.length === 1) {
                    state.selectedAnimal = action.payload[0].id;
                }
            }
        );
        builder.addMatcher(
            weightControlApi.endpoints.createWeight.matchFulfilled,
            (state) => {
                state.filters = { ...initialState.filters };
            }
        );
        builder.addMatcher(
            weightControlApi.endpoints.getWeightsStatisticsAnimal.matchFulfilled,
            (state, action) => {
                if (action.payload.dataByDate) {
                    state.statisticsAnimal = { ...action.payload };
                } else {
                    state.statisticsAnimal = { ...initialState.statisticsAnimal };
                }
            }
        );
    },
});

export default slice.reducer;

export const {
    changeFiltersWeightControl,
    changeSelectedAnimal,
    resetFiltersWeightControl,
    changeSortersWeightControls,
    resetSortersWeightControls,
} = slice.actions;

export const selectFiltersWeightControl = (state: RootState) =>
    state.weightControl.filters;

export const selectSelectedAnimalWeightControl = (state: RootState) =>
    state.weightControl.selectedAnimal;

export const selectAnimalsWeightControl = (state: RootState) =>
    state.weightControl.animals;

export const selectSortersWeightControl = (state: RootState) =>
    state.weightControl.sortersWeight;

export const selectStatisticsAnimal = (state: RootState) =>
    state.weightControl.statisticsAnimal;
