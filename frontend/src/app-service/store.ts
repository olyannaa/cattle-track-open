import { Action, configureStore, ThunkAction } from '@reduxjs/toolkit';
import { api } from '../app-service/services/api';
import animalsDailyActions from './slices/animalsDailyActionsSlice';
import dailyActions from './slices/dailyActionsSlice';
import animals from '../app/animal-accounting/services/animalsSlice';
import weightControl from '../app/weight-control/service/weightControlSlice';
import group from '../app/feeding-record/services/feeding-record-slice';
import moderation from '../app/user-moderation/services/moderationSlice';

export const store = configureStore({
    reducer: {
        [api.reducerPath]: api.reducer,
        animalsDailyActions,
        dailyActions,
        animals,
        weightControl,
        group,
        moderation,
    },
    middleware: (getDefaultMiddleware) => getDefaultMiddleware().concat(api.middleware),
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
export type AppThunk<ReturnType = void> = ThunkAction<
    ReturnType,
    RootState,
    unknown,
    Action<string>
>;
