import { useEffect, useState } from 'react';
import { useWindowSize } from '../../../../../../hooks/useWindowSize';
import { useGetCommonStatMonthQuery } from '../../../../services/feeding-record';
import { getCountItemsChart } from '../../../../../weight-control/functions/getCountItemsChart';
import { EmptyDataAlert } from '../../../../../../global-components/expty-data-alert/EmptyDataAlert';
import { Line } from '@ant-design/plots';
import { Button, Flex, message } from 'antd';
import { useDownload } from '../../../../../../hooks/useDownloadFile';

interface chartPoint {
    date: string;
    kg: number;
    group: string;
    color: string;
    groupId: string;
}

export const CommonConsumptionChart = () => {
    const widthWindow = useWindowSize();
    const { data = [] } = useGetCommonStatMonthQuery();
    const [countItem, setCountItem] = useState(getCountItemsChart('', widthWindow));
    const { downloadFile, isLoading } = useDownload();
    const [messageApi, contextHolder] = message.useMessage();

    useEffect(() => {
        setCountItem(getCountItemsChart('', widthWindow));
    }, [widthWindow]);

    if (!data || data.length === 0) {
        return (
            <div className='form-title'>
                <EmptyDataAlert />
            </div>
        );
    }

    const handleDownload = async () => {
        const success = await downloadFile('feeding/analysis/export');

        if (!success) {
            messageApi.open({
                type: 'error',
                content: 'Не удалось скачать файл',
            });
        }
    };

    const generateRandomColor = () => {
        const letters = '0123456789ABCDEF';
        let color = '#';
        for (let i = 0; i < 6; i++) {
            color += letters[Math.floor(Math.random() * 16)];
        }
        return color;
    };

    const uniqueGroups = [...new Set(data.map((item) => item.groupName))];

    const colorPalette = uniqueGroups.reduce((acc, group) => {
        acc[group] = generateRandomColor();
        return acc;
    }, {} as Record<string, string>);

    const transformData = (rawData: typeof data) => {
        return rawData.flatMap((group) => {
            if (!group.events || group.events.length === 0) return [];

            return group.events.map((event) => ({
                date: event.eventDate.split('T')[0],
                kg: event.totalFactKg,
                group: group.groupName,
                color: colorPalette[group.groupName],
                groupId: group.groupId,
            }));
        });
    };

    const chartData = transformData(data);

    const config = {
        data: chartData,
        xField: 'date',
        yField: 'kg',
        seriesField: 'group',
        colorField: 'group',
        lineStyle: {
            lineWidth: 3,
        },
        point: {
            size: 5,
            shape: 'circle',
            style: {
                fill: ({ group }: chartPoint) => chartData.find((d) => d.group === group)?.color || '#1890FF',
                stroke: '#fff',
                lineWidth: 2,
            },
        },
        tooltip: {
            title: (d: chartPoint) => d.date,
            items: [
                (d: chartPoint) => ({
                    color: '#ff4218',
                    name: d.group,
                    value: `${d.kg} кг`,
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
                text: 'Количество кг корма',
                style: {
                    fontSize: 14,
                },
            },
            label: {
                formatter: (val: string) => `${val} кг`,
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
                <h2>Анализ кормления за 30 дней(все группы)</h2>
                <Button type='primary' onClick={handleDownload} loading={isLoading}>
                    Скачать данные
                </Button>
            </Flex>
            <Line {...config} className='bottom-margin-xl' {...config} style={{ overflowX: 'auto' }} />
        </div>
    );
};
