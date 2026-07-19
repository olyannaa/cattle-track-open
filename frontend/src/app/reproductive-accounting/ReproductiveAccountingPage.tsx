import { Tabs, TabsProps } from 'antd';
import { useState } from 'react';
import { InseminationForm } from './components/insemination-form/InseminationForm';
import { PregnancyRateForm } from './components/pregnancy-rate-form/PregnancyRateForm';
import { CalvingForm } from './components/calving-form/CalvingForm';

export const ReproductiveAccountingPage = () => {
    const [activeTab, setActiveTab] = useState('1');
    const items: TabsProps['items'] = [
        {
            key: '1',
            label: 'Осеменение',
        },
        {
            key: '2',
            label: 'Стельность',
        },
        {
            key: '3',
            label: 'Отёлы',
        },
    ];

    const onChange = (key: string) => {
        setActiveTab(key);
    };

    return (
        <div>
            <div className='header-container'>
                <h1 className='header-title'>Репродуктивный учёт</h1>
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
            {activeTab === '1' && <InseminationForm />}
            {activeTab === '2' && <PregnancyRateForm />}
            {activeTab === '3' && <CalvingForm />}
        </div>
    );
};
