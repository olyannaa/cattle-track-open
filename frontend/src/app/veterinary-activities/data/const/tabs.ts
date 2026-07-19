import { TabsProps } from 'antd';

export const items: TabsProps['items'] = [
    {
        key: '1',
        label: 'Обработка',
    },
    {
        key: '2',
        label: 'Исследования',
    },
    {
        key: '3',
        label: 'Препараты',
    },
];

export const getNameTabs = (keyTab: string) => {
    return items?.find((item) => item.key === keyTab)?.label?.toString() || '';
};
