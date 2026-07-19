import { useEffect, useMemo, useState } from 'react';
import { Table, DatePicker, Button, InputNumber, Form, message, Flex } from 'antd';
import dayjs from 'dayjs';
import type { ColumnsType } from 'antd/es/table';
import { useLazyGetRationPlanQuery, useSaveRecordFeedingMutation } from '../../../../services/feeding-record';
import { correspondenceTimeOfDay, TableRow, transformRationPlan } from './transformFeedingData';
import { FeedingTime, PlanItem } from '../../../../data/feeding-plan-row';
import { RequestRecordFeedingPlan } from '../../../../data/request-record-feeding-plan';
import { useDownload } from '../../../../../../hooks/useDownloadFile';

const disabledDate = (current: dayjs.Dayjs) => current && current > dayjs().endOf('day');

const FeedingPlanTable = ({ refresh, setRefresh }: { refresh: boolean; setRefresh: (val: boolean) => void }) => {
    const [form] = Form.useForm();
    const [getPlan, { isLoading }] = useLazyGetRationPlanQuery();
    const [plan, setPlan] = useState<PlanItem[]>([]);
    const [selectedDate, setSelectedDate] = useState(dayjs());
    const [saveChanges, { isLoading: loadingSave }] = useSaveRecordFeedingMutation();
    const { downloadFile } = useDownload();
    const [messageApi, contextHolder] = message.useMessage();

    const fetchPlan = async (date: dayjs.Dayjs) => {
        try {
            const response = await getPlan(date.format('DD.MM.YYYY')).unwrap();
            setPlan(response || []);
        } catch (error) {
            messageApi.open({
                type: 'error',
                content: 'Не удалось загрузить план кормления',
            });
            setPlan([]);
            console.log(error);
        }
    };

    const savePlan = async () => {
        const success = await downloadFile('feeding/main/group-feeding-stats', {
            date: selectedDate.format('DD.MM.YYYY'),
        });

        if (!success) {
            messageApi.open({
                type: 'error',
                content: 'Не удалось скачать файл',
            });
        }
    };

    useEffect(() => {
        fetchPlan(selectedDate);
    }, [selectedDate]);

    useEffect(() => {
        if (refresh) {
            fetchPlan(selectedDate);
            setRefresh(false);
        }
    }, [refresh]);

    const { tableData, timeRowSpans } = useMemo(() => {
        const transformed = transformRationPlan(plan);
        const rowSpans = transformed.reduce((acc, row) => {
            acc[row.time] = (acc[row.time] || 0) + 1;
            return acc;
        }, {} as Record<string, number>);
        return { tableData: transformed, timeRowSpans: rowSpans };
    }, [plan]);

    useEffect(() => {
        if (tableData.length > 0) {
            form.setFieldsValue({
                rows: tableData.map((row) => ({
                    factKg: row.factKg,
                    mark: row.mark,
                    feedingCoefficient: row.feedingCoefficient,
                })),
            });
        }
    }, [tableData, form]);

    const handleSave = async () => {
        try {
            const values = await form.validateFields();
            const changedRecords = tableData
                .map((row, index) => {
                    const formValues = values.rows?.[index];
                    if (!formValues) return null;

                    const hasChanges = Object.keys(formValues).some(
                        (key) => formValues[key] !== row[key as keyof TableRow]
                    );

                    return hasChanges
                        ? {
                              eventDate: selectedDate.format('YYYY-MM-DD'),
                              groupId: row.groupId,
                              animalCount: row.animalCount,
                              groupRationId: row.groupRationId,
                              totalKg: row.realKg,
                              totalKgForGroup: row.realKgForGroup,
                              feedingTime: correspondenceTimeOfDay[row.time],
                              feedingMark: row.coefficient,
                              feedingCoefficient: formValues.feedingCoefficient ? formValues.feedingCoefficient / 100  :  0,
                              factKg: formValues.factKg ?? 0,
                              mark: formValues.mark ?? 0,
                          }
                        : null;
                })
                .filter(Boolean);

            if (changedRecords.length === 0) {
                message.warning('Нет изменений для сохранения');
                return;
            }

            await saveChanges(changedRecords as RequestRecordFeedingPlan[]).unwrap();
            message.success('Изменения сохранены');
            fetchPlan(selectedDate);
        } catch (error) {
            console.error('Ошибка:', error);
            message.error('Ошибка при сохранении');
        }
    };

    const columns: ColumnsType<TableRow> = [
        {
            title: 'Время',
            dataIndex: 'time',
            width: 80,
            render: (value: FeedingTime, _, index) => {
                const isFirstRow = index === 0 || tableData[index - 1].time !== value;
                return {
                    children: <b>{value}</b>,
                    props: {
                        rowSpan: isFirstRow ? timeRowSpans[value] : 0,
                    },
                };
            },
        },
        {
            title: 'Группа',
            dataIndex: 'groupName',
            width: 140,
            ellipsis: true,
        },
        {
            title: 'Голов',
            dataIndex: 'animalCount',
            width: 80,
        },
        {
            title: 'Рацион',
            dataIndex: 'groupRationName',
            width: 200,
            ellipsis: true,
            render: (value) => <span style={{ color: '#fa541c', fontWeight: 500 }}>{value || '-'}</span>,
        },
        {
            title: 'Коэфф.',
            dataIndex: 'coefficient',
            width: 80,
            render: (value) => value.toFixed(2),
        },
        {
            title: 'РВ/голова (кг)',
            dataIndex: 'totalKg',
            width: 80,
            render: (value) => value?.toFixed(2),
        },
        {
            title: 'РВ/группа (кг)',
            dataIndex: 'totalKgForGroup',
            width: 80,
            render: (value) => value?.toFixed(2),
        },
        {
            title: 'Факт (кг)',
            dataIndex: 'factKg',
            width: 120,
            render: (_, record, index) => (
                <Form.Item name={['rows', index, 'factKg']} initialValue={record.factKg}>
                    <InputNumber min={0} step={0.1} style={{ width: '100%' }} placeholder='Вес' />
                </Form.Item>
            ),
        },
        {
            title: 'Баллы',
            dataIndex: 'mark',
            width: 100,
            render: (_, record, index) => (
                <Form.Item name={['rows', index, 'mark']} initialValue={record.mark}>
                    <InputNumber min={1} max={5} style={{ width: '100%' }} placeholder='1-5' />
                </Form.Item>
            ),
        },
        {
            title: 'Коэфф. поедания (%)',
            dataIndex: 'feedingCoefficient',
            width: 150,
            render: (_, record, index) => (
                <Form.Item name={['rows', index, 'feedingCoefficient']} initialValue={record.feedingCoefficient}>
                    <InputNumber
                        min={1}
                        max={100}
                        style={{ width: '100%' }}
                        placeholder='1-100'
                        formatter={(value) => `${value}%`}
                        parser={(value?: string) => {
                            const num = parseInt(value?.replace('%', '') || '0', 10);
                            return Math.min(100, Math.max(1, isNaN(num) ? 1 : num));
                        }}
                    />
                </Form.Item>
            ),
        },
    ];

    return (
        <div>
            {contextHolder}
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap' }}>
                <h2>План кормления на {selectedDate.format('DD.MM.YYYY')}</h2>
                <Flex>
                    <DatePicker
                        value={selectedDate}
                        onChange={(date) => date && setSelectedDate(date)}
                        disabledDate={disabledDate}
                        format='DD.MM.YYYY'
                        style={{ width: 150, marginRight: 8 }}
                    />
                    <Button type='primary' onClick={() => savePlan()}>
                        Скачать план
                    </Button>
                </Flex>
            </div>

            <Form form={form} component={false}>
                <Table
                    columns={columns}
                    dataSource={tableData}
                    pagination={false}
                    rowKey={(record) => `${record.time}-${record.groupId}`}
                    bordered
                    scroll={{ x: 'max-content' }}
                    style={{
                        width: '100%',
                        margin: '24px 0',
                        maxWidth: '98vw',
                    }}
                    loading={isLoading}
                    size='middle'
                />
            </Form>

            <div style={{ textAlign: 'right', marginTop: 16 }}>
                <Button
                    type='primary'
                    onClick={handleSave}
                    loading={loadingSave}
                    disabled={isLoading || loadingSave}
                    size='large'
                >
                    Сохранить изменения
                </Button>
            </div>
        </div>
    );
};

export default FeedingPlanTable;
