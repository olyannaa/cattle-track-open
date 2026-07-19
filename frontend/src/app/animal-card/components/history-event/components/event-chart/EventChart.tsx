import { useEffect, useState } from 'react';
import { useAppSelector } from '../../../../../../app-service/hooks';
import { selectSelectedAnimals } from '../../../../../../app-service/slices/animalsDailyActionsSlice';
import { useLazyGetChartInfoQuery } from '../../../../services/animal-card';
import { DatePicker, Spin } from 'antd';
import dayjs from 'dayjs';
import isSameOrAfter from 'dayjs/plugin/isSameOrAfter';
import isSameOrBefore from 'dayjs/plugin/isSameOrBefore';
import styles from '../Event.module.css';

const { RangePicker } = DatePicker;
dayjs.extend(isSameOrAfter);
dayjs.extend(isSameOrBefore);
import Plot from 'react-plotly.js';
import { EmptyDataAlert } from '../../../../../../global-components/expty-data-alert/EmptyDataAlert';

interface HistoryEventPoint {
    x: string;
    y: string;
    description?: string;
}

interface AnimalHistoryData {
    points: HistoryEventPoint[];
    title: string;
    xAxisLabel: string;
    yAxisLabel: string;
}

const colorPalette = [
    '#FF4218',
    '#FF6A3D',
    '#FF8C66',
    '#FFAA8C',
    '#FFD3C4',
    '#D32F2F',
    '#E53935',
    '#F44336',
    '#E57373',
    '#EF9A9A',
    '#FFC1B3',
];

export const EventChart: React.FC = () => {
    const selectedAnimals = useAppSelector(selectSelectedAnimals);
    const [getChartData, { isLoading }] = useLazyGetChartInfoQuery();
    const [chartData, setChartData] = useState<AnimalHistoryData | null>(null);
    const [dateRange, setDateRange] = useState<[dayjs.Dayjs, dayjs.Dayjs] | null>(null);

    useEffect(() => {
        if (selectedAnimals.length) {
            fetchChart();
        }
    }, [selectedAnimals]);

    const fetchChart = async () => {
        try {
            const response = await getChartData(selectedAnimals[0]).unwrap();
            setChartData(response);
        } catch (err) {
            console.error(err);
        }
    };

    if (!chartData || !chartData.points.length) {
        return <EmptyDataAlert />;
    }

    const filteredPoints = dateRange
        ? chartData.points.filter(
              (p) =>
                  dayjs(p.x).isSameOrAfter(dateRange[0], 'day') &&
                  dayjs(p.x).isSameOrBefore(dateRange[1], 'day')
          )
        : chartData.points;

    const eventTypes = Array.from(new Set(filteredPoints.map((p) => p.y)));

    const traces = eventTypes.map((eventType, index) => {
        const filteredData = filteredPoints.filter((p) => p.y === eventType);
        return {
            x: filteredData.map((p) => p.x),
            y: filteredData.map((p) => p.y),
            mode: 'markers',
            type: 'scatter',
            name: eventType,
            marker: {
                size: 10,
                symbol: 'circle',
                opacity: 0.9,
                color: colorPalette[index % colorPalette.length],
            },
            text: filteredData.map((p) => p.description || ''),
            hoverinfo: 'text+x+y',
            showlegend: false,
        };
    });

    return (
        <div className={styles['event']}>
            <div>
                <RangePicker
                    className={styles['event__date']}
                    onChange={(dates) => {
                        if (dates) setDateRange(dates as [dayjs.Dayjs, dayjs.Dayjs]);
                        else setDateRange(null);
                    }}
                    format='YYYY-MM-DD'
                    allowClear
                />
            </div>
            {isLoading ? (
                <Spin />
            ) : (
                <Plot
                    data={traces}
                    layout={{
                        title: chartData.title || 'Хронология событий',
                        xaxis: {
                            title: chartData.xAxisLabel || 'Дата',
                            type: 'date',
                            tickformat: '%d.%m.%Y',
                            autorange: true,
                            gridcolor: '#e0e0e0',
                            gridwidth: 1,
                            griddash: 'dot',
                        },
                        yaxis: {
                            title: chartData.yAxisLabel || 'Тип события',
                            type: 'category',
                            categoryorder: 'array',
                            categoryarray: eventTypes,
                            automargin: true,
                            gridcolor: '#e0e0e0',
                            gridwidth: 1,
                            griddash: 'dot',
                        },
                        margin: {
                            l: 120,
                            r: 20,
                            t: 40,
                            b: 40,
                        },
                        height: 500,
                        showlegend: true,
                        hoverlabel: {
                            bordercolor: 'transparent',
                            font: {
                                color: '#000000',
                                size: 12,
                            },
                        },
                    }}
                    style={{ width: '100%', height: '100%' }}
                    config={{ responsive: true }}
                />
            )}
        </div>
    );
};
