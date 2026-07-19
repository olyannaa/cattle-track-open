import { Tabs, TabsProps } from 'antd';
import { ImportSelexForm } from './components/forms/import-selex-form/ImportSelexForm';
import { ManualRegistrationForm } from './components/forms/manual-registration-form/ManualRegistrationForm';
import { useState } from 'react';
import styles from './RegisterAnimalPage.module.css';

export const RegisterAnimalPage = () => {
    const [activeTab, setActiveTab] = useState('1');
    const items: TabsProps['items'] = [
        {
            key: '1',
            label: 'Ручная регистрация',
        },
        {
            key: '2',
            label: 'Импорт из файла',
        },
    ];
    const onChange = (key: string) => {
        setActiveTab(key);
    };

    return (
        <div>
            <div className={styles['register-animals__header']}>
                <h1>Регистрация нового животного</h1>
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
            <div className={styles['register-animals__content']}>
                {activeTab === '1' && <ManualRegistrationForm />}
                {activeTab === '2' && <ImportSelexForm />}
            </div>
        </div>
    );
};
