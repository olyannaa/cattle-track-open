import { useEffect, useState } from 'react';
import { useAppSelector } from '../../../../../../app-service/hooks';
import {
    selectSelectedAnimals,
    setSelectedAnimal,
} from '../../../../../../app-service/slices/animalsDailyActionsSlice';
import { useLazyGetAnimalActionsQuery } from '../../../../services/animal-card';
import { AnimalAction } from '../../../../data/interfaces/animal-actions';
import { Button, Collapse, CollapseProps, Flex, Pagination, Skeleton } from 'antd';
import styles from '../Event.module.css';
import { useDispatch } from 'react-redux';

const PAGE_SIZE = 5;

export const Events = () => {
    const selectedAnimals = useAppSelector(selectSelectedAnimals);
    const [getAnimalActions, { isLoading: loadEvents }] = useLazyGetAnimalActionsQuery();
    const [data, setData] = useState<AnimalAction[]>([]);
    const [currentPage, setCurrentPage] = useState(1);
    const dispatch = useDispatch();

    const handleSelectAnimal = (animalId?: string) => {
        if (animalId) {
            dispatch(setSelectedAnimal(animalId));
        }
    };

    useEffect(() => {
        if (selectedAnimals.length) {
            fetchData();
        }
    }, [selectedAnimals]);

    const fetchData = async () => {
        try {
            const response = await getAnimalActions(selectedAnimals[0]).unwrap();
            setData(response);
            setCurrentPage(1);
        } catch (err) {
            console.error(err);
        }
    };

    const formatDate = (dateString: string) => {
        const date = new Date(dateString);
        return date.toLocaleDateString('ru-RU');
    };

    const paginatedData = data.slice(
        (currentPage - 1) * PAGE_SIZE,
        currentPage * PAGE_SIZE
    );

    const collapseItems: CollapseProps['items'] = paginatedData.map((action) => ({
        key: action.actionId,
        label: (
            <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                <span>{action.eventType}</span>
                <span>{formatDate(action.eventDate)}</span>
            </div>
        ),
        children: (
            <div>
                {Object.entries(action.fields).map(([key, value]) => (
                    <p key={key}>
                        <span>
                            {key}: {value || '—'}
                        </span>
                    </p>
                ))}
                {action.performedBy && <p>Выполнил: {action.performedBy}</p>}
                {action.eventType === 'Отёл' && action.calfId && (
                    <Button
                        style={{ marginTop: '8px' }}
                        onClick={() => handleSelectAnimal(action.calfId)}
                    >
                        Открыть карточку теленка #{action.fields['Номер телёнка']}
                    </Button>
                )}
                {action.eventType === 'Осеменение' && action.bullId && (
                    <Button
                        style={{ marginTop: '8px' }}
                        onClick={() => handleSelectAnimal(action.bullId)}
                    >
                        Открыть карточку быка
                    </Button>
                )}
            </div>
        ),
    }));

    if (loadEvents) {
        return (
            <Flex className={styles.event} vertical gap={4}>
                <Skeleton.Input active={true} block={true} />
                <Skeleton.Input active={true} block={true} />
                <Skeleton.Input active={true} block={true} />
            </Flex>
        );
    }

    return (
        <Flex className={styles.event} vertical>
            <Collapse
                className={styles.event__list}
                items={collapseItems}
                style={{ width: '100%' }}
            />
            {data.length > PAGE_SIZE && (
                <Pagination
                    current={currentPage}
                    pageSize={PAGE_SIZE}
                    total={data.length}
                    onChange={(page) => setCurrentPage(page)}
                    style={{ marginTop: 16, alignSelf: 'center' }}
                />
            )}
        </Flex>
    );
};
