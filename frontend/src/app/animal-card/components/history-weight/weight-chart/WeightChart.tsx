import { useEffect, useState } from 'react';
import { HistoryPoint } from '../../../data/interfaces/animal-chart';
import { Line } from '@ant-design/plots';
import { getCountItemsChart } from '../../../../weight-control/functions/getCountItemsChart';
import { useWindowSize } from '../../../../../hooks/useWindowSize';
import { chartStyles } from '../../../../../styles/chart-styles';
import { EmptyDataAlert } from '../../../../../global-components/expty-data-alert/EmptyDataAlert';

export const WeightChart = ({ points }: { points: HistoryPoint[] }) => {
    const widthWindow = useWindowSize();
    const [countItem, setCountItem] = useState(getCountItemsChart('', widthWindow));

    useEffect(() => {
        setCountItem(getCountItemsChart('', widthWindow));
    }, [widthWindow]);

    const config = {
        data: points,
        xField: 'x',
        yField: 'y',
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
            title: (d: HistoryPoint) => d.x,
            items: [
                (d: HistoryPoint) => ({
                    color: '#ff4218',
                    name: 'Вес',
                    value: `${d.y} кг`,
                }),
            ],
        },
        scrollbar: {
            x:
                points.length < countItem
                    ? false
                    : {
                          ratio: countItem / points.length,
                      },
        },
        ...chartStyles,
    };

    if (!points.length) {
        return <EmptyDataAlert />;
    }

    return <Line className='bottom-margin-xl' {...config} style={{ overflowX: 'auto' }} />;
};
