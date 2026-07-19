import { Flex } from 'antd';
import { useGetGroupStatQuery } from '../../../../services/feeding-record';
import { useEffect, useState } from 'react';
import { IGroupRationInfo } from '../../../../data/group-ratio-info';
import styles from './GroupInfo.module.css';

export const GroupInfo = ({ id }: { id: string }) => {
    const { data: groupData = [] } = useGetGroupStatQuery();
    const [currentData, setCurrentData] = useState<IGroupRationInfo | null>();

    useEffect(() => {
        if (id && groupData) {
            setCurrentData(groupData.find((group) => group.groupId === id) ?? null);
        }
    }, [id, groupData]);

    return (
        <Flex vertical className='form-additional' style={{ maxWidth: 1200 }}>
            <h2 className='form-title'>Информация о группе</h2>
            <div className={styles['group-info__container']}>
                <div className={styles['group-info__item']}>
                    <p>Название группы:</p>
                    <p>
                        <strong>{currentData?.groupName}</strong>
                    </p>
                </div>
                <div className={styles['group-info__item']}>
                    <p>Количество голов:</p>
                    <p>
                        <strong>{currentData?.activeAnimalsCount}</strong>
                    </p>
                </div>
            </div>
            <div className={styles['group-info__container']}>
                <div className={styles['group-info__item']}>
                    <p>Рацион:</p>
                    <p>
                        <strong>{currentData?.rationName}</strong>
                    </p>
                </div>
                <div className={styles['group-info__item']}>
                    <p>Итоговая стоимость:</p>
                    <h4>
                        <strong>
                            {(currentData?.rationCostPerHead ?? 0) * (currentData?.activeAnimalsCount ?? 0)}
                        </strong>
                    </h4>
                </div>
            </div>
        </Flex>
    );
};
