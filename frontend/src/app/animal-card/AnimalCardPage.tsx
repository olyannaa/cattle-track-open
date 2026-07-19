import { useEffect, useState } from 'react';
import { FilterFormInfo } from './components/filter-form/FilterFormInfo';
import { useAppSelector } from '../../app-service/hooks';
import {
    selectSelectedAnimals,
    setSelectedAnimal,
} from '../../app-service/slices/animalsDailyActionsSlice';
import { useLazyGetAnimalDetailQuery } from './services/animal-card';
import { AnimalDetail } from './data/interfaces/animal-details';
import { BaseInfo } from './components/base-info/BaseInfo';
import { HistoryEventWrapper } from './components/history-event/HistoryEventWrapper';
import { HistoryWeightWrapper } from './components/history-weight/HistoryWeightWrapper';
import { Lineage } from './components/lineage/Lineage';
import { Flex } from 'antd';
import { LoadingOutlined } from '@ant-design/icons';
import { Spin } from 'antd/lib';
import { useParams } from 'react-router-dom';
import { useDispatch } from 'react-redux';

export const AnimalCardPage = () => {
    const selectedAnimals = useAppSelector(selectSelectedAnimals);
    const [getAnimalDetails, { isLoading: loadBaseInfo }] = useLazyGetAnimalDetailQuery();
    const [animalData, setAnimalData] = useState<AnimalDetail | null>(null);
    const { id } = useParams<{ id: string }>();
    const dispatch = useDispatch();
    const handleSelectAnimal = (animalId: string) => {
        dispatch(setSelectedAnimal(animalId));
    };

    useEffect(() => {
        if (id) {
            handleSelectAnimal(id);
        }
    }, [id]);

    useEffect(() => {
        if (selectedAnimals.length) {
            getDetails();
        }
    }, [selectedAnimals]);

    const getDetails = async () => {
        try {
            const result = await getAnimalDetails(selectedAnimals[0]).unwrap();
            setAnimalData(result);
        } catch (error) {
            console.error('Ошибка при получении данных животного:', error);
        }
    };

    return (
        <div className='content-container'>
            <h1 className='form-title'>Карточка животного</h1>
            <FilterFormInfo />
            {loadBaseInfo && (
                <Flex className='top-margin-xl ' align='center' justify='center'>
                    <Spin
                        indicator={
                            <LoadingOutlined
                                style={{ color: '#FF4218', fontSize: 48 }}
                                spin
                            />
                        }
                    />
                </Flex>
            )}
            {animalData && <BaseInfo animal={animalData} updateAnimal={getDetails} />}
            {animalData && <HistoryEventWrapper />}
            {animalData && <HistoryWeightWrapper />}
            {animalData && <Lineage animal={animalData} />}
        </div>
    );
};
