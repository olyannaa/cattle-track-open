import { Checkbox, TableProps } from 'antd';

import { SortersAnimalsType } from '../../../../utils/sortersAnimals';
import { TableCheckbox } from '../../../../global-components/daily-actions/TableCheckbox';
import { IDailyActionTable } from '../../../../app/veterinary-activities/data/interface/IDailyActionTable';

export const columnsTableHistoryResearch = (
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
        title: 'Название исследования',
        dataIndex: 'name',
        key: 'name',
        minWidth: 100,
        sorter: true,
        sortOrder:
            sorters.column === 'Name'
                ? sorters.descending
                    ? 'descend'
                    : 'ascend'
                : null,
    },
    {
        title: 'Вид материала',
        dataIndex: 'materialType',
        key: 'materialType',
        minWidth: 120,
        sorter: true,
        sortOrder:
            sorters.column === 'MaterialType'
                ? sorters.descending
                    ? 'descend'
                    : 'ascend'
                : null,
    },
    {
        title: 'Дата забора',
        dataIndex: 'collectionDate',
        key: 'collectionDate',
        minWidth: 120,
        sorter: true,
        sortOrder:
            sorters.column === 'CollectionDate'
                ? sorters.descending
                    ? 'descend'
                    : 'ascend'
                : null,
    },
    {
        title: 'Кто проводил взятие',
        dataIndex: 'collectedBy',
        key: 'collectedBy',
        minWidth: 140,
        sorter: true,
        sortOrder:
            sorters.column === 'CollectedBy'
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
        title: 'Результат',
        dataIndex: 'result',
        key: 'result',
        minWidth: 120,
        render: (_, action) => (
            <div style={{ marginLeft: '20px' }}>
                <Checkbox checked={action.result === 'true' ? true : false} />
            </div>
        ),
        sorter: true,
        sortOrder:
            sorters.column === 'result'
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
