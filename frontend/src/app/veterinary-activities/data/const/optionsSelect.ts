export const optionsVaccination = [
    {
        label: 'Вакцинация',
        value: 'Вакцинация',
    },
    {
        label: 'Дегельминтизация',
        value: 'Дегельминтизация',
    },
    {
        label: 'Обработка от эктопаразитов',
        value: 'Обработка от эктопаразитов',
    },
    {
        label: 'Другое',
        value: 'Другое',
    },
];

export const optionsDisposal = [
    {
        label: 'Вынужденная прирезка',
        value: 'Вынужденная прирезка',
    },
    {
        label: 'Забой на мясо в хозяйстве',
        value: 'Забой на мясо в хозяйстве',
    },
    {
        label: 'Падеж',
        value: 'Падеж',
    },
    {
        label: 'Продажа',
        value: 'Продажа',
    },
    {
        label: 'Прочее',
        value: 'Прочее',
    },
    {
        label: 'Забой на мясокомбинат',
        value: 'Забой на мясокомбинат',
    },
];

export const optionsResearch = [
    {
        label: 'Кровь',
        value: 'Кровь',
    },
    {
        label: 'Молоко',
        value: 'Молоко',
    },
    {
        label: 'Моча',
        value: 'Моча',
    },
    {
        label: 'Кал',
        value: 'Кал',
    },
    {
        label: 'Соскоб',
        value: 'Соскоб',
    },
    {
        label: 'Смыв',
        value: 'Смыв',
    },
    {
        label: 'Другое',
        value: 'Другое',
    },
];

export const getOptionsType = (keyTab: string) => {
    return keyTab === '8'
        ? ['Телка', 'Бычок']
        : ['Телка', 'Нетель', 'Корова', 'Бычок', 'Бык'];
};

export const optionsTypesTreatment = [
    {
        label: 'Вакцинация',
        value: 'Вакцинация',
    },
    {
        label: 'Лечение',
        value: 'Лечение',
    },
    {
        label: 'Дегельминтизация',
        value: 'Дегельминтизация',
    },
    {
        label: 'Другое',
        value: 'Другое',
    },
];
