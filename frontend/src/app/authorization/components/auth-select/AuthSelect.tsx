import { Flex, Tabs, TabsProps, Typography } from 'antd';
import styles from './AuthSelect.module.css';
import { useState } from 'react';
import { LoginForm } from './components/login-form/LoginForm';
import { RegistrationForm } from './components/registration-form/RegistrationForm';

const tabsLogin: TabsProps['items'] = [
    {
        label: 'Вход',
        key: '1',
    },
    {
        label: 'Регистрация',
        key: '2',
    },
];

type Props = {
    isInvite: boolean;
}

export const AuthSelect = ({isInvite}: Props) => {
    const [currentTab, setCurrentTab] = useState<string>(isInvite ? '2' : '1');

    return (
        <Flex className={styles.login} vertical>
            <Typography.Title level={2} className={styles['login__title']}>
                Система учета КРС
            </Typography.Title>
            <Tabs
                defaultActiveKey={isInvite ? '2' : '1'}
                items={tabsLogin}
                onChange={setCurrentTab}
                size='large'
                style={{ marginBottom: '24px' }}
            />
            {currentTab === '1' ? <LoginForm isInvite={isInvite}/> : <RegistrationForm isInvite={isInvite}/>}
        </Flex>
    );
};
