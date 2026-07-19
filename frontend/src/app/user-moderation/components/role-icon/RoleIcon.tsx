import { CrownOutlined, ToolFilled, UserOutlined } from '@ant-design/icons';
import { Tag } from 'antd';

type Props = {
    role: string;
};

export const RoleIcon = ({ role }: Props) => {
    if (role === '8d5716d0-4cde-45f6-a67f-96323236b0f8') {
        return (
            <div>
                <Tag icon={<CrownOutlined />} color='rgb(239 68 68)'>
                    Орг. админ
                </Tag>
            </div>
        );
    } else if (role === '8d5716d0-4cde-45f6-a67f-96323236b0f7') {
        return (
            <div>
                <Tag icon={<ToolFilled />} color='rgba(255, 66, 24, 1)'>
                    Лок. админ
                </Tag>
            </div>
        );
    } else {
        return (
            <div>
                <Tag icon={<UserOutlined />} color='rgb(59 130 246)'>
                    Пользователь
                </Tag>
            </div>
        );
    }
};
