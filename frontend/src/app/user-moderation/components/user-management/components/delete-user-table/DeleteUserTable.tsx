import { Button} from 'antd';
import { DeleteOutlined} from '@ant-design/icons';
import { IUserTable } from '../../UserManagement';
import { useDeleteUserMutation, useLazyGetUsersModerationQuery } from '../../../../services/apiModeration';
import { ConfirmModal } from '../../../../../../global-components/confirm-modal/ConfirmModal';
import { useState } from 'react';

type Props = {
    user: IUserTable
}

export const DeleteUserTable = ({user}: Props) => {
    const [deleteUser] = useDeleteUserMutation()
    const [getUsersModeration] = useLazyGetUsersModerationQuery()
    const [isConfirmOpen, setIsConfirmOpen] = useState(false);

    const handleDeleteUser = async ()=> {
        try{
            await deleteUser(user.id).unwrap()
            await getUsersModeration()
        }catch(error){
            console.error('Failed to delete user', error);
        } finally {
            setIsConfirmOpen(false);
        }
    }

    return (
        <>
            <Button
                variant='link'
                style={{ color: 'rgb(239 68 68)', display: 'inline-block' }}
                onClick={() => setIsConfirmOpen(true)}
            >
                <DeleteOutlined width={'16px'} />
            </Button>
            <ConfirmModal
                isOpen={isConfirmOpen}
                title={`Удалить пользователя ${user.login}?`}
                okButtonText='Удалить'
                cancelButtonText='Отмена'
                onConfirm={handleDeleteUser}
                onCancel={() => setIsConfirmOpen(false)}
            />
        </>
    )
};
