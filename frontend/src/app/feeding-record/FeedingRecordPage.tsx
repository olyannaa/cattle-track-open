import { TabsProps } from 'antd';
import { useState } from 'react';
import { Diets } from './components/diets/Diets';
import { Components } from './components/components/Components';
import { HeaderContent } from '../../global-components/header-content/HeaderContent';
import { MainWrapper } from './components/main/MainWrapper';
import { FeedingAnalysisPage } from './components/feeding-analysis/FeedingAnalysisPage';

export const FeedingRecordPage = () => {
    const [activeTab, setActiveTab] = useState('1');
    const items: TabsProps['items'] = [
        {
            key: '1',
            label: 'Главная',
        },
        {
            key: '2',
            label: 'Рационы',
        },
        {
            key: '3',
            label: 'Компоненты',
        },
        {
            key: '4',
            label: 'Анализ кормления',
        },
    ];
    const changeActive = (key: string) => {
        setActiveTab(key);
    };
    return (
        <div>
            <HeaderContent items={items} title='Учет кормления' onChange={changeActive} activeKey={activeTab} />
            {activeTab === '1' && <MainWrapper />}
            {activeTab === '2' && <Diets />}
            {activeTab === '3' && <Components />}
            {activeTab === '4' && <FeedingAnalysisPage />}
        </div>
    );
};
