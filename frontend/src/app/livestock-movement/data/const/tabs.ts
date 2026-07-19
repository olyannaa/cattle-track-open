import { TabsProps } from 'antd';

export const items: TabsProps['items'] = [
    {
        key: '1',
        label: 'Перевод',
    },
    {
        key: '5',
        label: 'Выбытие',
    },
    {
        key: '6',
        label: 'Присвоение номеров',
    },
    {
        key: '7',
        label: 'Изменение половозрастной группы',
    },
];

export const getNameTabs = (keyTab: string) => {
    return items?.find((item) => item.key === keyTab)?.label?.toString() || '';
};
