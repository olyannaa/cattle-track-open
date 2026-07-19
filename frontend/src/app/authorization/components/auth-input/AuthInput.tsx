import { Flex, Form, Input, Typography } from 'antd';
import styles from './AuthInput.module.css';
import { Rule } from 'antd/es/form';

type Props = {
    name: string;
    placeholder: string;
    type?: string;
    regExp?: RegExp;
    label: string;
    rules?: Rule[];
};

export const AuthInput = ({ name, placeholder, type = 'text', label, rules }: Props) => {
    return (
        <Flex vertical>
            <Typography.Title level={5}>{label}</Typography.Title>
            <Form.Item
                name={name}
                className={styles.formItem}
                rules={[
                    {
                        required: name !== 'ogrn' && name !== 'inn',
                        message: 'Обязательное поле',
                    },
                    {
                        max: name === 'login' ? 50 : name === 'password' ? 15 : undefined,
                        message: `Максимальная длина ${name === 'login' ? 50 : 15}`,
                    },
                    {
                        min: name === 'login' ? 6 : name === 'password' ? 8 : undefined,
                        message: `Минимальная длина ${name === 'login' ? 6 : 8}`,
                    },
                    {
                        pattern: name === 'login' ? /^[a-z0-9]+$/u : undefined,
                        message: 'Допустимы только строчные латинские буквы и цифры',
                    },
                    {
                        pattern: name === 'phone' ? /^\+7[0-9]{10}$/ : undefined,
                        message: 'Формат: +79991234567',
                    },
                    ...(rules || []),
                ]}
            >
                {type === 'password' ? (
                    <Input.Password
                        placeholder={placeholder}
                        type={type}
                        className={styles.loginInput}
                        size='large'
                    />
                ) : (
                    <Input
                        placeholder={placeholder}
                        type={type}
                        className={styles.loginInput}
                        size='large'
                    />
                )}
            </Form.Item>
        </Flex>
    );
};
