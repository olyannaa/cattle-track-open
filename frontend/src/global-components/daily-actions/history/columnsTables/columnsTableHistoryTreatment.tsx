import { TableProps } from 'antd';

import { SortersAnimalsType } from '../../../../utils/sortersAnimals';
import { TableCheckbox } from '../../../../global-components/daily-actions/TableCheckbox';
import { IDailyActionTable } from '../../../../app/veterinary-activities/data/interface/IDailyActionTable';

export const columnsTableHistoryTreatment = (
    sorters: SortersAnimalsType
): TableProps<IDailyActionTable>['columns'] => [
    {
        title: '№ животного',
        dataIndex: 'tagNumber',
        key: 'tagNumber',
        minWidth: 120,
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
        minWidth: 105,
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
        title: 'Тип обработки',
        dataIndex: 'subtype',
        key: 'subtype',
        minWidth: 135,
        sorter: true,
        sortOrder:
            sorters.column === 'Subtype'
                ? sorters.descending
                    ? 'descend'
                    : 'ascend'
                : null,
    },
    {
        title: 'Диагноз',
        dataIndex: 'result',
        key: 'result',
        minWidth: 115,
        sorter: true,
        sortOrder:
            sorters.column === 'Result'
                ? sorters.descending
                    ? 'descend'
                    : 'ascend'
                : null,
    },
    {
        title: 'Препарат',
        dataIndex: 'medicine',
        key: 'medicine',
        minWidth: 120,
        sorter: true,
        sortOrder:
            sorters.column === 'Medicine'
                ? sorters.descending
                    ? 'descend'
                    : 'ascend'
                : null,
    },
    {
        title: 'Доза',
        dataIndex: 'dose',
        key: 'dose',
        minWidth: 90,
        sorter: true,
        sortOrder:
            sorters.column === 'Dose'
                ? sorters.descending
                    ? 'descend'
                    : 'ascend'
                : null,
    },
    {
        title: 'Срок выведения',
        dataIndex: 'drugEliminator',
        key: 'drugEliminator',
        minWidth: 133,
        sorter: true,
        sortOrder:
            sorters.column === 'DrugEliminator'
                ? sorters.descending
                    ? 'descend'
                    : 'ascend'
                : null,
    },
    {
        title: 'Дата следующего осмотра',
        dataIndex: 'nextDate',
        key: 'nextDate',
        minWidth: 133,
        sorter: true,
        sortOrder:
            sorters.column === 'NextDate'
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
        title: 'Выбрать',
        dataIndex: 'checked',
        key: 'checked',
        minWidth: 106,
        render: (_, { id }) => <TableCheckbox id={id} />,
    },
];
