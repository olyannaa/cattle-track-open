import { ComponentItem } from './defaultNewComponent';

export const nameNutrientsColumn: { [key: string]: { short: string; full: string } } = {
    sv: { short: 'СВ', full: 'Сухое вещество' },
    sp: { short: 'СП', full: 'Сырой протеин' },
    cep: { short: 'ЧЭП', full: 'Чистая энергия для прироста' },
    ndk: { short: 'НДК', full: 'Нейтрально-детергентная клетчатка' },
    cost: { short: 'Цена (₽/кг)', full: 'Цена (₽/кг)' },
};

export const floatFields = new Set<keyof ComponentItem>(['cep', 'cost']);
