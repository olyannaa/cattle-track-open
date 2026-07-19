import { Flex } from 'antd';
import { InfoUser } from './components/info-user/InfoUser';
import { InvitingUsers } from './components/inviting-users/InvitingUsers';
import { UserManagement } from './components/user-management/UserManagement';
import { useGetUsersModerationQuery } from './services/apiModeration';
import { useGetUserInfoQuery } from '../../app-service/services/general';

export const UserModerationPage = () => {
    const {isLoading: isLoadingUserInfo} = useGetUserInfoQuery()
    const {isLoading: isLoadingUsersModeartion} = useGetUsersModerationQuery()

    return (
        <Flex vertical gap={'16px'} style={{ maxWidth: '1000px' }}>
            {isLoadingUserInfo  || isLoadingUsersModeartion ? 
            'Загрузка' : 
            <>
                <InfoUser />
                <InvitingUsers />
                <UserManagement />
            </>}
        </Flex>
    );
};
