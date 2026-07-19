import { Flex, Table } from 'antd';
import { tableUsers } from '../../data/const/tableUsers';
import styles from './UserManagement.module.css';
import { useAppSelector } from '../../../../app-service/hooks';
import { selectUserInfo, selectUsersModeration } from '../../services/moderationSlice';

export type IUserTable = {
    key: string;
    name: string;
    login: string;
    roleId: string;
    id: string;
};

export const UserManagement = () => {
    const users = useAppSelector(selectUsersModeration)
    const userInfo = useAppSelector(selectUserInfo)
    return (
        <Flex vertical className={styles['user-management']} gap={24}>
            <h2 className={styles['user-management__title']}>
                Управление пользователями
            </h2>
            <Table<IUserTable>
                columns={tableUsers(userInfo.roleName, userInfo.id)}
                style={{
                    width: '100%',
                    overflowX: 'auto',
                }}
                dataSource={users.map((user) => ({
                    ...user,
                    key: user.id,
                }))}
                pagination={false}
            />
        </Flex>
    );
};
