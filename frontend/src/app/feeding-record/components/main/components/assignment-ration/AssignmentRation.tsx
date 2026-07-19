import { useEffect, useMemo, useState } from 'react';
import { Table, Button, message, Flex } from 'antd';
import {
    useAssignRationMutation,
    useGetGroupStatQuery,
    useGetRationsWithComponentsQuery,
} from '../../../../services/feeding-record';
import { EditOutlined } from '@ant-design/icons';
import { SelectDataType } from '../../../../../../utils/selectDataType';
import { IGroupRationInfo } from '../../../../data/group-ratio-info';
import { DetailedRationInfo } from './../detailed-ration-info/DetailedRationInfo';
import Search from 'antd/es/transfer/search';
import { ErrorHandlerMessage } from '../../../../../../utils/errorHandlerMessage';
import styles from './AssignmentRation.module.css';

export const AssignmentRation = ({ refreshPlan }: { refreshPlan: (val: boolean) => void }) => {
    const { data: allRations, refetch: refetchRations, isLoading: loadDiets } = useGetRationsWithComponentsQuery();
    const { data: groupData = [], refetch, isLoading: loadStat } = useGetGroupStatQuery();
    const [editGroupRation] = useAssignRationMutation();
    const [search, setSearch] = useState('');
    const [groupRationData, setGroupRationData] = useState<IGroupRationInfo[]>([]);
    const [rations, setRations] = useState<SelectDataType[]>([]);
    const [messageApi, contextHolder] = message.useMessage();
    const [loadingGroupId, setLoadingGroupId] = useState<string | null>(null);
    const [isModalOpen, setIsModalOpen] = useState<boolean>(false);
    const [selectedRecord, setSelectedRecord] = useState<IGroupRationInfo | null>(null);

    useEffect(() => {
        refetch();
        refetchRations();
    }, []);

    useEffect(() => {
        if (groupData.length > 0) {
            setGroupRationData(groupData);
            if (selectedRecord) {
                const updateRecord = groupData.find((group) => group.groupId === selectedRecord.groupId);
                setSelectedRecord(updateRecord ?? null);
            }
        }
    }, [groupData]);

    useEffect(() => {
        const mapData = allRations?.map((ration) => ({
            value: ration.rationId,
            label: ration.rationName,
        }));
        setRations(mapData ?? []);
    }, [allRations]);

    const filteredGroups = useMemo(
        () => groupRationData.filter((g) => g.groupName.toLowerCase().includes(search.toLowerCase())),
        [search, groupRationData]
    );

    const handleRationChange = async (groupId: string, newRationId: string) => {
        setLoadingGroupId(groupId);
        try {
            const newRation = allRations?.find((r) => r.rationId === newRationId);
            if (!newRation || !selectedRecord) return;

            await editGroupRation({
                groupId,
                rationId: newRationId,
                morningFeeding: selectedRecord.morningFeeding,
                dayFeeding: selectedRecord.dayFeeding,
                nightFeeding: selectedRecord.nightFeeding,
            }).unwrap();
            refreshPlan(true);

            messageApi.success('Рацион успешно изменен');
        } catch (err) {
            messageApi.error(ErrorHandlerMessage(err));
            console.error(err);
        } finally {
            setLoadingGroupId(null);
        }
    };

    const columns = [
        {
            title: 'Группы',
            dataIndex: 'groupName',
            key: 'groupName',
        },
        {
            title: 'Рацион',
            key: 'ration',
            render: (_: unknown, record: IGroupRationInfo) => (
                <Flex className={styles['assignment-ration__name-container']}>
                    <p className={styles['assignment-ration__title']}>{record.rationName ?? 'Не назначен'}</p>
                    <Button
                        type='link'
                        icon={<EditOutlined />}
                        onClick={() => {
                            setSelectedRecord(record);
                            setIsModalOpen(true);
                        }}
                    ></Button>
                </Flex>
            ),
        },
        {
            title: 'Кол-во голов',
            dataIndex: 'activeAnimalsCount',
            key: 'activeAnimalsCount',
        },
        {
            title: 'Кормление утро',
            dataIndex: 'morningFeeding',
            key: 'morningFeeding',
            render: (value: number) => `${value * 100}%`,
        },
        {
            title: 'Кормление день',
            dataIndex: 'dayFeeding',
            key: 'dayFeeding',
            render: (value: number) => `${value * 100}%`,
        },
        {
            title: 'Кормление вечер',
            dataIndex: 'nightFeeding',
            key: 'nightFeeding',
            render: (value: number) => `${value * 100}%`,
        },
        {
            title: 'Стоимость на 1 голову',
            dataIndex: 'rationCostPerHead',
            key: 'rationCostPerHead',
            render: (value: number) => `${value.toFixed(2)} ₽`,
        },
        {
            title: 'Итоговая стоимость',
            dataIndex: 'totalRationCost',
            key: 'totalRationCost',
            render: (value: number) => `${value.toFixed(2)} ₽`,
        },
    ];

    return (
        <div style={{ width: '100%' }}>
            {contextHolder}
            <div className='form-input_default'>
                <Search placeholder='Поиск по группам...' onChange={(e) => setSearch(e.target.value)} />
            </div>
            <Table
                dataSource={filteredGroups}
                columns={columns}
                rowKey='groupId'
                pagination={{
                    pageSize: 5,
                    showSizeChanger: false,
                    showTotal: (total, range) => `${range[0]}-${range[1]} из ${total} записей`,
                }}
                scroll={{ x: 'max-content' }}
                style={{ width: '100%', margin: '24px 0' }}
                loading={loadStat || loadDiets}
            />
            {selectedRecord && (
                <DetailedRationInfo
                    groupInfo={selectedRecord}
                    allRations={allRations ?? []}
                    rationOptions={rations}
                    handleRationChange={handleRationChange}
                    open={isModalOpen}
                    onCancel={() => setIsModalOpen(false)}
                    loading={loadingGroupId === selectedRecord.groupId}
                    setRefresh={refreshPlan}
                />
            )}
        </div>
    );
};
