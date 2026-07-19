import { useEffect, useState } from 'react';
import { useAppSelector } from '../../../../app-service/hooks';
import { useLazyGetParentsQuery } from '../../services/animal-card';
import { selectSelectedAnimals } from '../../../../app-service/slices/animalsDailyActionsSlice';
import { AnimalDetail } from '../../data/interfaces/animal-details';
import { Flex } from 'antd';
import { ParentCard } from './components/parent-card/ParentCard';
import styles from './Lineage.module.css';

export const Lineage = ({ animal }: { animal: AnimalDetail }) => {
    const selectedAnimals = useAppSelector(selectSelectedAnimals);
    const [getAnimalParents] = useLazyGetParentsQuery();
    const [data, setData] = useState<AnimalDetail[]>([]);
    const [father, setFather] = useState<AnimalDetail[]>([]);
    const [mother, setMother] = useState<AnimalDetail[]>([]);
    const [grandfatherFather, setGrandfatherFather] = useState<AnimalDetail[]>([]);
    const [grandmotherFather, setGrandmotherFather] = useState<AnimalDetail[]>([]);
    const [grandfatherMother, setGrandfatherMother] = useState<AnimalDetail[]>([]);
    const [grandmotherMother, setGrandmotherMother] = useState<AnimalDetail[]>([]);

    useEffect(() => {
        if (selectedAnimals.length) {
            fetchData();
        }
    }, [selectedAnimals]);

    useEffect(() => {
        const filterData = data.filter((item) => item !== null);
        setMother(filterData.filter((parent) => parent.name === 'Мать') || []);
        setFather(filterData.filter((parent) => parent.name === 'Отец') || []);
        setGrandfatherFather(
            filterData.filter((parent) => parent.name === 'Дедушка (отец отца)') || []
        );
        setGrandmotherFather(
            filterData.filter((parent) => parent.name === 'Бабушка (мать отца)') || []
        );
        setGrandfatherMother(
            filterData.filter((parent) => parent.name === 'Дедушка (отец матери)') || []
        );
        setGrandmotherMother(
            filterData.filter((parent) => parent.name === 'Бабушка (мать матери)') || []
        );
    }, [data]);

    const fetchData = async () => {
        try {
            const response = await getAnimalParents(selectedAnimals[0]).unwrap();
            setData(response);
        } catch (err) {
            console.error(err);
        }
    };

    return (
        <Flex vertical>
            <h2>Родословная животного №{animal.tagNumber}</h2>
            <Flex className={styles.gridContainer}>
                <div className={styles.gridItem}>Номер: {animal.tagNumber}</div>
                <div className={styles.gridItem}>Порода: {animal.breed}</div>
                <div className={styles.gridItem}>Дата рождения: {animal.birthDate}</div>
                <div className={styles.gridItem}>Статус: {animal.status}</div>
            </Flex>
            <Flex wrap={true} gap={16}>
                <Flex className={styles.lineage__col} vertical gap={24}>
                    {[father, grandfatherFather, grandmotherFather]
                        .filter((arr) => arr.length > 0)
                        .map((parent, i) => (
                            <ParentCard key={i} parents={parent} />
                        ))}
                </Flex>
                <Flex className={styles.lineage__col} vertical gap={24}>
                    {[mother, grandfatherMother, grandmotherMother]
                        .filter((arr) => arr.length > 0)
                        .map((parent, i) => (
                            <ParentCard key={i} parents={parent} />
                        ))}
                </Flex>
            </Flex>
        </Flex>
    );
};
