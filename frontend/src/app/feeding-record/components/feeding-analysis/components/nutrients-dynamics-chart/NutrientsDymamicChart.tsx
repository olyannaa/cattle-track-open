/* eslint-disable @typescript-eslint/no-explicit-any */
import { useEffect, useState } from 'react';
import { useLazyGetRationNutrientsDynamicQuery } from '../../../../services/feeding-record';
import { FeedingChart, NutrientsPoint } from '../../../../data/chart-data';
import { EmptyDataAlert } from '../../../../../../global-components/expty-data-alert/EmptyDataAlert';
import { Line } from '@ant-design/plots';
import { Button, Flex, message } from 'antd';
import { useDownload } from '../../../../../../hooks/useDownloadFile';
import { useWindowSize } from '../../../../../../hooks/useWindowSize';
import { getCountItemsChart } from '../../../../../weight-control/functions/getCountItemsChart';

interface NutrientsChartData {
    date: string;
    value: number;
    category: string;
    rationName: string;
}

export const NutrientsDynamicChart = ({ id }: { id: string }) => {
    const [getPointsNutrients] = useLazyGetRationNutrientsDynamicQuery();
    const { downloadFile, isLoading } = useDownload();
    const [data, setData] = useState<FeedingChart<NutrientsPoint> | null>(null);
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
        const res = (await getPointsNutrients(id)).data;
        if (res) {
            setData(res);
        }
    };

    const handleDownload = async () => {
        const success = await downloadFile('/api/feeding/group-ration-nutrition/export', { groupId: id });

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

    const transformData = (rawData: { records: NutrientsPoint[] }): NutrientsChartData[] => {
        return rawData.records.flatMap((record) => {
            const date = record.eventDate.split('T')[0];
            return [
                {
                    date,
                    value: record.totalSv,
                    category: 'СВ (Сухое вещество)',
                    rationName: record.rationName,
                },
                {
                    date,
                    value: record.totalSp,
                    category: 'СП (Сырой протеин)',
                    rationName: record.rationName,
                },
                {
                    date,
                    value: record.totalCep,
                    category: 'ЧЭП (Чистая энергия для прироста)',
                    rationName: record.rationName,
                },
                {
                    date,
                    value: record.totalNdk,
                    category: 'НДК (Нейтрально-детергентная клетчатка)',
                    rationName: record.rationName,
                },
            ];
        });
    };

    const chartData = transformData(data);

    const config = {
        data: chartData,
        xField: 'date',
        yField: 'value',
        seriesField: 'category',
        colorField: 'category',
        color: ['#1979C9', '#D62A0D', '#FAA219', '#27B154'],

        lineStyle: (datum: NutrientsChartData) => ({
            lineWidth: 3,
            stroke: (() => {
                switch (datum.category) {
                    case 'СВ (Сухое вещество)':
                        return '#1979C9';
                    case 'СП (Сырой протеин)':
                        return '#D62A0D';
                    case 'ЧЭП (Чистая энергия для прироста)':
                        return '#FAA219';
                    case 'НДК (Нейтрально-детергентная клетчатка)':
                        return '#27B154';
                    default:
                        return '#8884d8';
                }
            })(),
        }),

        point: {
            size: 4,
            shape: 'circle',
            style: (datum: NutrientsChartData) => ({
                fill: 'white',
                stroke: (() => {
                    switch (datum.category) {
                        case 'СВ (Сухое вещество)':
                            return '#1979C9';
                        case 'СП (Сырой протеин)':
                            return '#D62A0D';
                        case 'ЧЭП (Чистая энергия для прироста)':
                            return '#FAA219';
                        case 'НДК (Нейтрально-детергентная клетчатка)':
                            return '#27B154';
                        default:
                            return '#8884d8';
                    }
                })(),
                lineWidth: 2,
            }),
        },

        tooltip: {
            title: (d: NutrientsChartData) => `${d.date} - Рацион: ${d.rationName}`,
            customContent: (title: string, items: any[]) => {
                const rationName = items[0]?.data?.rationName || '';
                const nutrients = items.filter((_, index) => index < 4);

                return `<div style="padding: 8px;">
                    <div style="margin-bottom: 8px; font-weight: bold;">${title}</div>
                    <div style="margin-bottom: 8px;">
                        <span style="color: #666;">Рацион: </span>
                        <span>${rationName}</span>
                    </div>
                    ${nutrients
                        .map(
                            (item) => `
                        <div style="display: flex; justify-content: space-between; margin-bottom: 4px;">
                            <div>
                                <span style="display: inline-block; width: 8px; height: 8px; 
                                    background: ${item.color}; margin-right: 8px;"></span>
                                <span>${item.name}</span>
                            </div>
                            <div>${item.value}</div>
                        </div>
                    `
                        )
                        .join('')}
                </div>`;
            },
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
                text: 'Количество',
                style: {
                    fontSize: 14,
                },
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
                <h2>Динамика основных нутриентов за 30 дней</h2>
                <Button type='primary' onClick={handleDownload} loading={isLoading}>
                    Скачать данные
                </Button>
            </Flex>
            <Line {...config} className='bottom-margin-xl' {...config} style={{ overflowX: 'auto' }} />
        </div>
    );
};
