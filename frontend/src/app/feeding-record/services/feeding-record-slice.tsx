import { createSlice, PayloadAction } from '@reduxjs/toolkit';
import { RootState } from '../../../app-service/store';

export interface GroupStat {
    id: number;
    name: string;
    animalCount: number;
    rationId: number | null;
    accountingType: 'auto' | 'manual';
}

interface GroupState {
    groups: GroupStat[];
}

const initialState: GroupState = {
    groups: [],
};

const groupSlice = createSlice({
    name: 'group',
    initialState,
    reducers: {
        setGroups: (state, action: PayloadAction<GroupStat[]>) => {
            state.groups = action.payload;
        },
        updateGroupRation: (
            state,
            action: PayloadAction<{
                groupId: number;
                rationId: number;
                accountingType: 'auto' | 'manual';
            }>
        ) => {
            const { groupId, rationId, accountingType } = action.payload;
            const group = state.groups.find((g) => g.id === groupId);
            if (group) {
                group.rationId = rationId;
                group.accountingType = accountingType;
            }
        },
    },
});

export const { setGroups, updateGroupRation } = groupSlice.actions;
export const groupSelector = (state: RootState) => state.group.groups;
export default groupSlice.reducer;
