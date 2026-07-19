import { TableProps } from 'antd';
import { TableWeight } from '../types/tableWeight';
import { SortersAnimalsType } from '../../../../utils/sortersAnimals';

export const columnsTableWeights = (
    sorters: SortersAnimalsType
): TableProps<TableWeight>['columns'] => [
    {
        title: 'Дата',
        dataIndex: 'date',
        key: 'date',
        minWidth: 100,
        sorter: true,
        sortOrder:
            sorters.column === 'Date'
                ? sorters.descending
                    ? 'descend'
                    : 'ascend'
                : null,
    },
    {
        title: 'Вес, кг',
        dataIndex: 'weight',
        key: 'weight',
        minWidth: 140,
        sorter: true,
        sortOrder:
            sorters.column === 'Weight'
                ? sorters.descending
                    ? 'descend'
                    : 'ascend'
                : null,
    },

    {
        title: 'Возраст, мес',
        dataIndex: 'age',
        key: 'age',
        minWidth: 180,
        sorter: true,
        sortOrder:
            sorters.column === 'Age' ? (sorters.descending ? 'descend' : 'ascend') : null,
    },
    {
        title: 'СУП (кг/сут)',
        dataIndex: 'sup',
        key: 'sup',
        minWidth: 135,
        sorter: true,
        sortOrder:
            sorters.column === 'Sup' ? (sorters.descending ? 'descend' : 'ascend') : null,
    },
];
