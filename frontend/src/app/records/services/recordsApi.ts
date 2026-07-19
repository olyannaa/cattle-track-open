import { api } from '../../../app-service/services/api';

export type IRequestDataChart = {
    startDate: string;
    endDate: string;
};

export type IPointTwoChart = {
    date: string;
    kind: string;
    value: number;
};

export type IPointOneChart = {
    date: string;
    weight: number;
};

export type IPointBirthWeightChart = {
    kind: string;
    avg: number;
    max: number;
};

export type IPointBloodTestsChart = {
    diagnosis: string;
    kind: string;
    value: number;
};

export const recordsApi = api.injectEndpoints({
    endpoints: (builder) => ({
        getCalvingsDataChart: builder.query<IPointTwoChart[], IRequestDataChart>({
            query: ({ startDate, endDate }) => ({
                url: `statistics/charts/calvings?dateFrom=${startDate}&dateTo=${endDate}`,
                method: 'GET',
            }),
        }),
        getDailyWeightGainDataChart: builder.query<IPointOneChart[], IRequestDataChart>({
            query: ({ startDate, endDate }) => ({
                url: `statistics/charts/daily-weight-gain?dateFrom=${startDate}&dateTo=${endDate}`,
                method: 'GET',
            }),
        }),
        getBirth12WeightDataChart: builder.query<IPointOneChart[], IRequestDataChart>({
            query: ({ startDate, endDate }) => ({
                url: `statistics/charts/weight-at-12-months?dateFrom=${startDate}&dateTo=${endDate}`,
                method: 'GET',
            }),
        }),
        getPregnancyDataChart: builder.query<IPointTwoChart[], IRequestDataChart>({
            query: ({ startDate, endDate }) => ({
                url: `statistics/charts/pregnancy?dateFrom=${startDate}&dateTo=${endDate}`,
                method: 'GET',
            }),
        }),
        getVaccinationsDataChart: builder.query<IPointTwoChart[], IRequestDataChart>({
            query: ({ startDate, endDate }) => ({
                url: `statistics/charts/vaccinations?dateFrom=${startDate}&dateTo=${endDate}`,
                method: 'GET',
            }),
        }),
        getBloodTestsDataChart: builder.query<IPointBloodTestsChart[], IRequestDataChart>(
            {
                query: ({ startDate, endDate }) => ({
                    url: `statistics/charts/blood-tests?dateFrom=${startDate}&dateTo=${endDate}`,
                    method: 'GET',
                }),
            },
        ),
        getBirthWeight: builder.query<IPointBirthWeightChart[], IRequestDataChart>({
            query: ({ startDate, endDate }) => ({
                url: `statistics/charts/birth-weight?dateFrom=${startDate}&dateTo=${endDate}`,
                method: 'GET',
            }),
        }),
    }),
});

export const {
    useGetCalvingsDataChartQuery,
    useGetDailyWeightGainDataChartQuery,
    useGetBirth12WeightDataChartQuery,
    useGetPregnancyDataChartQuery,
    useGetVaccinationsDataChartQuery,
    useGetBloodTestsDataChartQuery,
    useGetBirthWeightQuery,
} = recordsApi;
