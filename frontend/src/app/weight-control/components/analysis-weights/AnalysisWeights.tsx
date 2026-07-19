import { Flex, Spin } from 'antd';
import { TableWeights } from './components/TableWeights';
import { useAppSelector } from '../../../../app-service/hooks';
import { selectSelectedAnimalWeightControl } from '../../service/weightControlSlice';
import { ChartWeightDynamics } from './components/charts/ChartWeightDynamics';
import { useLazyGetWeightsStatisticsAnimalQuery } from '../../service/weightControl';
import { useEffect } from 'react';
import { ChartWeightGain } from './components/charts/chart-weight-gain/ChartWeightGain';
import { LoadingOutlined } from '@ant-design/icons';

export const AnalysisWeights = () => {
    const selectedAnimal = useAppSelector(selectSelectedAnimalWeightControl);
    const [getWeightsStatisticsAnimalQuery, { isLoading }] =
        useLazyGetWeightsStatisticsAnimalQuery();

    const getWeightsStatisticsAnimal = async () => {
        await getWeightsStatisticsAnimalQuery(selectedAnimal);
    };

    useEffect(() => {
        if (selectedAnimal) {
            getWeightsStatisticsAnimal();
        }
    }, [selectedAnimal]);

    return (
        selectedAnimal && (
            <Flex vertical gap={24}>
                {isLoading ? (
                    <Spin
                        indicator={
                            <LoadingOutlined
                                style={{ color: '#FF4218', fontSize: 48 }}
                                spin
                            />
                        }
                    />
                ) : (
                    <>
                        <div style={{ fontSize: '24px', fontWeight: '500' }}>
                            Динамика веса животного
                        </div>
                        <ChartWeightDynamics byItem='age' />
                        <ChartWeightDynamics byItem='date' />
                        <div style={{ fontSize: '24px', fontWeight: '500' }}>
                            Среднесуточные привесы
                        </div>
                        <ChartWeightGain />
                        <TableWeights />{' '}
                    </>
                )}
            </Flex>
        )
    );
};
