import { Button, Flex } from 'antd';
import { Rations } from './components/rations/Rations';
import { useState } from 'react';
import { PlusOutlined } from '@ant-design/icons';
import { CreateDietModal } from '../create-diet/CreateDiet';

export const Diets = () => {
    const [searchValue, setSearchValue] = useState('');
    const [isModalOpen, setIsModalOpen] = useState(false);
    return (
        <Flex vertical className='content-container content_without-max-width'>
            <Flex className='content-align-center bottom-margin-xl'>
                <h2>Рационы</h2>
                <Button type='primary' icon={<PlusOutlined />} onClick={() => setIsModalOpen(true)}>
                    Создать рацион
                </Button>
            </Flex>
            <Rations searchValue={searchValue} onSearchChange={setSearchValue} />
            <CreateDietModal open={isModalOpen} onCancel={() => setIsModalOpen(false)} />
        </Flex>
    );
};
