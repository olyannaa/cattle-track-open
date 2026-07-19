import { useEffect } from "react";
import { useGetCommonStatQuery } from "../../services/animals";
import { Flex } from "antd";
import styles from './Statistics.module.css'
import { AnimalType } from "../../data/const/animalTypes";

export const Statistics = () => {
    const { data: livestock, refetch } = useGetCommonStatQuery();
    
    useEffect(() => {
        refetch();
    }, []);
    

    return (
        <Flex vertical className={styles['statistics']} gap={16}>
            <h2>Статистика</h2>
            <Flex gap={10} wrap={'wrap'} className={styles['statistics-items']}>
            {livestock &&
                Object.entries(AnimalType).map(([key, value]) => (
                    <Flex key={key} className={styles['statistics-item']} align="center" justify="center">
                        <p className={styles['statistics-item__title']}>{`${value}: `}<span className={styles['statistics-item__count']}>{livestock[key]}</span></p>
                    </Flex>
                ))}
            </Flex>
        </Flex>
    )
}