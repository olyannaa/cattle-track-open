import { Flex } from 'antd';
import { HeaderContent } from '../../global-components/header-content/HeaderContent';
import { TabsContent } from './components/TabsContent';
import { History } from '../../global-components/daily-actions/history/History';
import { items } from './data/const/tabs';
import { useAppDispatch, useAppSelector } from '../../app-service/hooks';
import {
    changeKeyTab,
    reset,
    resetFiltersAnimals,
    selectKeyTab,
} from '../../app-service/slices/animalsDailyActionsSlice';
import { useEffect } from 'react';
import { Drugs } from './components/drugs/Drugs';

export const VeterinaryActivitiesPage = () => {
    const activeTab = useAppSelector(selectKeyTab);
    const dispatch = useAppDispatch();
    const changeTab = (value: string) => {
        dispatch(changeKeyTab(value));
        dispatch(resetFiltersAnimals());
    };

    useEffect(() => {
        return () => {
            dispatch(reset());
        };
    }, []);

    return (
        <Flex vertical gap={16} style={{ maxWidth: '1200px' }}>
            <HeaderContent
                title='Ветеринарные мероприятия'
                items={items}
                onChange={changeTab}
                activeKey={activeTab}
            />
            <Flex
                style={{
                    padding: '24px 20px 15px',
                    background: 'var(--global-bg)',
                    borderRadius: '8px',
                }}
            >
                {activeTab === '1' && <TabsContent />}
                {activeTab === '2' && <TabsContent />}
                {activeTab === '3' && <Drugs />}
            </Flex>
            {activeTab !== '3' && <History isVeterinary />}
        </Flex>
    );
};
