import {
    Button,
    Checkbox,
    CheckboxChangeEvent,
    Flex,
    Table,
    TablePaginationConfig,
    Typography,
} from 'antd';
import {
    useDeleteDailyActionsMutation,
    useDeleteDailyActionsResearchMutation,
    useLazyGetAllActionsIdQuery,
    useLazyGetDailyActionsQuery,
    useLazyGetPaginationInfoDailyActionsQuery,
} from '../../../app-service/services/dailyActions';

import { useEffect, useState } from 'react';
import { IDailyActionTable } from '../../../app/livestock-movement/data/interface/IDailyActionTable';
import { useAppDispatch, useAppSelector } from '../../../app-service/hooks';
import {
    addAllActions,
    changeSortersDailyActions,
    deleteAllActions,
    selectAllActionsId,
    selectDailyActions,
    selectPaginationInfoDailyActions,
    selectSelectedDailyActions,
    selectSortersDailyActions,
} from '../../../app-service/slices/dailyActionsSlice';
import { FilterValue, SorterResult } from 'antd/es/table/interface';

import { getColumnsTable } from '../../../functions/getColumnsTableHistory';
import styles from './History.module.css';
import { selectKeyTab } from '../../../app-service/slices/animalsDailyActionsSlice';
import { getNameTabs } from './items';

type Props = {
    isVeterinary?: boolean;
}

export const History = ({isVeterinary= false}: Props) => {
    const sorters = useAppSelector(selectSortersDailyActions);
    const keyTab = useAppSelector(selectKeyTab);
    const selectedDailyActions = useAppSelector(selectSelectedDailyActions);
    const paginationInfo = useAppSelector(selectPaginationInfoDailyActions);
    const dailyActions = useAppSelector(selectDailyActions);
    const allActionsId = useAppSelector(selectAllActionsId);

    const [nameTab, setNameTab] = useState(getNameTabs(keyTab, isVeterinary));
    const [isSelectedAllActions, setIsSelectedAllActions] = useState<boolean>(false);

    const dispatch = useAppDispatch();

    const [getDailyActionsQuery, { isLoading: isLoadingGetDailyActions }] =
        useLazyGetDailyActionsQuery();
    const [deleteDailyActionsQuery] = useDeleteDailyActionsMutation();
    const [deleteDailyActionsResearchQuery] = useDeleteDailyActionsResearchMutation();
    const [
        getPaginationInfoDailyActionsQuery,
        { isLoading: isLoadingGetPaginationInfoDailyActions },
    ] = useLazyGetPaginationInfoDailyActionsQuery();
    const [getAllActionsIdQuery] = useLazyGetAllActionsIdQuery();

    const getDailyActions = async () => {
        await getDailyActionsQuery({
            ...sorters,
            type: nameTab,
        });
    };

    const getPaginationInfoDailyActivities = async () => {
        await getPaginationInfoDailyActionsQuery(nameTab);
    };

    const getAllActionsId = async () => {
        await getAllActionsIdQuery({
            ...sorters,
            type: nameTab,
        });
    };

    const handlerChangeSelectedAllActions = (e: CheckboxChangeEvent) => {
        setIsSelectedAllActions(e.target.checked);
        if (e.target.checked) {
            dispatch(addAllActions(allActionsId));
        } else {
            dispatch(deleteAllActions());
        }
    };

    const handlerDeleteActions = async () => {
        if (selectedDailyActions.length === 0) {
            return;
        }
        if (keyTab === '2') {
            await deleteDailyActionsResearchQuery(selectedDailyActions);
        } else {
            await deleteDailyActionsQuery(selectedDailyActions);
        }
        dispatch(
            changeSortersDailyActions({
                ...sorters,
                page: 1,
            })
        );
    };

    const onChangeTable = (
        newPagination: TablePaginationConfig,
        _filters: Record<string, FilterValue | null>,
        sorter: SorterResult<IDailyActionTable> | SorterResult<IDailyActionTable>[]
    ) => {
        if (!sorter || (!Array.isArray(sorter) && !sorter.field)) {
            dispatch(
                changeSortersDailyActions({
                    page: newPagination.current,
                    column: '',
                    descending: true,
                })
            );
        } else {
            if (!Array.isArray(sorter)) {
                const field = sorter.field as string;
                dispatch(
                    changeSortersDailyActions({
                        page: newPagination.current,
                        column: field.charAt(0).toUpperCase() + field.slice(1),
                        descending: sorter.order === 'descend',
                    })
                );
            }
        }
    };

    useEffect(() => {
        const newName = getNameTabs(keyTab, isVeterinary);
        dispatch(
            changeSortersDailyActions({
                column: '',
                descending: true,
                page: 1,
            })
        );
        dispatch(deleteAllActions());
        setNameTab(newName);
    }, [keyTab]);

    useEffect(() => {
        if (
            selectedDailyActions.length === allActionsId.length &&
            allActionsId.length > 0
        ) {
            setIsSelectedAllActions(true);
        } else {
            setIsSelectedAllActions(false);
        }
    }, [selectedDailyActions]);

    useEffect(() => {
        getDailyActions();
        getPaginationInfoDailyActivities();
        getAllActionsId();
    }, [sorters]);

    return (
        <Flex
            gap={12}
            vertical
            style={{
                padding: '24px 20px',
                background: 'var(--global-bg)',
                borderRadius: '8px',
            }}
        >
            <Typography.Title level={3}>История</Typography.Title>
            <Flex
                justify='space-between'
                className={styles['wrapper-delete-actions']}
                wrap={'wrap-reverse'}
                gap={8}
            >
                <Button
                    onClick={handlerDeleteActions}
                    style={{
                        height: '40px',
                    }}
                    className={styles['delete-actions__button']}
                >
                    Удалить выбранные записи
                </Button>
                <Flex align='center' gap={16} className={styles['wrapper-button']}>
                    <div
                        style={{ fontWeight: '500' }}
                    >{`Выбрано: ${selectedDailyActions.length}`}</div>

                    <Checkbox
                        onChange={handlerChangeSelectedAllActions}
                        style={{
                            width: '145px',
                            padding: '8px 12px 10px',
                            border: '1px solid var(--grey-border)',
                            borderRadius: '2px',
                            background: 'var(--global-bg)',
                            height: '40px',
                        }}
                        checked={isSelectedAllActions}
                    >
                        Выбрать все
                    </Checkbox>
                </Flex>
            </Flex>
            <Table<IDailyActionTable>
                key={keyTab}
                columns={getColumnsTable(keyTab === '1' && !isVeterinary ? '4' : keyTab, sorters)}
                dataSource={dailyActions.map((dailyAction) => ({
                    ...dailyAction,
                    key: dailyAction.id,
                }))}
                style={{ width: '100%', overflowX: 'auto' }}
                pagination={{
                    showSizeChanger: false,
                    current: sorters.page,
                    total: paginationInfo?.count,
                    pageSize: paginationInfo?.entriesPerPage,
                    showTotal: (total, range) =>
                        `${range[0]}-${range[1]} из ${total} элементов`,
                }}
                onChange={onChangeTable}
                loading={
                    isLoadingGetDailyActions || isLoadingGetPaginationInfoDailyActions
                }
            />
        </Flex>
    );
};
