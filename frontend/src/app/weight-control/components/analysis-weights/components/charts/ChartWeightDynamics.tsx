import { Line, LineConfig } from '@ant-design/charts';
import { useAppSelector } from '../../../../../../app-service/hooks';
import { selectStatisticsAnimal } from '../../../../service/weightControlSlice';
import { Flex } from 'antd';
import { useWindowSize } from '../../../../../../hooks/useWindowSize';
import { useEffect, useState } from 'react';
import { getCountItemsChart } from '../../../../functions/getCountItemsChart';
import { chartStyles } from '../../../../../../styles/chart-styles';
import { EmptyDataAlert } from '../../../../../../global-components/expty-data-alert/EmptyDataAlert';

type Props = {
    byItem: 'date' | 'age';
};

export const ChartWeightDynamics = ({ byItem }: Props) => {
    const statisticsAnimal = useAppSelector(selectStatisticsAnimal);
    const widthWindow = useWindowSize();
    const [countItem, setCountItem] = useState(getCountItemsChart(byItem, widthWindow));
    useEffect(() => {
        setCountItem(getCountItemsChart(byItem, widthWindow));
    }, [widthWindow]);

    const config: LineConfig = {
        xField: byItem,
        yField: 'weight',
        height: 300,

        point: {
            sizeField: 5,
            style: {
                fill: '#ff4218',
                stroke: '#fff',
                lineWidth: 2,
                shadowColor: 'rgba(255,66,24,1)',
                shadowBlur: 4,
            },
        },
        tooltip: {
            title: (d) => (byItem === 'age' ? `${d.age} мес` : d.date),
            items: [
                (d) => ({
                    color: '#ff4218',
                    name: 'Вес',
                    value: d.weight,
                }),
            ],
        },
        scrollbar: {
            x:
                (byItem === 'age' && statisticsAnimal.dataByAge.length < countItem) ||
                (byItem === 'date' && statisticsAnimal.dataByDate.length < countItem)
                    ? false
                    : {
                          ratio:
                              byItem === 'age'
                                  ? countItem / statisticsAnimal.dataByAge.length
                                  : countItem / statisticsAnimal.dataByDate.length,
                      },
        },
        ...chartStyles,
    };

    return (
        <Flex gap={24} vertical>
            <div style={{ fontSize: '16px', fontWeight: '500' }}>
                {byItem === 'age' ? 'Вес по возрасту (месяцам)' : 'Вес по датам'}
            </div>
            {!statisticsAnimal.dataByAge.length ? (
                <EmptyDataAlert />
            ) : (
                <Line
                    {...config}
                    style={{ overflowX: 'auto' }}
                    data={
                        byItem === 'age'
                            ? statisticsAnimal.dataByAge.map((item) => ({
                                  age: `${item.age} мес`,
                                  weight: item.weight,
                              }))
                            : statisticsAnimal.dataByDate
                    }
                />
            )}
        </Flex>
    );
};
