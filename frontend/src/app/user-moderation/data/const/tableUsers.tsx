import { TableProps } from 'antd';
import { IUserTable } from '../../components/user-management/UserManagement';
import { RoleIcon } from '../../components/role-icon/RoleIcon';
import { DeleteUserTable } from '../../components/user-management/components/delete-user-table/DeleteUserTable';
import { ChangeRoleTable} from '../../components/user-management/components/delete-user-table/change-role-table/ChangeRoleTable';

export const tableUsers = (role: string, userId: string): TableProps<IUserTable>['columns'] => [
    {
        title: 'Логин',
        dataIndex: 'login',
        key: 'login',
        minWidth: 100,
    },
    {
        title: 'ФИО',
        dataIndex: 'name',
        key: 'name',
        minWidth: 150,
    },
    {
        title: 'Роль',
        dataIndex: 'roleId',
        key: 'roleId',
        minWidth: 154,
        render: (_, user) => role === 'org_admin' && userId !== user.id ? <ChangeRoleTable user={user}/> : <RoleIcon role={user.roleId} />,
    },

    ...(role === 'org_admin' ? [{
        title: 'Удаление',
        dataIndex: 'delete',
        key: 'delete',
        minWidth: 130,
        render: (_:unknown, user:IUserTable) =>  userId !== user.id ?<DeleteUserTable  user={user}/> : '',
    }] : []),
];
