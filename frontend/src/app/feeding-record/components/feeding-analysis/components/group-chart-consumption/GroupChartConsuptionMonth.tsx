import { useEffect, useState } from 'react';
import { useLazyGetFeedingMonthChartQuery } from '../../../../services/feeding-record';
import { FeedingChart, FeedingPoint } from '../../../../data/chart-data';
import { EmptyDataAlert } from '../../../../../../global-components/expty-data-alert/EmptyDataAlert';
import { Line } from '@ant-design/plots';
import { useDownload } from '../../../../../../hooks/useDownloadFile';
import { Button, Flex, message } from 'antd';
import { getCountItemsChart } from '../../../../../weight-control/functions/getCountItemsChart';
import { useWindowSize } from '../../../../../../hooks/useWindowSize';

export interface chartPoint {
    date: string;
    kg: number;
    rationName: string;
}

export const GroupChartConsumptionMonth = ({ id }: { id: string }) => {
    const [getPointsConsumption] = useLazyGetFeedingMonthChartQuery();
    const [data, setData] = useState<FeedingChart | null>(null);
    const { downloadFile, isLoading } = useDownload();
    const [messageApi, contextHolder] = message.useMessage();

    const widthWindow = useWindowSize();
    const [countItem, setCountItem] = useState(getCountItemsChart('', widthWindow));

    useEffect(() => {
        if (id) {
            getPoints(id);
        }
    }, [id]);

    useEffect(() => {
        setCountItem(getCountItemsChart('', widthWindow));
    }, [widthWindow]);

    const getPoints = async (id: string) => {
        const res = (await getPointsConsumption(id)).data;
        if (res) {
            setData(res);
        }
    };

    const handleDownload = async () => {
        const success = await downloadFile('feeding/group-analysis/export', { groupId: id });

        if (!success) {
            messageApi.open({
                type: 'error',
                content: 'Не удалось скачать файл',
            });
        }
    };

    if (!data) {
        return <EmptyDataAlert />;
    }

    const transformData = (rawData: typeof data) => {
        return rawData.records?.map((record: FeedingPoint) => {
            return {
                date: record.eventDate.split('T')[0],
                kg: record.dailyFactKg || 0,
                rationName: record.rationName,
            };
        });
    };
    const chartData = transformData(data);

    const config = {
        data: chartData,
        xField: 'date',
        yField: 'kg',
        seriesField: 'rationName',
        lineStyle: {
            lineWidth: 3,
        },
        tooltip: {
            items: [
                (d: chartPoint) => ({
                    color: '#ff4218',
                    name: d.rationName,
                    value: `Потребление: ${d.kg} кг.`,
                }),
            ],
        },
        legend: false,
        xAxis: {
            type: 'cat',
            label: {
                autoRotate: true,
                style: {
                    fontSize: 12,
                },
            },
            title: {
                text: 'Дата',
                style: {
                    fontSize: 14,
                },
            },
        },
        yAxis: {
            title: {
                text: 'Стоимость корма (руб)',
                style: {
                    fontSize: 14,
                },
            },
            label: {
                formatter: (val: string) => `${val} руб`,
            },
        },
        smooth: true,
        interactions: [{ type: 'element-active' }],
        animation: {
            appear: {
                animation: 'path-in',
                duration: 1000,
            },
        },
        scrollbar: {
            x:
                chartData.length < countItem
                    ? false
                    : {
                          ratio: countItem / chartData.length,
                      },
        },
    };

    return (
        <div>
            {contextHolder}
            <Flex justify='space-between' className='form-title'>
                <h2>Анализ кормления за 30 дней</h2>
                <Button type='primary' onClick={handleDownload} loading={isLoading}>
                    Скачать данные
                </Button>
            </Flex>
            <Line {...config} className='bottom-margin-xl' {...config} style={{ overflowX: 'auto' }} />
        </div>
    );
};
