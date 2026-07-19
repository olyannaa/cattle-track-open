import { Alert } from 'antd';

export const EmptyDataAlert = () => {
    return <Alert className='alert' message='Нет данных' type='info' showIcon />;
};
