import { useMemo, useState } from 'react';
import {
    IPointOneChart,
    useGetDailyWeightGainDataChartQuery,
} from '../../services/recordsApi';
import { getRangeMonthToToday, Range } from '../range-date-selector/rangeDateUtils';
import { Flex } from 'antd';
import styles from '../../RecordsPage.module.css';
import { LoadingOutlined } from '@ant-design/icons';
import { Line } from '@ant-design/plots';
import { RangeDateSelector } from '../range-date-selector/RangeDateSelector';
import dayjs from 'dayjs';

export const DailyWeightGainChart = () => {
    const [range, setRange] = useState<Range>(getRangeMonthToToday());

    const { data, isLoading, isFetching } = useGetDailyWeightGainDataChartQuery(
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

    const config = {
        data: {
            value: hasData ? data : [{ date: 0, weight: 0, type: '', __empty: true }],
            transform: [
                {
                    type: 'fold',
                    fields: ['Суточный привес'],
                    key: 'type',
                    value: 'value',
                },
            ],
        },

        xField: 'date',
        yField: 'weight',
        height: 300,
        colorField: 'type',
        point: {
            sizeField: 5,
            style: {
                fill: '#ff4218',
                stroke: '#fff',
                lineWidth: 2,
                shadowColor: 'rgba(255,66,24,1)',
                shadowBlur: 4,
                opacity: hasData ? 1 : 0,
            },
        },

        style: {
            lineWidth: hasData ? 2 : 0,
            stroke: 'rgba(255,66,24,1)',
        },

        axis: {
            y: {
                title: 'Привес (кг/сутки)',
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

        tooltip: hasData && {
            items: [
                (d: IPointOneChart) => ({
                    color: '#ff4218',
                    name: 'Cуточный привес:',
                    value: `${d.weight} кг/сутки`,
                }),
            ],
        },

        scale: {
            color: {
                domain: ['Суточный привес'],
                range: ['#ff4218'],
            },
        },

        legend: {
            color: {
                itemMarker: 'circle',
                position: 'bottom',
                itemLabelFontSize: 16,
                itemMarkerFill: '#ff4218',
                layout: {
                    justifyContent: 'center',
                },
            },
        },
    };

    return (
        <Flex vertical gap={16} className={styles['layout-chart']}>
            <div className={styles['layout-chart__title']}>Суточные привесы</div>
            <div className={styles['layout-chart__description']}>
                Изменение среднесуточных привесов животных за период.
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
                    <Line {...config} />
                </Flex>
            )}
        </Flex>
    );
};
