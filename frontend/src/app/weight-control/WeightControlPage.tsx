import { Flex, message } from 'antd';
import { HeaderContent } from '../../global-components/header-content/HeaderContent';
import { useState } from 'react';
import { items } from './data/const/tabs';
import { Filters } from './components/filters/Filters';
import { FormAddWeight } from './components/form-add-weight/FormAddWeight';
import { AnalysisWeights } from './components/analysis-weights/AnalysisWeights';

export const WeightControlPage = () => {
    const [activeTab, setActiveTab] = useState('Взвешивание');
    const [messageApi, contextHolder] = message.useMessage();
    const success = (message: string) => {
        messageApi.open({
            type: 'success',
            content: message,
        });
    };

    return (
        <Flex vertical gap={16} style={{ maxWidth: '920px' }}>
            {contextHolder}
            <HeaderContent
                title='Контроль привесов'
                items={items}
                onChange={setActiveTab}
            />
            <Flex
                style={{
                    padding: '24px 20px 15px',
                    background: 'var(--global-bg)',
                    borderRadius: '8px',
                }}
                vertical
                gap={24}
            >
                <h2>
                    {activeTab === 'Взвешивание' ? 'Регистрация взвешивания' : activeTab}
                </h2>
                <Filters keyTab={activeTab} />
                {activeTab === 'Взвешивание' && (
                    <FormAddWeight success={success} />
                )}
                {activeTab === 'Анализ привесов' && <AnalysisWeights />}
            </Flex>
        </Flex>
    );
};
