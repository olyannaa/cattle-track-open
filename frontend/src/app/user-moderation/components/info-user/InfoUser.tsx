import { Flex } from 'antd';
import { FieldCustom } from '../../../../global-components/custom-inputs/field/Field';
import styles from './InfoUser.module.css';
import { InputLabel } from '../../../../global-components/custom-inputs/input-label/InputLabel';
import { RoleIcon } from '../role-icon/RoleIcon';
import { InfoRole } from './components/info-role/InfoRole';
import { useAppSelector } from '../../../../app-service/hooks';
import { selectUserInfo } from '../../services/moderationSlice';

export const InfoUser = () => { 
    const infoUser = useAppSelector(selectUserInfo)
    return (
        <Flex vertical gap={'24px'} className={styles['info-user']}>
            <h2 className={styles['info-user__title']}>Информация о пользователе</h2>
            <Flex style={{ columnGap: '16px' }} vertical>
                <Flex style={{ width: '100%', columnGap: '16px' }} wrap>
                    <FieldCustom label='Логин' value={infoUser.login} />
                    <FieldCustom label='ФИО' value={infoUser.name || ''} />
                    <FieldCustom label='Организация' value={infoUser.orgName} />
                    <div style={{ marginBottom: '24px', maxWidth: '475px', width: '100%' }}>
                        <InputLabel label='Роль' />
                        <RoleIcon role={infoUser.roleId} />
                    </div>
                </Flex>
                {/* <Flex style={{ width: '100%', columnGap: '16px' }} wrap>
                    <ChangePassword userId={user.id}/>
                    
                </Flex> */}
                <InfoRole />
            </Flex>
        </Flex>
    );
};
