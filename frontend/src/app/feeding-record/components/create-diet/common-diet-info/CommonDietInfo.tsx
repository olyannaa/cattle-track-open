import { Form } from 'antd';
import { useWatch } from 'antd/es/form/Form';
import { ComponentDietItem } from '../../../data/component';
import styles from './CommonDietInfo.module.css';

export const CommonDietInfo = ({ total, components }: { total: number; components: ComponentDietItem[] }) => {
    const form = Form.useFormInstance();
    const watchedComponents = useWatch('components', form);
    const nutrientKeys = {
        sv: 'СВ',
        cep: 'ЧЭП',
        sp: 'СП',
        ndk: 'НДК',
    };

    const nutrientSums: Record<string, number> = {};
    let totalKg: number = 0;

    if (Array.isArray(watchedComponents) && Array.isArray(components)) {
        watchedComponents.forEach((row) => {
            const base = components.find((c) => c.id === row.componentId);
            if (!base || !row.kg) return;
            for (const key in nutrientKeys) {
                const nutrientValue = base[key as keyof typeof base];
                if (typeof nutrientValue === 'number') {
                    nutrientSums[key] = (nutrientSums[key] || 0) + nutrientValue * row.kg;
                }
            }
        });
        totalKg = watchedComponents.reduce((sum, item) => sum + (item.kg || 0), 0);
    }

    const format = (key: keyof typeof nutrientKeys) => nutrientSums[key]?.toFixed(2) ?? '—';

    return (
        <div className={`${styles['nutrients-container']}`}>
            <div className={`${styles['nutrients__item']}`}>
                <h3>Основная информация:</h3>
                <div className={`${styles['nutrients__info']}`}>
                    <p>Количество кг вещества:</p>
                    <p>{totalKg} кг</p>
                </div>
                <div className={`${styles['nutrients__info']}`}>
                    <p>Общая стоимость:</p>
                    <p>{total.toFixed(2)} ₽</p>
                </div>
            </div>
            <div className={`${styles['nutrients__item']}`}>
                <h3>Итоговые нутриенты:</h3>
                <div className={`${styles['nutrients__info']}`}>
                    СВ: <b>{format('sv')} кг</b>
                </div>
                <div className={`${styles['nutrients__info']}`}>
                    СП: <b>{format('sp')} кг</b>
                </div>
                <div className={`${styles['nutrients__info']}`}>
                    ЧЭП: <b>{format('cep')} МДж</b>
                </div>
                <div className={`${styles['nutrients__info']}`}>
                    <p>HДК:</p> <b>{format('ndk')} МДж</b>
                </div>
            </div>
        </div>
    );
};
