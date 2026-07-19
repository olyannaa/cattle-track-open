import { Button, Form, message, Radio } from 'antd';
import { AuthInput } from '../../../auth-input/AuthInput';
import styles from './RegistrationForm.module.css';
import { useLazyCheckLoginQuery, useRegistrUserMutation } from '../../../../services/registrApi';
import { useLoginMutation } from '../../../../../../app-service/services/auth';
import { useNavigate, useParams } from 'react-router-dom';
import { ErrorType } from '../../../../../../utils/errorType';

export type DataRegistrType = {
    name: string;
    login: string;
    password: string;
    type: number;
    phone: string;
};

type Props = {
    isInvite: boolean
}

export const RegistrationForm = ({isInvite}: Props) => {
    const [messageApi, contextHolder] = message.useMessage();
    const [checkLoginQuery] = useLazyCheckLoginQuery()
    const [registerUser] = useRegistrUserMutation()
    const [login] = useLoginMutation()
    const navigate = useNavigate();
    const params = useParams()
    
    const onFinish= async (data: DataRegistrType) => {
        try{
            await registerUser({
                login: data.login,
                name: data.name,
                password: data.password,
                phoneNumber: data.phone.substring(1,data.phone.length),
                isOrgAdmin: data.type ? data.type === 1 : false
            }).unwrap()
            const response = await login({
                login: data.login,
                password: data.password,
            }).unwrap();
            localStorage.setItem('user', JSON.stringify(response));
            if (isInvite){
                navigate(`/invite/${params.token}`)
                return
            }
            if (response.organizationId === 'Нет организации'){
                navigate('/new-user');
                return
            }
            navigate('/accounting');
        }catch (err) {
            if ((err as ErrorType).data?.errorText === 'Номер телефона уже зарегестрирован.') {
                messageApi.open({
                    type: 'error',
                    content: 'Номер телефона уже зарегестрирован',
                    style: {
                        marginTop: '20vh',
                    },
                });
            } else {
                messageApi.open({
                    type: 'error',
                    content: 'Сервис временно не доступен. Попробуйте позже',
                    style: {
                        marginTop: '20vh',
                    },
                });
            }
        }
        
    };

    const checkLogin = async(login: string) => {
        try{
            const response = await checkLoginQuery(login).unwrap()
            return (response)
        }catch{
            return(false)
        }
    }

    return (
        <>
            {contextHolder}
            <Form className={styles['form-login']} onFinish={onFinish}>
                <AuthInput name='name' label='ФИО' placeholder='Введите ФИО' />
                <AuthInput 
                    name='login' 
                    label='Логин' 
                    placeholder='Введите логин' 
                    rules={[
                        () => ({
                            async validator(_, value) {
                            if (!value) return Promise.resolve();
                            
                            const isBusy = await checkLogin(value);
                            if (isBusy) {
                                return Promise.reject(new Error('Этот логин уже занят'));
                                
                            }
                            return Promise.resolve();
                            },
                        }),
                    ]}
                />
                <AuthInput
                    name='password'
                    label='Пароль'
                    placeholder='Введите пароль'
                    type='password'
                />
                <AuthInput
                    name='passwordConfirm'
                    label='Подтверждение пароля'
                    placeholder='Повторите пароль'
                    type='password'
                    rules={[
                        ({ getFieldValue }) => ({
                            validator(_, value) {
                                if (!value || getFieldValue('password') === value) {
                                    return Promise.resolve();
                                }
                                return Promise.reject(
                                    new Error('Пароли не совпадают')
                                );
                            },
                        }),
                    ]}
                />
                <AuthInput
                    name='phone'
                    label='Номер телефона'
                    placeholder='Введите номер'
                    type='phone'
                />
                {!isInvite && <Form.Item name='type' initialValue={1}>
                    <Radio.Group
                        options={[
                            {
                                value: 1,
                                label: 'Владелец',
                            },
                            {
                                value: 2,
                                label: 'Сотрудник',
                            },
                        ]}
                    />
                </Form.Item>}
                <Button
                    type='primary'
                    size='large'
                    color='default'
                    variant='solid'
                    className={styles['button_login']}
                    htmlType='submit'
                >
                    Зарегистрироваться
                </Button>
            </Form>
        </>
    );
};
