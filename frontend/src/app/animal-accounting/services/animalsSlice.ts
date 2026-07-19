import { createSlice } from '@reduxjs/toolkit';
import { RootState } from '../../../app-service/store';
import { animalsApi } from './animals';
import { dataIndexTypesChanged, IChangedAnimal } from '../data/types/animal';

export interface IAnimalGroup {
    id: string;
    name: string;
}

type InitialState = {
    animalGroups: IAnimalGroup[];
    changedAnimals: IChangedAnimal[];
    selectedAnimalsDelete: string[];
};

const initialState: InitialState = {
    animalGroups: [],
    changedAnimals: [],
    selectedAnimalsDelete: [],
};

type ActionAnimals = {
    payload: {
        id: string;
        value: string | null;
        dataIndex: dataIndexTypesChanged;
    };
};

type ActionAnimalsMoreFields = {
    payload: {
        id: string;
        value: string | null;
        nameField: string;
    };
};

const slice = createSlice({
    name: 'animals',
    initialState: initialState,
    reducers: {
        updateChangedAnimals: (state, action: ActionAnimals) => {
            const index = state.changedAnimals.findIndex((animal) => animal.id === action.payload.id);
            if (index !== -1) {
                const changedAnimal = {
                    ...state.changedAnimals[index],
                    [action.payload.dataIndex]: action.payload.value,
                };
                if (
                    Object.keys(changedAnimal).every((key) => {
                        if (key === 'id') return true;
                        if (key === 'identificationFields') return true;
                        return changedAnimal[key] == null;
                    }) &&
                    !changedAnimal.identificationFields.length
                ) {
                    state.changedAnimals = state.changedAnimals.filter((animal) => animal.id !== action.payload.id);
                } else {
                    state.changedAnimals[index] = {
                        ...changedAnimal,
                    };
                }
            } else {
                if (action.payload.value === null) return;
                console.log(action.payload.value);
                state.changedAnimals = [
                    ...state.changedAnimals,
                    {
                        ...{
                            id: action.payload.id,
                            tagNumber: null,
                            birthDate: null,
                            breed: null,
                            groupID: null,
                            status: null,
                            origin: null,
                            originLocation: null,
                            motherTagNumber: null,
                            fatherTagNumbers: null,
                            identificationFields: [],
                        },
                        [action.payload.dataIndex]: action.payload.value,
                    },
                ];
            }
        },
        updateChangedAnimalsMoreFields: (state, action: ActionAnimalsMoreFields) => {
            const index = state.changedAnimals.findIndex((animal) => animal.id === action.payload.id);

            if (index !== -1) {
                const indexField = state.changedAnimals[index].identificationFields.findIndex(
                    (field) => field.name === action.payload.nameField
                );

                if (indexField !== -1) {
                    state.changedAnimals[index].identificationFields[indexField].value = action.payload.value;
                } else {
                    state.changedAnimals[index].identificationFields.push({
                        name: action.payload.nameField,
                        value: action.payload.value,
                    });
                }

                const changedAnimal = {
                    ...state.changedAnimals[index],
                };
                if (
                    Object.keys(changedAnimal).every((key) => {
                        if (key === 'id') return true;
                        if (key === 'identificationFields') return true;
                        return changedAnimal[key] == null;
                    }) &&
                    changedAnimal.identificationFields.every((field) => field.value === null)
                ) {
                    state.changedAnimals = state.changedAnimals.filter((animal) => animal.id !== action.payload.id);
                } else {
                    state.changedAnimals[index] = {
                        ...changedAnimal,
                    };
                }
            } else {
                if (action.payload.value === null) return;
                state.changedAnimals.push({
                    id: action.payload.id,
                    tagNumber: null,
                    birthDate: null,
                    breed: null,
                    groupID: null,
                    status: null,
                    origin: null,
                    originLocation: null,
                    motherTagNumber: null,
                    fatherTagNumbers: null,
                    identificationFields: [
                        {
                            name: action.payload.nameField,
                            value: action.payload.value,
                        },
                    ],
                });
            }
        },
        addDeleteAnimals: (state, action) => {
            const isAnimal = !!state.selectedAnimalsDelete.find((animal) => animal === action.payload);
            if (!isAnimal) {
                state.selectedAnimalsDelete.push(action.payload);
            }
        },
        addDeleteAllAnimals: (state, action) => {
            state.selectedAnimalsDelete = [...action.payload];
        },
        resetDeleteAllAnimals: (state) => {
            state.selectedAnimalsDelete = [];
        },
        deleteDeleteAnimals: (state, action) => {
            state.selectedAnimalsDelete = state.selectedAnimalsDelete.filter((animal) => animal !== action.payload);
        },
    },
    extraReducers: (builder) => {
        builder.addMatcher(animalsApi.endpoints.getAnimalsGroups.matchFulfilled, (state, action) => {
            state.animalGroups = [...action.payload];
        });
        builder.addMatcher(animalsApi.endpoints.getAnimals.matchFulfilled, (state) => {
            state.changedAnimals = [];
        });
    },
});

export default slice.reducer;
export const selectAnimalGroups = (state: RootState) => state.animals.animalGroups;
export const selectChangedAnimals = (state: RootState) => state.animals.changedAnimals;
export const selectDeleteAnimals = (state: RootState) => state.animals.selectedAnimalsDelete;

export const {
    updateChangedAnimals,
    updateChangedAnimalsMoreFields,
    addDeleteAllAnimals,
    addDeleteAnimals,
    resetDeleteAllAnimals,
    deleteDeleteAnimals,
} = slice.actions;
