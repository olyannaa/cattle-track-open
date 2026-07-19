import { IAnimalTableBasic } from '../../data/interfaces/animalTable';
import { Badge } from 'antd';
import styles from './FieldTable.module.css';
import { dataIndexTypes } from '../../data/types/animal';

type Props = {
    animal: IAnimalTableBasic;
    dataIndex: dataIndexTypes;
};

export const FieldTable = ({ animal, dataIndex }: Props) => {
    const dataIndexTyped = dataIndex as keyof IAnimalTableBasic;
    const name: string | string[] = animal[dataIndexTyped];


    return dataIndex !== 'groupName' && dataIndex !== 'status' ? 
            <div
                className={styles[`text-cell__${dataIndex}`]}
            >
                { 
                    dataIndex === 'fatherTagNumbers' && Array.isArray(name)
                    ? (name as string[]).slice(0, 3).join(', ')
                    : name 
                }
            </div>
     : dataIndex === 'status' ? (
        <Badge
            status={name === 'Активное' ? 'success' : 'error'}
            text={name}
            className={styles[`text-cell__${dataIndex}`]}
        />
    ) : dataIndex === 'groupName' ? (
        <div
            className={styles[`text-cell__${dataIndex}`]}
        >
            {name}
        </div>
    ) : (
        ''
    );
};
