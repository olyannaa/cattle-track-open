import { Table } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { HistoryPoint } from '../../../data/interfaces/animal-chart';

interface DataType extends HistoryPoint {
    key: string;
}

export const WeightTable = ({ points }: { points: HistoryPoint[] }) => {
    const dataSource: DataType[] = points.map((point, index) => ({
        ...point,
        key: index.toString(),
    }));

    const columns: ColumnsType<DataType> = [
        {
            title: 'Дата',
            dataIndex: 'x',
            key: 'x',
            render: (date: string) => new Date(date).toLocaleDateString('ru-RU'),
            sorter: (a, b) => new Date(a.x).getTime() - new Date(b.x).getTime(),
            defaultSortOrder: 'descend',
        },
        {
            title: 'Вес, кг',
            dataIndex: 'y',
            key: 'y',
            render: (weight: number | null) => (weight != null ? `${weight} кг` : '—'),
            sorter: (a, b) => {
                const aWeight = a.y != null ? Number(a.y) : -Infinity;
                const bWeight = b.y != null ? Number(b.y) : -Infinity;
                return aWeight - bWeight;
            },
        },
    ];

    return (
        <Table<DataType>
            className='bottom-margin-xl'
            columns={columns}
            dataSource={dataSource}
            showSorterTooltip={{ target: 'sorter-icon' }}
        />
    );
};
