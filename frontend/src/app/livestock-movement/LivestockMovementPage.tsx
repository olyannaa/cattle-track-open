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

export const LivestockMovementPage = () => {
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
                title='Движение поголовья'
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
                {activeTab === '5' && <TabsContent />}
                {activeTab === '6' && <TabsContent />}
                {activeTab === '7' && <TabsContent />}
            </Flex>
            <History />
        </Flex>
    );
};
