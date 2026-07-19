import { api } from '../../../app-service/services/api';
import { InfrastructureDataItem } from '../../infrastructure/services/infrastructure-service';
import { ComponentDietItem, ComponentDietType } from '../data/component';
import { NewDiet } from '../data/new-diet';
import { IRation } from '../data/ration';
import { IGroupRationInfo } from '../data/group-ratio-info';
import { RequestAssignRation } from '../data/request-assign-ration';
import { ComponentRow } from '../data/component-row';
import { CommonConsumptionGraph } from '../data/common-consumption';
import {
    CostPoints,
    FeedingChart,
    FeedingPoint,
    NutrientsPoint,
} from '../data/chart-data';
import { PlanItem } from '../data/feeding-plan-row';
import { RequestRecordFeedingPlan } from '../data/request-record-feeding-plan';

export const feedingRecord = api.injectEndpoints({
    endpoints: (builder) => ({
        getComponents: builder.query<ComponentDietItem[], void>({
            query: () => ({
                url: `feeding/component`,
                method: 'GET',
            }),
        }),
        deleteComponent: builder.mutation<void, string>({
            query: (componentId: string) => ({
                url: `feeding/component`,
                method: 'DELETE',
                params: { componentId },
            }),
        }),
        createComponent: builder.mutation<void, ComponentDietType>({
            query: (component: ComponentDietType) => ({
                url: `feeding/component`,
                method: 'POST',
                body: component,
            }),
        }),
        editComponent: builder.mutation<void, ComponentDietItem>({
            query: (component: ComponentDietItem) => ({
                url: `feeding/component`,
                method: 'PATCH',
                body: component,
            }),
        }),
        createDiet: builder.mutation<void, NewDiet>({
            query: (component: NewDiet) => ({
                url: `feeding/ration`,
                method: 'POST',
                body: component,
            }),
            invalidatesTags: ['Rations'],
        }),
        getGroups: builder.query<InfrastructureDataItem[], void>({
            query: () => ({
                url: `/groups`,
            }),
        }),
        getGroupStat: builder.query<IGroupRationInfo[], void>({
            query: () => ({
                url: `/feeding/group-stats`,
            }),
            providesTags: [{ type: 'GroupsStat', id: 'LIST1' }],
        }),
        getRationsWithComponents: builder.query<IRation[], void>({
            query: () => ({
                url: `/feeding/rations-with-components`,
            }),
            providesTags: [{ type: 'Rations', id: 'LIST2' }],
        }),
        assignRation: builder.mutation<void, RequestAssignRation>({
            query: (body) => ({
                url: `/feeding/assign-ration-to-group`,
                method: 'POST',
                body: body,
            }),
            invalidatesTags: ['GroupsStat'],
        }),
        changeRationComponents: builder.mutation<
            void,
            { rationId: string; components: ComponentRow[] }
        >({
            query: (data) => ({
                url: `/feeding/ration/${data.rationId}`,
                method: 'PUT',
                body: { components: data.components },
            }),
            invalidatesTags: ['Rations'],
        }),
        getRationPlan: builder.query<PlanItem[], string>({
            query: (date: string) => ({
                url: `feeding/main/plan-to-date`,
                method: 'GET',
                params: { date: date },
            }),
        }),
        saveRecordFeeding: builder.mutation<void, RequestRecordFeedingPlan[]>({
            query: (data) => ({
                url: `/feeding/record-feeding`,
                method: 'POST',
                body: data,
            }),
        }),
        /** Графики */
        getCommonStatMonth: builder.query<CommonConsumptionGraph[], void>({
            query: () => ({
                url: `feeding/analysis/graph`,
                method: 'GET',
            }),
        }),
        getFeedingMonthChart: builder.query<FeedingChart<FeedingPoint>, string>({
            query: (id: string) => ({
                url: `feeding/group-analysis/graph`,
                method: 'GET',
                params: { groupId: id },
            }),
        }),
        getRationCostMonth: builder.query<CostPoints, string>({
            query: (id: string) => ({
                url: `feeding/group-ration-cost/graph`,
                method: 'GET',
                params: { groupId: id },
            }),
        }),
        getRationCostYear: builder.query<CostPoints, string>({
            query: (id: string) => ({
                url: `feeding/group-ration-cost-yearly/graph`,
                method: 'GET',
                params: { groupId: id },
            }),
        }),
        getRationNutrientsDynamic: builder.query<FeedingChart<NutrientsPoint>, string>({
            query: (id: string) => ({
                url: `feeding/group-ration-nutrition/graph`,
                method: 'GET',
                params: { groupId: id },
            }),
        }),
        /** Скачивание данных по графикам */
        exportRationNutrientsDynamic: builder.query<FeedingChart<NutrientsPoint>, string>(
            {
                query: (id: string) => ({
                    url: `feeding/group-ration-nutrition/export`,
                    method: 'GET',
                    params: { groupId: id },
                }),
            }
        ),
    }),
});

export const {
    useCreateComponentMutation,
    useDeleteComponentMutation,
    useEditComponentMutation,
    useCreateDietMutation,
    useGetComponentsQuery,
    useGetGroupsQuery,
    useGetGroupStatQuery,
    useGetRationsWithComponentsQuery,
    useAssignRationMutation,
    useChangeRationComponentsMutation,
    useLazyGetRationPlanQuery,
    useSaveRecordFeedingMutation,
    /** Графики */
    useGetCommonStatMonthQuery,
    useLazyGetFeedingMonthChartQuery,
    useGetRationCostMonthQuery,
    useGetRationCostYearQuery,
    useLazyGetRationNutrientsDynamicQuery,
    /** Скачивание данных по графикам */
    useLazyExportRationNutrientsDynamicQuery,
} = feedingRecord;

export const {
    endpoints: { assignRation, getRationCostMonth },
} = feedingRecord;
