import { useMemo, useState } from 'react';
import { IPointTwoChart, useGetCalvingsDataChartQuery } from '../../services/recordsApi';
import { RangeDateSelector } from '../range-date-selector/RangeDateSelector';
import { getRangeMonthToToday, Range } from '../range-date-selector/rangeDateUtils';
import { LoadingOutlined } from '@ant-design/icons';
import { Flex } from 'antd';
import styles from '../../RecordsPage.module.css';
import { Column, ColumnConfig } from '@ant-design/plots';
import dayjs from 'dayjs';

export const CalvingChart = () => {
    const [range, setRange] = useState<Range>(getRangeMonthToToday());

    const { data, isLoading, isFetching } = useGetCalvingsDataChartQuery(
        {
            startDate: range[0]?.format('YYYY-MM-DD') || '',
            endDate: range[1]?.format('YYYY-MM-DD') || '',
        },
        {
            skip: !range[0] || !range[1],
            refetchOnMountOrArgChange: 20,
        },
    );

    const hasData = useMemo(() => {
        return data?.length ? true : false;
    }, [data]);

    const config: ColumnConfig = {
        data: {
            value: hasData
                ? data
                : [
                      { date: 0, kind: 'Живой', value: 0, __empty: true },
                      { date: 0, kind: 'Аборт', value: 0, __empty: true },
                      { date: 0, kind: 'Мертворожденный', value: 0, __empty: true },
                  ],
        },
        xField: 'date',
        yField: 'value',
        colorField: 'kind',
        stack: true,
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

        scale: {
            color: {
                domain: ['Живой', 'Аборт', 'Мертворожденный'],
                range: [
                    'rgb(76, 175, 80)', // Живой
                    'rgb(255, 140, 66)', // Аборт
                    'rgb(231, 76, 60)', // Мертворожденный
                ],
            },

            x: { paddingInner: 0.5, paddingOuter: 0.5 },
        },
        // Легенда под графиком
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
        height: 300,
        tooltip: hasData && {
            items: [
                (d: IPointTwoChart) => ({
                    color:
                        d.kind === 'Живой'
                            ? 'rgb(76, 175, 80)'
                            : d.kind === 'Аборт'
                              ? 'rgb(255, 140, 66)'
                              : 'rgb(231, 76, 60)',
                    name: `${d.kind}:`,
                    value: `${d.value}`,
                }),
            ],
        },
    };

    return (
        <Flex vertical gap={16} className={styles['layout-chart']}>
            <div className={styles['layout-chart__title']}>Отёлы</div>
            <div className={styles['layout-chart__description']}>
                Динамика отёлов за выбранный период с распределением по результатам.
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
