import { TabsProps } from "antd";

export const items: TabsProps['items'] = [
    {
        key: '1',
        label: 'Перевод',
    },
    {
        key: '1',
        label: 'Обработка'
    },
    {
        key: '2',
        label: 'Исследования'
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

export const getNameTabs = (keyTab: string, isVeterinary: boolean) => {
    if (isVeterinary && keyTab === '1' ) {
        return 'Обработка';
    }
    return items?.find((item) => item.key === keyTab)?.label?.toString() || '';
};
