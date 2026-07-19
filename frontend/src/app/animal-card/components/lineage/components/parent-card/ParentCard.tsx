import { Card } from 'antd';
import { AnimalDetail } from '../../../../data/interfaces/animal-details';
import { useDispatch } from 'react-redux';
import { setSelectedAnimal } from '../../../../../../app-service/slices/animalsDailyActionsSlice';
import styles from './ParentCard.module.css';

export const ParentCard = ({ parents }: { parents: AnimalDetail[] }) => {
    const dispatch = useDispatch();

    const handleSelectAnimal = (animalId: string) => {
        dispatch(setSelectedAnimal(animalId));
    };
    console.log(parents);
    return (
        <Card className={styles['parent-card']} title={parents[0].name}>
            {parents.map((parent) => (
                <div key={parent.id} className={styles['parent-card__info']}>
                    <p>
                        {'Номер: '}
                        <span
                            className={styles['parent-card__link']}
                            onClick={() => handleSelectAnimal(parent.id)}
                        >
                            {parent.tagNumber}
                        </span>
                    </p>
                    <p>Порода: {parent.breed}</p>
                    <p>Дата рождения: {parent.birthDate}</p>
                    <p>Статус: {parent.status}</p>
                </div>
            ))}
        </Card>
    );
};
