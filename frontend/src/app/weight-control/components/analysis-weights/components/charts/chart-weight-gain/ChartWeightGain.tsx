import { Line, LineConfig } from '@ant-design/charts';
import { useAppSelector } from '../../../../../../../app-service/hooks';
import { selectStatisticsAnimal } from '../../../../../service/weightControlSlice';
import { Flex } from 'antd';
import styles from './ChartWeightGain.module.css';
import { useEffect, useState } from 'react';
import { useWindowSize } from '../../../../../../../hooks/useWindowSize';
import { getCountItemsChart } from '../../../../../functions/getCountItemsChart';
import { chartStyles } from '../../../../../../../styles/chart-styles';
import { EmptyDataAlert } from '../../../../../../../global-components/expty-data-alert/EmptyDataAlert';

export const ChartWeightGain = () => {
    const statisticsAnimal = useAppSelector(selectStatisticsAnimal);
    const widthWindow = useWindowSize();
    const [countItem, setCountItem] = useState(getCountItemsChart('SUP', widthWindow));
    useEffect(() => {
        setCountItem(getCountItemsChart('SUP', widthWindow));
    }, [widthWindow]);

    const config: LineConfig = {
        xField: 'date',
        yField: 'sup',
        point: {
            sizeField: 5,
            style: {
                fill: '#fff',
                stroke: '#ff4218',
                lineWidth: 1,
            },
        },
        tooltip: {
            title: (d) => d.date,
            items: [
                (d) => ({
                    color: '#ff4218',
                    name: 'СУП',
                    value: d.sup,
                }),
            ],
        },
        scrollbar: {
            x:
                statisticsAnimal.dataBySUP.length < countItem
                    ? false
                    : {
                          ratio: countItem / statisticsAnimal.dataBySUP.length,
                      },
        },
        ...chartStyles,
    };
    return !statisticsAnimal.dataBySUP.length ? (
        <EmptyDataAlert />
    ) : (
        <Flex wrap={'wrap-reverse'} gap={24} align='flex-end'>
            <div
                style={{
                    height: '300px',
                    background: '#fff',
                    borderRadius: 8,
                    boxShadow: '0 1px 2px 0 rgba(0,0,0,0.03)',
                }}
                className={styles['wrapper-chart']}
            >
                <Line
                    {...config}
                    data={statisticsAnimal.dataBySUP}
                    style={{ overflowX: 'auto' }}
                />
            </div>
            <Flex wrap className={styles['statistics']} gap={16}>
                <div className={styles['statistics-item']}>
                    {`Средний СУП: ${statisticsAnimal.meanSUP} кг/сут`}
                </div>
                <div className={styles['statistics-item']}>
                    {`Максимальный СУП: ${statisticsAnimal.maxSUP} кг/сут`}
                </div>
                <div className={styles['statistics-item']}>
                    {`Минимальный СУП: ${statisticsAnimal.minSUP} кг/сут`}
                </div>
            </Flex>
        </Flex>
    );
};
