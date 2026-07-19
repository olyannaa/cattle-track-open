// columnsTableDrugs.ts
import { TableProps } from 'antd';
import { IDrugsTable } from '../interface/IDrugsTable';
import { SettingsDrugTable } from '../../components/drugs/components/SettingsDrugTable';
import type { MessageInstance } from 'antd/es/message/interface';

export const getColumnsTableDrugs = (
    messageApi: MessageInstance
): TableProps<IDrugsTable>['columns'] => [
    {
        title: 'Название',
        dataIndex: 'name',
        key: 'name',
        minWidth: 140,
    },
    {
        title: 'Действующее вещество',
        dataIndex: 'substance',
        key: 'substance',
        minWidth: 140,
    },
    {
        title: 'Срок выведения',
        dataIndex: 'drugEliminatior',
        key: 'drugEliminatior',
        minWidth: 100,
    },
    {
        title: 'Срок хранения',
        dataIndex: 'shelfLife',
        key: 'shelfLife',
        minWidth: 100,
    },
    {
        title: 'Производитель',
        dataIndex: 'factory',
        key: 'factory',
        minWidth: 100,
    },
    {
        title: '',
        dataIndex: 'checked',
        key: 'checked',
        minWidth: 106,
        render: (_, drug) => <SettingsDrugTable drug={drug} messageApi={messageApi} />,
    },
];
