/* eslint-disable @typescript-eslint/no-explicit-any */
import { Button, Table, Space, Typography, message } from 'antd';
import { EditOutlined, SaveOutlined, DownOutlined, RightOutlined } from '@ant-design/icons';
import { useEffect, useState } from 'react';
import {
    useChangeRationComponentsMutation,
    useGetComponentsQuery,
    useGetGroupStatQuery,
    useGetRationsWithComponentsQuery,
} from '../../../../services/feeding-record';
import { IRation, RationComponent } from '../../../../data/ration';
import { RationComposition } from './components/RationComposition';
import { formatDataForSelectInput, SelectDataType } from '../../../../../../utils/formatting-data';
import { ComponentDietItem } from '../../../../data/component';
import Search from 'antd/es/transfer/search';
import { ErrorHandlerMessage } from '../../../../../../utils/errorHandlerMessage';

const { Text } = Typography;

export const Rations = ({
    searchValue,
    onSearchChange,
}: {
    searchValue: string;
    onSearchChange: (value: string) => void;
}) => {
    const { data: rations = [], refetch, isLoading: loadRations } = useGetRationsWithComponentsQuery();
    const { refetch: refetchGroupStat } = useGetGroupStatQuery();
    const [updateRation] = useChangeRationComponentsMutation();
    const [expandedRowKeys, setExpandedRowKeys] = useState<string[]>([]);
    const [editingId, setEditingId] = useState<string | null>(null);
    const [editingComponents, setEditingComponents] = useState<any>([]);
    const { data } = useGetComponentsQuery();
    const [dietComponentsOptions, setDietComponentsOptions] = useState<SelectDataType[]>([]);
    const [filteredData, setFilteredData] = useState<IRation[]>([]);
    const [messageApi, contextHolder] = message.useMessage();

    useEffect(() => {
        if (data) {
            setDietComponentsOptions(formatDataForSelectInput(data));
        }
    }, [data]);

    useEffect(() => {
        if (rations) {
            const filtered = rations.filter((ration: IRation) =>
                ration.rationName.toLowerCase().includes(searchValue.toLowerCase())
            );
            setFilteredData(filtered);
        }
    }, [searchValue, rations]);

    const toggleExpand = (rationId: string) => {
        setExpandedRowKeys((prev) =>
            prev.includes(rationId) ? prev.filter((id) => id !== rationId) : [...prev, rationId]
        );
    };

    const startEdit = (ration: IRation) => {
        setEditingId(ration.rationId);
        setEditingComponents([...ration.components]);
    };

    const cancelEdit = () => {
        setEditingId(null);
        setEditingComponents([]);
    };

    const saveEdit = async () => {
        try {
            if (!validateComponents()) {
                messageApi.open({
                    type: 'error',
                    content: 'Не все обязательные поля заполнены!',
                });
                return;
            }
            if (editingId) {
                const components = editingComponents.map((comp: any) => ({
                    component_id: comp.componentId,
                    count: comp.kg,
                    cost: comp.cost,
                }));
                await updateRation({
                    rationId: editingId,
                    components: components,
                }).unwrap();
                await refetch();
                refetchGroupStat();
                cancelEdit();
                messageApi.open({
                    type: 'success',
                    content: 'Рацион обновлён',
                });
            }
        } catch (err) {
            messageApi.open({
                type: 'error',
                content: ErrorHandlerMessage(err),
            });
        }
    };

    const handleComponentChange = (index: number, field: string, value: unknown) => {
        const updated = [...editingComponents];
        updated[index] = { ...updated[index], [field]: value };
        if (field === 'componentId') {
            const current = data?.find((comp: ComponentDietItem) => comp.id === value);
            updated[index] = {
                ...updated[index],
                cost: current?.cost ?? 0,
                componentName: current?.name ?? '',
                sv: current?.sv ?? 0,
                sp: current?.sp ?? 0,
                cep: current?.cep ?? 0,
                ndk: current?.ndk ?? 0,
            };
        }
        setEditingComponents(updated);
    };

    const addComponent = () => {
        setEditingComponents([
            ...editingComponents,
            {
                componentId: '',
                componentName: '',
                kg: 1,
                cost: 0,
                sv: 0,
                ndk: 0,
                sp: 0,
                cep: 0,
            },
        ]);
    };

    const removeComponent = (index: number) => {
        const updated = [...editingComponents];
        updated.splice(index, 1);
        setEditingComponents(updated);
    };

    const validateComponents = () => {
        return editingComponents.every((comp: RationComponent) => comp.componentName && comp.kg > 0);
    };

    const expandedRowRender = (ration: IRation) => {
        const components = editingId === ration.rationId ? editingComponents : ration.components;
        const isEditing = editingId === ration.rationId;
        return (
            <RationComposition
                groups={ration.groupNames}
                components={components}
                isEditing={isEditing}
                handleComponentChange={handleComponentChange}
                removeComponent={removeComponent}
                addComponent={addComponent}
                dietComponentsOptions={dietComponentsOptions}
            />
        );
    };

    const columns = [
        {
            title: 'Рацион',
            dataIndex: 'rationName',
            key: 'rationName',
            render: (text: string) => <Text strong>{text}</Text>,
        },
        {
            title: 'Общая стоимость',
            key: 'totalCost',
            render: (_: unknown, record: IRation) => <Text>{record.totalCost?.toFixed(2)} ₽</Text>,
        },
        {
            title: 'Действия',
            key: 'actions',
            render: (_: unknown, record: IRation) =>
                editingId === record.rationId ? (
                    <Space>
                        <Button type='primary' icon={<SaveOutlined />} onClick={saveEdit}>
                            Сохранить
                        </Button>
                        <Button onClick={cancelEdit}>Отмена</Button>
                    </Space>
                ) : (
                    <Button icon={<EditOutlined />} onClick={() => startEdit(record)}>
                        Редактировать
                    </Button>
                ),
        },
        {
            key: 'expand',
            width: 50,
            render: (_: unknown, record: IRation) => (
                <Button
                    type='text'
                    icon={expandedRowKeys.includes(record.rationId) ? <DownOutlined /> : <RightOutlined />}
                    onClick={() => toggleExpand(record.rationId)}
                />
            ),
        },
    ];

    return (
        <div>
            {contextHolder}
            <div style={{ marginBottom: 24 }} className='form-input_default'>
                <Search
                    placeholder='Поиск по названию рациона'
                    value={searchValue}
                    onChange={(e) => onSearchChange(e.target.value)}
                />
            </div>
            <Table
                columns={columns}
                dataSource={filteredData}
                rowKey='rationId'
                expandable={{
                    expandedRowRender,
                    expandedRowKeys,
                    onExpand: (_, record) => toggleExpand(record.rationId),
                    expandIconColumnIndex: -1,
                }}
                pagination={{
                    pageSize: 5,
                    showSizeChanger: false,
                    showTotal: (total, range) => `${range[0]}-${range[1]} из ${total} записей`,
                }}
                scroll={{ x: 'max-content' }}
                style={{ width: '100%' }}
                loading={loadRations}
            />
        </div>
    );
};
