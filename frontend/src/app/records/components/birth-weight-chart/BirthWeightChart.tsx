import { useMemo, useState } from 'react';
import { useGetBirthWeightQuery } from '../../services/recordsApi';
import { RangeDateSelector } from '../range-date-selector/RangeDateSelector';
import { getRangeMonthToToday, Range } from '../range-date-selector/rangeDateUtils';
import { Flex } from 'antd';
import styles from '../../RecordsPage.module.css';
import { LoadingOutlined } from '@ant-design/icons';
import { Box } from '@ant-design/plots';

export const BirthWeightChart = () => {
    const [range, setRange] = useState<Range>(getRangeMonthToToday());

    const { data, isLoading, isFetching } = useGetBirthWeightQuery(
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
            value: hasData
                ? data?.map((item) => ({
                      x: item.kind,
                      y: [0, 0, 0, item.avg, item.max],
                  }))
                : [{ x: 0, y: [0, 0, 0, 0, 0], __empty: true }],
        },
        xField: 'x',
        yField: 'y',
        style: {
            stroke: 'rgb(255, 66, 24)',
            fill: 'rgb(255, 66, 24)',
            opacity: hasData ? 1 : 0,
        },

        axis: {
            y: {
                title: 'Вес (кг)',
                line: true,
                lineLineWidth: 2,
                titleFill: 'rgba(0, 0, 0, 0.6)',
                titleFontSize: 16,
                grid: true,
            },
            x: {
                line: true,
                lineLineWidth: 2,
                label: hasData,
                tick: hasData,
                title: 'Средний вес',
                titleFill: 'rgba(0, 0, 0, 0.6)',
                titleFontSize: 16,
            },
        },
        height: 300,
        tooltip: {
            items: [
                (d: { x: string; y: string[] }) => {
                    console.log(d);
                    return {
                        color: '#ff4218',
                        name: 'Средний вес:',
                        value: `${d.y[3]} кг`,
                    };
                },
                (d: { x: string; y: string[] }) => {
                    return {
                        color: '#ff4218',
                        name: 'Максимальный вес:',
                        value: `${d.y[4]} кг`,
                    };
                },
            ],
        },

        scale: {
            x: { paddingInner: 0.5, paddingOuter: 0.5 },
            y: { zero: true },
        },
    };

    return (
        <Flex vertical gap={16} className={styles['layout-chart']}>
            <div className={styles['layout-chart__title']}>Вес при рождении</div>
            <div className={styles['layout-chart__description']}>
                Средний вес телят при рождении с разбивкой по полу.
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
                    <Box {...config} />
                </Flex>
            )}
        </Flex>
    );
};
