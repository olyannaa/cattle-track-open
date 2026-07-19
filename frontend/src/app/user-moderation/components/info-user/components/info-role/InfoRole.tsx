import { Flex } from 'antd';
import styles from './InfoRole.module.css';

export const InfoRole = () => {
    return (
        <div className={styles['info-role']}>
            <h3 className={styles['info-role__title']}>Права доступа:</h3>
            <Flex className={styles['roles']} vertical gap={'3px'}>
                <div>
                    <span>Организационный админ</span>: может удалять всех пользователей,
                    приглашать новых и изменять роли
                </div>
                <div>
                    <span>Локальный админ</span>: может только приглашать обычных
                    пользователей
                </div>
                <div>
                    <span>Пользователь</span>: не имеет доступа к странице модерации
                </div>
            </Flex>
        </div>
    );
};
