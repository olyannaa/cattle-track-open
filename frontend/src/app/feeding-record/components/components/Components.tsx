/* eslint-disable @typescript-eslint/no-unused-vars */
import { useCallback, useEffect, useMemo, useState } from 'react';
import { Table, InputNumber, Input, Button, Space, Popconfirm, message, Form, Flex } from 'antd';
import { EditOutlined, DeleteOutlined, SaveOutlined, CloseOutlined, PlusOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import {
    useCreateComponentMutation,
    useDeleteComponentMutation,
    useEditComponentMutation,
    useGetComponentsQuery,
} from '../../services/feeding-record';
import { ComponentDietItem, ComponentDietType } from '../../data/component';
import { floatFields, nameNutrientsColumn } from './table/data/const/nameNutrientsColumn';
import { ComponentItem, defaultComponent } from './table/data/const/defaultNewComponent';
import { ErrorHandlerMessage } from '../../../../utils/errorHandlerMessage';
import Search from 'antd/es/transfer/search';

export const Components = () => {
    const [data, setData] = useState<ComponentDietItem[]>([]);
    const [filteredData, setFilteredData] = useState<ComponentDietItem[]>([]);
    const [editingId, setEditingId] = useState<string | null>(null);
    const [editingRow, setEditingRow] = useState<Partial<ComponentItem>>({});

    const { data: nutrients, refetch, isLoading } = useGetComponentsQuery();
    const [createComponent] = useCreateComponentMutation();
    const [deleteComponent, { isLoading: isDeleting }] = useDeleteComponentMutation();
    const [editComponent, { isLoading: isEditing }] = useEditComponentMutation();
    const [messageApi, contextHolder] = message.useMessage();
    const [form] = Form.useForm();

    useEffect(() => {
        if (nutrients) {
            setData(nutrients);
            setFilteredData(nutrients);
        }
    }, [nutrients]);

    useEffect(() => {
        refetch();
    }, []);

    const startEdit = (record: ComponentItem) => {
        form.setFieldsValue({ name: record.name });
        setEditingId(record.id);
        setEditingRow({ ...record });
    };

    const cancelEdit = () => {
        if (editingRow.isNew && editingRow.name) {
            setFilteredData((prev) => prev.filter((i) => i.id !== editingRow.id));
        } else if (!editingRow.name?.trim()) {
            setFilteredData((prev) => prev.filter((i) => i.id !== editingRow.id));
        }
        setEditingId(null);
        setEditingRow({});
        form.resetFields();
    };

    const saveEdit = async () => {
        await form.validateFields();
        if (form.getFieldError('name').length !== 0) {
            return;
        }
        setFilteredData((prev) =>
            prev.map((row) => (row.id === editingId ? { ...(editingRow as ComponentItem), isNew: false } : row))
        );
        try {
            if (!editingRow.isNew) {
                await editComponent(editingRow as ComponentDietItem).unwrap();
                refetch();
            } else {
                const { id, isNew, ...payload } = editingRow;
                await createComponent(payload as ComponentDietType).unwrap();
                refetch();
            }
            setEditingId(null);
            setEditingRow({});
            form.resetFields();
        } catch (err) {
            messageApi.open({
                type: 'error',
                content: ErrorHandlerMessage(err),
            });
        }
    };

    const handleInputChange = useCallback((key: keyof ComponentItem, value: unknown) => {
        setEditingRow((prev) => ({ ...prev, [key]: value }));
    }, []);

    const deleteRow = async (id: string) => {
        try {
            await deleteComponent(id).unwrap();
            refetch();
        } catch (err) {
            messageApi.open({
                type: 'error',
                content: ErrorHandlerMessage(err),
            });
        }
    };

    const addComponent = () => {
        const newItem = defaultComponent();
        setData((prev) => [...prev, newItem]);
        setFilteredData((prev) => [...prev, newItem]);
        setEditingId(newItem.id);
        setEditingRow(newItem);
    };

    const searchComponent = (e: string) => {
        const value = e.toLowerCase();
        setFilteredData(
            data.filter((item) => Object.values(item).some((val) => val?.toString().toLowerCase().includes(value)))
        );
    };

    const columns: ColumnsType<ComponentItem> = useMemo(
        () => [
            {
                title: 'Название',
                dataIndex: 'name',
                render: (_text, record) =>
                    editingId === record.id ? (
                        <Form.Item
                            name='name'
                            rules={[{ required: true, message: 'Название обязательно' }]}
                            style={{ margin: 0 }}
                        >
                            <Input
                                defaultValue={editingRow.name ?? ''}
                                onBlur={(e) => handleInputChange('name', e.currentTarget.value)}
                                placeholder='Введите название'
                            />
                        </Form.Item>
                    ) : (
                        record.name
                    ),
            },
            ...(Object.keys(nameNutrientsColumn) as (keyof ComponentItem)[]).map((key) => ({
                title: nameNutrientsColumn[key].short,
                dataIndex: key,
                align: 'center' as const,
                render: (_: unknown, record: ComponentItem) =>
                    editingId === record.id ? (
                        <InputNumber
                            value={editingRow[key] as number}
                            min={0}
                            step={floatFields.has(key) ? 0.01 : 1}
                            type='number'
                            onChange={(value) => {
                                handleInputChange(key, value ?? 0);
                            }}
                        />
                    ) : (
                        record[key]
                    ),
            })),
            {
                title: 'Действия',
                fixed: 'right',
                width: 130,
                render: (_: unknown, record: ComponentItem) =>
                    editingId === record.id ? (
                        <Space>
                            <Button icon={<SaveOutlined />} onClick={saveEdit} loading={isEditing} />
                            <Button icon={<CloseOutlined />} onClick={cancelEdit} danger />
                        </Space>
                    ) : (
                        <Space>
                            <Button icon={<EditOutlined />} onClick={() => startEdit(record)} loading={isEditing} />
                            <Popconfirm
                                title='Удалить компонент?'
                                onConfirm={() => deleteRow(record.id)}
                                okText='Да'
                                cancelText='Нет'
                            >
                                <Button
                                    icon={<DeleteOutlined />}
                                    danger
                                    disabled={record.inRation}
                                    loading={isDeleting}
                                />
                            </Popconfirm>
                        </Space>
                    ),
            },
        ],
        [editingId, editingRow]
    );

    return (
        <div className='content-container content_without-max-width'>
            {contextHolder}
            <Flex className='content-align-center bottom-margin-xl'>
                <h2>Компоненты</h2>
                <Button type='primary' icon={<PlusOutlined />} onClick={addComponent}>
                    Добавить компонент
                </Button>
            </Flex>

            <div className='form-input_default'>
                <Search
                    placeholder='Поиск по компонентам...'
                    onChange={(e) => searchComponent(e.currentTarget.value)}
                />
            </div>
            <Form form={form}>
                <Table
                    columns={columns}
                    dataSource={filteredData}
                    rowKey='id'
                    scroll={{ x: 'max-content' }}
                    pagination={false}
                    loading={isLoading}
                    style={{ width: '100%', margin: '24px 0' }}
                />
            </Form>
        </div>
    );
};
