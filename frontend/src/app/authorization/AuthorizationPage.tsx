import { Flex } from 'antd';
import styles from './AuthorizationPage.module.css';
import { AuthSelect } from './components/auth-select/AuthSelect';
import { useNavigate } from 'react-router-dom';
import { useEffect } from 'react';
import logo from '../../assets/header-logo.svg';
import { IUser } from '../../utils/userType';

type Props = {
    isInvite?: boolean;
}

export const Authorization = ({isInvite= false}: Props) => {
    const navigate = useNavigate();
    const user: IUser = JSON.parse(localStorage.getItem('user') || 'null');

    useEffect(() => {
        if (user) {
            navigate(user.organizationId === 'Нет организации' ? '/new-user' : '/accounting');
        }
    }, []);

    return (
        <Flex className={styles.wrapper} align='center' justify='center'>
            <Flex className={styles.header}>
                <Flex className={styles.header__logo}>
                    <img width={124} src={logo} />
                </Flex>
            </Flex>
            <Flex className={styles.body} align='center' justify='center'>
                <AuthSelect isInvite={isInvite}/>
            </Flex>
        </Flex>
    );
};
