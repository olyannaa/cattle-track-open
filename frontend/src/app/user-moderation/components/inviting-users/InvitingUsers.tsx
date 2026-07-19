import { Button, Flex, message } from 'antd';
import { useState } from 'react';
import { FieldCustom } from '../../../../global-components/custom-inputs/field/Field';
import { CopyOutlined } from '@ant-design/icons';
import styles from './InvitingUsers.module.css';
import { SelectFilters } from '../../../../global-components/custom-inputs/filter-inputs/SelectFilters';
import { checkRole } from '../../../../const/roles';
import { useCreateLinkInviteMutation } from '../../services/apiModeration';
import { IUser } from '../../../../utils/userType';
import { rolesOptions } from '../../data/const/optionsSelect';

export const InvitingUsers = () => {
    const [inviteLink, setInviteLink] = useState<string>('');
    const [role, setRole] = useState<string>('8d5716d0-4cde-45f6-a67f-96323236b0f6');
    const [createLinkInvite] = useCreateLinkInviteMutation()
    const user: IUser = JSON.parse(localStorage.getItem('user')!)
    const [messageApi, contextHolder] = message.useMessage();

    const handleCopy = () => {
        navigator.clipboard.writeText(inviteLink);
    };

    const handleCreateLink = async () => {
        try{
            const response = await createLinkInvite({roleId: role, usageLimit: 1, expireTime: '7.00:00:00', orgId: user.organizationId}).unwrap()
            setInviteLink(response.link)
        }catch{
            messageApi.open({
                type: 'error',
                content: 'Сервис временно не доступен. Попробуйте позже',
                style: {
                    marginTop: '20vh',
                },
            });
        }
        
    };

    return (
        <>
        {contextHolder}
        <Flex vertical gap={'24px'} className={styles['inviting-users']}>
            <h2 className={styles['inviting-users__title']}>Приглашение пользователей</h2>
            <Flex className={styles['inviting-users__create-link']}>
                <SelectFilters
                    label='Роль для приглашения'
                    options={checkRole('org_admin') ? rolesOptions : rolesOptions.filter(role => role.label === 'Пользователь')}
                    value={role}
                    styles={{ marginBottom: 0 }}
                    onChange={setRole}
                />
                <Button
                    type='primary'
                    size='large'
                    color='default'
                    variant='solid'
                    style={{ alignSelf: 'flex-end' }}
                    onClick={handleCreateLink}
                >
                    Создать ссылку
                </Button>
            </Flex>
            {inviteLink && (
                <Flex gap={'10px'}>
                    <FieldCustom
                        label='Ссылка для приглашения'
                        value={inviteLink}
                        styles={{ marginBottom: '0' }}
                    />
                    <Button
                        variant='link'
                        className={styles['btn-copy']}
                        onClick={handleCopy}
                    >
                        <CopyOutlined className={styles['btn-copy__icon']} />
                    </Button>
                </Flex>
            )}
        </Flex>
        </>
    );
};
