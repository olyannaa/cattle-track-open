import { Flex, Result } from 'antd';

export const InDevelopment = () => {
    return (
        <Flex className='content-container'>
            <Result style={{ margin: 'auto' }} status='404' title='Раздел находится в разработке' />
        </Flex>
    );
};
