import { TableProps } from 'antd';
import { IDailyActionTable } from '../../../../app/livestock-movement/data/interface/IDailyActionTable';
import { TableCheckbox } from '../../TableCheckbox';
import { SortersAnimalsType } from '../../../../utils/sortersAnimals';

export const columnsTableHistoryTransfer = (
    sorters: SortersAnimalsType
): TableProps<IDailyActionTable>['columns'] => [
    {
        title: '№ животного',
        dataIndex: 'tagNumber',
        key: 'tagNumber',
        minWidth: 140,
        sorter: true,
        sortOrder:
            sorters.column === 'TagNumber'
                ? sorters.descending
                    ? 'descend'
                    : 'ascend'
                : null,
    },
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
        title: 'Исполнитель',
        dataIndex: 'performedBy',
        key: 'performedBy',
        minWidth: 180,
        sorter: true,
        sortOrder:
            sorters.column === 'PerformedBy'
                ? sorters.descending
                    ? 'descend'
                    : 'ascend'
                : null,
    },
    {
        title: 'Примечания',
        dataIndex: 'notes',
        key: 'notes',
        minWidth: 135,
        sorter: true,
        sortOrder:
            sorters.column === 'Notes'
                ? sorters.descending
                    ? 'descend'
                    : 'ascend'
                : null,
    },
    {
        title: 'Старая группа',
        dataIndex: 'oldGroupName',
        key: 'oldGroupName',
        minWidth: 140,
        sorter: true,
        sortOrder:
            sorters.column === 'OldGroupName'
                ? sorters.descending
                    ? 'descend'
                    : 'ascend'
                : null,
    },
    {
        title: 'Новая группа',
        dataIndex: 'newGroupName',
        key: 'newGroupName',
        minWidth: 140,
        sorter: true,
        sortOrder:
            sorters.column === 'NewGroupName'
                ? sorters.descending
                    ? 'descend'
                    : 'ascend'
                : null,
    },
    {
        title: 'Выбрать',
        dataIndex: 'checked',
        key: 'checked',
        minWidth: 106,
        render: (_, { id }) => <TableCheckbox id={id} />,
    },
];
