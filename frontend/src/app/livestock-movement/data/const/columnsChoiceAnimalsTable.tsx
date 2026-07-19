import { TableProps } from 'antd';
import { IDailyActionAnimalsTable } from '../interface/IDailyActionAnimalsTable';
import { TableCheckbox } from '../../../../global-components/daily-actions/TableCheckbox';

export const columnsChoiceAnimalsTable: TableProps<IDailyActionAnimalsTable>['columns'] =
    [
        {
            title: '№ животного',
            dataIndex: 'tagNumber',
            key: 'tagNumber',
            width: 140,
            minWidth: 140,
            sorter: true,
        },
        {
            title: 'Категория',
            dataIndex: 'type',
            key: 'type',
            minWidth: 137,
            sorter: true,
        },
        {
            title: 'Группа',
            dataIndex: 'groupName',
            key: 'groupName',
            minWidth: 140,
            sorter: true,
        },
        {
            title: 'Выбрать',
            dataIndex: 'checked',
            key: 'checked',
            render: (_, animal) => <TableCheckbox id={animal.id} isAnimal />,
            width: 104,
        },
    ];
