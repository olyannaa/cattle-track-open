import { useMemo, useState } from 'react';
import {
    IPointBloodTestsChart,
    useGetBloodTestsDataChartQuery,
} from '../../services/recordsApi';
import { RangeDateSelector } from '../range-date-selector/RangeDateSelector';
import { getRangeMonthToToday, Range } from '../range-date-selector/rangeDateUtils';
import { Flex } from 'antd';
import styles from '../../RecordsPage.module.css';
import { LoadingOutlined } from '@ant-design/icons';
import { Column } from '@ant-design/plots';

export const BloodTestsChart = () => {
    const [range, setRange] = useState<Range>(getRangeMonthToToday());

    const { data, isLoading, isFetching } = useGetBloodTestsDataChartQuery(
        {
            startDate: range[0]?.format('YYYY-MM-DD') || '',
            endDate: range[1]?.format('YYYY-MM-DD') || '',
        },
        {
            skip: !range[0] || !range[1],
            refetchOnMountOrArgChange: 20,
        }
    );

    const hasData = useMemo(() => {
        return data?.length ? true : false;
    }, [data]);

    const config = {
        data: {
            value: hasData ? data : [{ date: 0, kind: '0', value: 0, __empty: true }],
        },
        xField: 'diagnosis',
        yField: 'value',
        colorField: 'kind',
        stack: true,
        procent: true,
        axis: {
            y: {
                title: 'Процент (%)',
                line: true,
                lineLineWidth: 2,
                titleFill: 'rgba(0, 0, 0, 0.6)',
                titleFontSize: 16,
                grid: hasData,
            },
            x: {
                line: true,
                lineLineWidth: 2,
                label: hasData,
                tick: hasData,
                titleFill: 'rgba(0, 0, 0, 0.6)',
                titleFontSize: 16,
                grid: hasData,
            },
        },

        scale: {
            color: {
                domain: ['Положительные', 'Отрицательные'],
                range: [
                    'rgb(76, 175, 80)', // Живой
                    'rgb(231, 76, 60)', // Мертворожденный
                ],
            },

            x: { paddingInner: 0.5, paddingOuter: 0.5 },
        },
        legend: hasData && {
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
                (d: IPointBloodTestsChart) => ({
                    color:
                        d.kind === 'Положительные'
                            ? 'rgb(76, 175, 80)'
                            : 'rgb(231, 76, 60)',
                    name: `${d.kind}:`,
                    value: `${d.value}%`,
                }),
            ],
        },
    };

    return (
        <Flex vertical gap={16} className={styles['layout-chart']}>
            <div className={styles['layout-chart__title']}>
                Взятие крови (диагностика)
            </div>
            <div className={styles['layout-chart__description']}>
                Результаты лабораторных исследований крови по диагнозам.
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
