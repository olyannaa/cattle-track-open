import { Flex, Skeleton } from 'antd';
import { WeightChart } from './weight-chart/WeightChart';
import { WeightTable } from './weight-table/WeightTable';
import { useAppSelector } from '../../../../app-service/hooks';
import { useLazyGetWeightInfoQuery } from '../../services/animal-card';
import { selectSelectedAnimals } from '../../../../app-service/slices/animalsDailyActionsSlice';
import { useEffect, useState } from 'react';
import { AnimalHistoryData } from '../../data/interfaces/animal-chart';

export const HistoryWeightWrapper = () => {
    const selectedAnimals = useAppSelector(selectSelectedAnimals);
    const [getChartData, { isLoading }] = useLazyGetWeightInfoQuery();
    const [chartData, setChartData] = useState<AnimalHistoryData | null>(null);

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

    if (isLoading) {
        return (
            <Flex className='top-margin-xl bottom-margin-xl' vertical gap={4}>
                <Skeleton.Input active={true} block={true} />
                <Skeleton.Input active={true} block={true} />
                <Skeleton.Input active={true} block={true} />
            </Flex>
        );
    }

    return (
        <Flex vertical>
            <h2 className='form-title'>Изменение веса</h2>
            {chartData && <WeightChart points={chartData.points} />}
            {chartData && <WeightTable points={chartData.points} />}
        </Flex>
    );
};
