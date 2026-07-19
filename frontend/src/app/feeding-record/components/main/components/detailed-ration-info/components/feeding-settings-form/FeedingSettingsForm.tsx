import { useState, useEffect } from 'react';
import { Table, InputNumber, Button, Select, Form, Typography, message, Flex } from 'antd';
import { IGroupRationInfo } from '../../../../../../data/group-ratio-info';
import { feedingCountOptions } from '../../data/const/feeding-count';
import { useAssignRationMutation } from '../../../../../../services/feeding-record';

const { Text } = Typography;
type FeedingTime = 'Утро' | 'День' | 'Вечер';

interface TableRow {
    key: string;
    time: FeedingTime;
    percentage: number;
}

const FeedingSettingsForm = ({
    groupInfo,
    disableBtn,
    setRefresh,
}: {
    groupInfo: IGroupRationInfo;
    disableBtn: boolean;
    setRefresh: (val: boolean) => void;
}) => {
    const [form] = Form.useForm();
    const [feedingsCount, setFeedingsCount] = useState<number>(3);
    const [feedingTimes, setFeedingTimes] = useState<string[]>(['Утро', 'День', 'Вечер']);
    const [totalPercentage, setTotalPercentage] = useState<number>(100);
    const [editGroupRation, { isLoading }] = useAssignRationMutation();
    const feedingTimeMap = {
        Утро: 'morning',
        День: 'day',
        Вечер: 'evening',
    } as const;

    useEffect(() => {
        if (groupInfo) {
            const count = groupInfo.dayFeeding !== null ? 3 : 2;
            setFeedingsCount(count);
            setFeedingTimes(count === 3 ? ['Утро', 'День', 'Вечер'] : ['Утро', 'Вечер']);

            form.setFieldsValue({
                morning: groupInfo.morningFeeding * 100 || 0,
                day: groupInfo.dayFeeding * 100 || 0,
                evening: groupInfo.nightFeeding * 100 || 0,
            });
            calculateTotal();
        }
    }, [groupInfo, form]);

    const handleFeedingsCountChange = (value: number) => {
        setFeedingsCount(value);
        if (value === 2) {
            setFeedingTimes(['Утро', 'Вечер']);
            form.setFieldsValue({
                morning: Math.round((form.getFieldValue('morning') + (form.getFieldValue('day') || 0)) / 2),
                evening: Math.round((form.getFieldValue('evening') + (form.getFieldValue('day') || 0)) / 2),
                day: undefined,
            });
        } else {
            setFeedingTimes(['Утро', 'День', 'Вечер']);
            form.setFieldsValue({
                morning: Math.round((form.getFieldValue('morning') || 0) * 1.5),
                day: Math.round((form.getFieldValue('day') || 0) * 1.5),
                evening: Math.round((form.getFieldValue('evening') || 0) * 1.5),
            });
        }
        calculateTotal();
    };

    const calculateTotal = () => {
        const values = form.getFieldsValue();
        setTotalPercentage(
            feedingTimes.reduce(
                (sum, time) => sum + (values[feedingTimeMap[time as keyof typeof feedingTimeMap]] || 0),
                0
            )
        );
    };

    const handleSubmit = async () => {
        try {
            const values = await form.validateFields();

            await editGroupRation({
                groupId: groupInfo.groupId,
                rationId: groupInfo.rationId,
                morningFeeding: values.morning || 0,
                dayFeeding: values.day || 0,
                nightFeeding: values.evening || 0,
            }).unwrap();

            setRefresh(true);

            message.success('Настройки сохранены успешно');
        } catch (err) {
            message.error('Ошибка при сохранении настроек');
            console.error(err);
        }
    };

    const columns = [
        {
            title: 'Время кормления',
            dataIndex: 'time',
            key: 'time',
        },
        {
            title: 'Процент (%)',
            dataIndex: 'percentage',
            key: 'percentage',
            render: (_: string, record: { time: keyof typeof feedingTimeMap }) => (
                <Form.Item
                    name={feedingTimeMap[record.time]}
                    rules={[
                        { required: true, message: 'Введите процент' },
                        { type: 'number', min: 1, max: 100, message: 'Допустимы значения от 1 до 100' },
                    ]}
                >
                    <InputNumber
                        min={1}
                        max={100}
                        formatter={(value?: string | number) => `${value}%`}
                        parser={(value?: string) => {
                            const num = parseInt(value?.replace('%', '') || '0', 10);
                            return Math.min(100, Math.max(1, isNaN(num) ? 1 : num));
                        }}
                        onChange={calculateTotal}
                    />
                </Form.Item>
            ),
        },
    ];

    const tableData: TableRow[] = (feedingTimes as FeedingTime[]).map((time) => ({
        key: time,
        time,
        percentage: form.getFieldValue(feedingTimeMap[time]) || 0,
    }));

    return (
        <div>
            <h3>Настройки кормления</h3>
            <Flex style={{ alignItems: 'center' }}>
                <p>Количество кормлений в день: </p>
                <Select
                    value={feedingsCount}
                    onChange={handleFeedingsCountChange}
                    style={{ marginLeft: 4 }}
                    options={feedingCountOptions}
                />
            </Flex>

            <Form form={form} onFinish={handleSubmit}>
                <h3>Распределение корма:</h3>
                <Table
                    columns={columns}
                    dataSource={tableData}
                    pagination={false}
                    bordered
                    style={{ margin: '16px 0' }}
                />

                <div style={{ marginBottom: 24 }}>
                    <Text strong>Общий процент: {totalPercentage}%</Text>
                    {totalPercentage !== 100 && (
                        <Text type='danger' style={{ marginLeft: 16 }}>
                            Сумма должна быть равна 100%
                        </Text>
                    )}
                </div>

                <Button
                    type='primary'
                    htmlType='submit'
                    disabled={totalPercentage !== 100 || disableBtn}
                    loading={isLoading}
                >
                    Сохранить настройки
                </Button>
            </Form>
        </div>
    );
};

export default FeedingSettingsForm;
