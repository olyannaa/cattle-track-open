import { useEffect, useState } from 'react';
import { useWindowSize } from '../../../../../../hooks/useWindowSize';
import { useGetRationCostMonthQuery } from '../../../../services/feeding-record';
import { getCountItemsChart } from '../../../../../weight-control/functions/getCountItemsChart';
import { EmptyDataAlert } from '../../../../../../global-components/expty-data-alert/EmptyDataAlert';
import { Line } from '@ant-design/plots';
import { chartPoint } from '../common-cost-chart/CommonCostChart';
import { useDownload } from '../../../../../../hooks/useDownloadFile';
import { Button, Flex, message } from 'antd';

export const CostChartMonth = ({ id }: { id: string }) => {
    const widthWindow = useWindowSize();
    const [countItem, setCountItem] = useState(getCountItemsChart('', widthWindow));
    const { data } = useGetRationCostMonthQuery(id);
    const { downloadFile, isLoading } = useDownload();
    const [messageApi, contextHolder] = message.useMessage();

    useEffect(() => {
        console.log(data);
    }, [data]);

    useEffect(() => {
        setCountItem(getCountItemsChart('', widthWindow));
    }, [widthWindow]);

    const handleDownload = async () => {
        const success = await downloadFile('feeding/group-ration-cost/export', { groupId: id });

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
        return rawData.records?.map((record) => {
            return {
                date: record.eventDate.split('T')[0],
                cost: record.totalRationCost || 0,
                rationName: record.rationName,
            };
        });
    };
    const chartData = transformData(data);

    const config = {
        data: chartData,
        xField: 'date',
        yField: 'cost',
        seriesField: 'rationName',
        lineStyle: {
            lineWidth: 3,
        },
        tooltip: {
            items: [
                (d: chartPoint) => ({
                    color: '#ff4218',
                    name: d.rationName,
                    value: `${d.cost} руб.`,
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
                <h2>Стоимость кормления за 30 дней</h2>
                <Button type='primary' onClick={handleDownload} loading={isLoading}>
                    Скачать данные
                </Button>
            </Flex>

            <Line {...config} className='bottom-margin-xl' {...config} style={{ overflowX: 'auto' }} />
        </div>
    );
};
