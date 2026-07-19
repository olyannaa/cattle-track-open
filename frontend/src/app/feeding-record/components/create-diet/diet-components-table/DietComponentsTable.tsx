import { Button, Flex, Form, InputNumber, Select, Table } from 'antd';
import { DeleteOutlined, PlusOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { useEffect, useMemo, useState } from 'react';
import { useWatch } from 'antd/es/form/Form';
import { useGetComponentsQuery } from '../../../services/feeding-record';
import { SelectDataType } from '../../../../../utils/selectDataType';
import { formatDataForSelectInput } from '../../../../../utils/data-functions/formatting-data';
import { ComponentDietItem } from '../../../data/component';
import { CommonDietInfo } from '../common-diet-info/CommonDietInfo';
import { ComponentRow } from '../../../data/component-row';

export interface Nutrients {
    [key: string]: number;
}

export const DietComponentTable = () => {
    const form = Form.useFormInstance();
    const watchedComponents = useWatch('components', form);
    const { data } = useGetComponentsQuery();
    const [dietComponents, setDietComponents] = useState<ComponentDietItem[]>([]);
    const [dietComponentsOptions, setDietComponentsOptions] = useState<SelectDataType[]>([]);
    const [rowNutrients, setRowNutrients] = useState<Record<number, Nutrients>>({});

    useEffect(() => {
        if (data) {
            setDietComponentsOptions(formatDataForSelectInput(data));
            setDietComponents(data);
        }
    }, [data]);

    const total = useMemo(() => {
        return (watchedComponents || []).reduce(
            (sum: number, row: ComponentRow) => sum + (row.kg || 0) * (row.cost || 0),
            0
        );
    }, [watchedComponents]);

    const handleComponentSelect = (index: number, componentId: string) => {
        const selected = dietComponents.find((opt) => opt.id === componentId);
        if (!selected) return;

        const components = form.getFieldValue('components') || [];
        const updatedRow: ComponentRow = {
            componentId: selected.id,
            kg: 1,
            cost: selected.cost,
        };
        const nutrients: Nutrients = {
            СВ: selected.sv,
            ЧЭП: selected.cep,
            НДК: selected.ndk,
            CП: selected.sp,
        };

        setRowNutrients((prev) => ({ ...prev, [index]: nutrients }));
        const updated = [...components];
        updated[index] = { ...updated[index], ...updatedRow };
        form.setFieldValue('components', updated);
    };

    const calculateNutrients = (base: Nutrients, count: number): Nutrients => {
        const result: Nutrients = {};
        for (const key in base) {
            result[key] = +(base[key] * count);
        }
        return result;
    };

    const addRow = () => {
        const current = form.getFieldValue('components') || [];
        form.setFieldValue('components', [...current, {}]);
    };

    const removeRow = (index: number) => {
        const current = form.getFieldValue('components') || [];
        const updated = [...current];
        updated.splice(index, 1);
        form.setFieldValue('components', updated);
    };

    return (
        <Form.List name='components'>
            {() => {
                const components: ComponentRow[] = form.getFieldValue('components') || [];

                const columns: ColumnsType<ComponentRow> = [
                    {
                        title: 'Компонент',
                        render: (_, __, index) => (
                            <Form.Item
                                name={[index, 'componentId']}
                                rules={[{ required: true, message: 'Обязательное поле' }]}
                                style={{ marginBottom: 0 }}
                            >
                                <Select
                                    placeholder='Выберите компонент'
                                    onChange={(value) => handleComponentSelect(index, value)}
                                    allowClear
                                    options={dietComponentsOptions}
                                ></Select>
                            </Form.Item>
                        ),
                        onCell: () => ({
                            style: { verticalAlign: 'middle' },
                        }),
                    },
                    {
                        title: 'Кол-во (кг)',
                        render: (_, __, index) => (
                            <Form.Item name={[index, 'kg']} rules={[{ required: true }]} initialValue={1} noStyle>
                                <InputNumber
                                    min={0}
                                    step={0.1}
                                    type='number'
                                    onChange={(value) => {
                                        const components = form.getFieldValue('components') || [];
                                        const baseComponent = dietComponents.find(
                                            (c) => c.id === components[index]?.componentId
                                        );

                                        const updated = components.map((item: ComponentRow, i: number) => {
                                            if (i !== index) return item;

                                            const baseNutrients = {
                                                СВ: baseComponent?.sv ?? 0,
                                                CП: baseComponent?.sp ?? 0,
                                                ЧЭП: baseComponent?.cep ?? 0,
                                                НДК: baseComponent?.ndk ?? 0,
                                            };
                                            const updatedNutrients = calculateNutrients(baseNutrients, value || 0);
                                            setRowNutrients((prev) => ({ ...prev, [index]: updatedNutrients }));

                                            return {
                                                ...item,
                                                kg: value,
                                            };
                                        });

                                        form.setFieldValue('components', updated);
                                    }}
                                />
                            </Form.Item>
                        ),
                    },
                    {
                        title: 'Цена за ед.',
                        render: (_, __, index) => {
                            const component = components[index] || {};
                            return (
                                <Form.Item
                                    name={[index, 'cost']}
                                    rules={[{ required: true }]}
                                    initialValue={component.cost ?? 0}
                                    noStyle
                                >
                                    <InputNumber
                                        min={0}
                                        step={0.01}
                                        onChange={(value) => {
                                            const components = form.getFieldValue('components') || [];
                                            const updated = components.map((item: ComponentRow, i: number) =>
                                                i === index ? { ...item, cost: value } : item
                                            );
                                            form.setFieldValue('components', updated);
                                        }}
                                    />
                                </Form.Item>
                            );
                        },
                    },
                    {
                        title: 'Стоимость',
                        render: (_, __, index) => {
                            const row = components[index] || {};
                            const cost = (row.kg || 0) * (row.cost || 0);
                            return <span>{cost.toFixed(2)} ₽</span>;
                        },
                    },
                    {
                        title: 'Нутриенты',
                        render: (_, __, index) => {
                            const nutrients = rowNutrients[index] || {};
                            return (
                                <div style={{ fontSize: 12 }}>
                                    {Object.entries(nutrients).map(([key, val]) => (
                                        <div key={key}>
                                            {key}: {(val ?? 0).toFixed(2)}
                                        </div>
                                    ))}
                                </div>
                            );
                        },
                    },
                    {
                        title: 'Действия',
                        render: (_, __, index) => (
                            <Button icon={<DeleteOutlined />} danger onClick={() => removeRow(index)} />
                        ),
                    },
                ];

                return (
                    <>
                        <Flex
                            style={{ justifyContent: 'space-between', width: '100%', alignItems: 'center' }}
                            wrap
                            gap='12px'
                        >
                            <h2>Компоненты рациона </h2>
                            <Button icon={<PlusOutlined />} onClick={addRow}>
                                Добавить компонент
                            </Button>
                        </Flex>
                        <Table
                            columns={columns}
                            dataSource={components}
                            pagination={false}
                            rowKey={(_, index) => String(index)}
                            locale={{ emptyText: 'Нажмите "Добавить компонент" чтобы начать создание рациона' }}
                            scroll={{ x: 'max-content' }}
                            style={{ margin: '24px 0' }}
                        />
                        {watchedComponents && <CommonDietInfo total={total} components={dietComponents} />}
                    </>
                );
            }}
        </Form.List>
    );
};
