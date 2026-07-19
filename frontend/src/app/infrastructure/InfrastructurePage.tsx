import { Tabs, TabsProps } from 'antd';
import { useState } from 'react';
import { GroupsContainer } from './components/groups/GroupsContainer';
import { IdentificationFields } from './components/identification-fields/IdentificationFields';
import { TypesGroups } from './components/types-groups/TypesGroups';
import { Settings } from './components/settings/Settings';

export const InfrastructurePage = () => {
    const [activeTab, setActiveTab] = useState('1');
    const items: TabsProps['items'] = [
        {
            key: '1',
            label: 'Группы',
        },
        {
            key: '2',
            label: 'Типы групп',
        },
        {
            key: '3',
            label: 'Поля идентификации',
        },
        {
            key: '4',
            label: 'Настройки',
        },
    ];
    const onChange = (key: string) => {
        setActiveTab(key);
    };
    return (
        <div>
            <div className='header-container'>
                <h1 className='header-title'>Инфраструктура</h1>
                <Tabs
                    style={{
                        fontWeight: 400,
                        lineHeight: '24px',
                    }}
                    defaultActiveKey='1'
                    items={items}
                    onChange={onChange}
                />
            </div>
            {activeTab === '1' && <GroupsContainer />}
            {activeTab === '2' && <TypesGroups />}
            {activeTab === '3' && <IdentificationFields />}
            {activeTab === '4' && <Settings />}
        </div>
    );
};
