import { useState } from 'react';
import { IPointTwoChart, useGetPregnancyDataChartQuery } from '../../services/recordsApi';
import { getRangeMonthToToday, Range } from '../range-date-selector/rangeDateUtils';
import { Flex } from 'antd';
import styles from '../../RecordsPage.module.css';
import { LoadingOutlined } from '@ant-design/icons';
import { RangeDateSelector } from '../range-date-selector/RangeDateSelector';
import { Column } from '@ant-design/plots';
import dayjs from 'dayjs';

export const PregnancyChart = () => {
    const [range, setRange] = useState<Range>(getRangeMonthToToday());

    const { data, isLoading, isFetching } = useGetPregnancyDataChartQuery(
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

    const config = {
        data: {
            value: hasData
                ? data
                : [
                      { date: 0, kind: 'Стельная', value: 0, __empty: true },
                      { date: 0, kind: 'Яловая', value: 0, __empty: true },
                      { date: 0, kind: 'Подлежит проверке', value: 0, __empty: true },
                  ],
        },
        xField: 'date',
        yField: 'value',
        colorField: 'kind',
        stack: true,
        scale: {
            color: {
                domain: ['Стельная', 'Яловая', 'Подлежит проверке'],
                order: ['Стельная', 'Яловая', 'Подлежит проверке'],
                range: [
                    'rgb(76, 175, 80, 0.6)',
                    'rgb(232, 232, 232, 0.6)',
                    'rgb(255, 140, 66, 0.6)',
                ],
            },
            x: { paddingInner: 0.5, paddingOuter: 0.8 },
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
        height: 300,
        tooltip: hasData && {
            shared: true,
            items: [
                (d: IPointTwoChart) => ({
                    name: d.kind,
                    value: d.value,
                    marker: {
                        symbol: 'circle',
                        style: {
                            fill:
                                d.kind === 'Стельная'
                                    ? 'rgb(76, 175, 80)'
                                    : d.kind === 'Яловая'
                                      ? 'rgb(232, 232, 232)'
                                      : 'rgb(255, 140, 66)',
                        },
                    },
                }),
            ],
        },
        legend: {
            // цветовая легенда (по seriesField)
            color: {
                itemMarker: 'circle',
                position: 'bottom',
                layout: {
                    justifyContent: 'center',
                },
            },
        },
    };

    return (
        <Flex vertical gap={16} className={styles['layout-chart']}>
            <div className={styles['layout-chart__title']}>Стельность коров</div>
            <div className={styles['layout-chart__description']}>
                Распределение статусов стельности коров.
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
