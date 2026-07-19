import { Flex, Form, Input, Typography } from "antd";

type Props = {
    name: string;
    placeholder: string;
    type?: string;
    label: string;
};

export const RegistrInput = ({ name, placeholder, type = 'text', label}: Props) => {
    return (
        <Flex vertical>
            <Typography.Title level={5}>{label}</Typography.Title>
            <Form.Item
                name={name}
                rules={[
                    {
                        required: name !== 'ogrn' && name !== 'inn',
                        message: 'Обязательное поле',
                    }
                ]}
            >
                <Input
                    placeholder={placeholder}
                    type={type}
                    size='large'
                />
            </Form.Item>
        </Flex>
    );
};
