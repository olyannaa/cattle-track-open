import { useState } from 'react';
import {
    IPointTwoChart,
    useGetVaccinationsDataChartQuery,
} from '../../services/recordsApi';
import { getRangeMonthToToday, Range } from '../range-date-selector/rangeDateUtils';
import { Flex } from 'antd';
import styles from '../../RecordsPage.module.css';
import { LoadingOutlined } from '@ant-design/icons';
import { Column, ColumnConfig } from '@ant-design/plots';
import { RangeDateSelector } from '../range-date-selector/RangeDateSelector';
import dayjs from 'dayjs';

export const VaccinationsChart = () => {
    const [range, setRange] = useState<Range>(getRangeMonthToToday());

    const { data, isLoading, isFetching } = useGetVaccinationsDataChartQuery(
        {
            startDate: range[0]?.format('YYYY-MM-DD') || '',
            endDate: range[1]?.format('YYYY-MM-DD') || '',
        },
        {
            skip: !range[0] || !range[1],
            refetchOnMountOrArgChange: 20,
        },
    );

    const hasData = data?.length ? true : false;

    const config: ColumnConfig = {
        data: {
            value: hasData ? data : [{ date: 0, kind: '0', value: 0, __empty: true }],
        },
        xField: 'date',
        yField: 'value',
        colorField: 'kind',

        tooltip: hasData && {
            items: [
                (d: IPointTwoChart) => ({
                    name: d.kind,
                    value: d.value,
                }),
            ],
        },

        axis: {
            y: {
                title: 'Количество',
                line: true,
                lineLineWidth: 2,
                titleFill: 'rgba(0, 0, 0, 0.6)',
                titleFontSize: 16,
                grid: hasData,
            },
            x: {
                line: true,
                lineLineWidth: 2,
                labelFormatter: (d: string) => {
                    const val = dayjs(d);
                    return val.format('D MMM');
                },
                label: hasData,
                tick: hasData,
                titleFill: 'rgba(0, 0, 0, 0.6)',
                titleFontSize: 16,
                grid: hasData,
            },
        },

        // Легенда под графиком
        legend: hasData && {
            // цветовая легенда (по seriesField)
            color: {
                itemMarker: 'circle',
                position: 'bottom',
                layout: {
                    justifyContent: 'center',
                },
            },
        },
        group: {
            type: 'dodgeX',
        },

        height: 300,
    };

    return (
        <Flex vertical gap={16} className={styles['layout-chart']}>
            <div className={styles['layout-chart__title']}>Вакцинации</div>
            <div className={styles['layout-chart__description']}>
                Выполнение вакцинаций за выбранный период.
            </div>
            {isLoading ? (
                <Flex
                    className={styles['layout-chart__loading']}
                    justify='center'
                    align='center'
                >
                    <LoadingOutlined style={{ fontSize: '40px' }} />
                </Flex>
            ) : (
                <Flex
                    vertical
                    gap={16}
                    className={isFetching ? styles['is-fetching'] : ''}
                >
                    <RangeDateSelector range={range} onRangeChange={setRange} />
                    <Column {...config} />
                </Flex>
            )}
        </Flex>
    );
};
