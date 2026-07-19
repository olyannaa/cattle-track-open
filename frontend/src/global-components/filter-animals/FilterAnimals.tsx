import { Checkbox, CheckboxChangeEvent, Flex, Table, TablePaginationConfig } from 'antd';
import { CheckboxCustom } from '../custom-inputs/checkbox/Checkbox';
import { FormFilter } from '../form-filter/FormFilter';
import { useEffect, useState } from 'react';
import { columnsChoiceAnimalsTable } from '../../app/livestock-movement/data/const/columnsChoiceAnimalsTable';
import {
    IRequestGetFilterAnimals,
    IResponsePaginationInfoDailyActions,
    useLazyGetAllAnimalsIdQuery,
    useLazyGetFilterAnimalsQuery,
    useLazyGetPaginationInfoFilterAnimalsQuery,
} from '../../app-service/services/dailyActions';
import { IDailyActionAnimalsTable } from '../../app/livestock-movement/data/interface/IDailyActionAnimalsTable';
import { useAppDispatch, useAppSelector } from '../../app-service/hooks';
import {
    addAllAnimals,
    changeFiltersAnimals,
    changeIsGroup,
    changeSortersAnimals,
    deleteAllAnimals,
    resetFiltersAnimals,
    resetSortersAnimals,
    selectAnimals,
    selectAnimalsId,
    selectFiltersAnimals,
    selectIsGroup,
    selectKeyTab,
    selectSelectedAnimals,
    selectSortersAnimals,
} from '../../app-service/slices/animalsDailyActionsSlice';
import { FilterValue, SorterResult } from 'antd/es/table/interface';
import { FiltersAnimalsType } from '../../utils/filtersAnimals';
import { AnimalFilters } from '../../utils/animals';

export type FiltersType = {
    group: string;
    type: string;
    identificationId: string;
    identificationValue: string;
    isActive: boolean;
};

export const FilterAnimals = () => {
    const keyTab = useAppSelector(selectKeyTab);
    const filters = useAppSelector(selectFiltersAnimals);
    const animals: AnimalFilters[] = useAppSelector(selectAnimals);
    const selectedAnimals = useAppSelector(selectSelectedAnimals);
    const sorters = useAppSelector(selectSortersAnimals);
    const animalsId = useAppSelector(selectAnimalsId);
    const isGroup = useAppSelector(selectIsGroup);

    const dispatch = useAppDispatch();

    const [paginationInfo, setPaginationInfo] =
        useState<IResponsePaginationInfoDailyActions>();
    const [isSelectedAllAnimals, setIsSelectedAllAnimals] = useState<boolean>(false);
    const [getFilterAnimalsQuery, { isLoading: isLoadingGetFilterAnimals }] =
        useLazyGetFilterAnimalsQuery();
    const [
        getPaginationInfoFilterAnimalsQuery,
        { isLoading: isLoadingGetPaginationInfoFilterAnimals },
    ] = useLazyGetPaginationInfoFilterAnimalsQuery();
    const [getAllAnimalsIdQuery] = useLazyGetAllAnimalsIdQuery();

    const getFilterAnimals = async (
        data: IRequestGetFilterAnimals = { filters: filters, sorters: sorters }
    ) => {
        await getFilterAnimalsQuery(data);
    };

    const getPaginationInfoFilterAnimals = async (data: FiltersAnimalsType = filters) => {
        const response = (await getPaginationInfoFilterAnimalsQuery(data)).data;
        setPaginationInfo(response);
    };

    const getAllAnimalsId = async (
        data: IRequestGetFilterAnimals = { filters: filters, sorters: sorters }
    ) => {
        await getAllAnimalsIdQuery(data);
    };

    const handlerChangeSelectedAllActions = (e: CheckboxChangeEvent) => {
        setIsSelectedAllAnimals(e.target.checked);
        if (e.target.checked) {
            dispatch(addAllAnimals(animalsId));
        } else {
            dispatch(deleteAllAnimals());
        }
    };

    useEffect(() => {
        getFilterAnimals({
            filters: filters,
            sorters: { ...sorters, page: isGroup ? 1 : 0 },
        });
        getAllAnimalsId({
            filters: filters,
            sorters: { ...sorters, page: 0 },
        });
        dispatch(resetSortersAnimals());
        if (isGroup) {
            getPaginationInfoFilterAnimals();
            dispatch(changeSortersAnimals({ ...sorters, page: 1 }));
        }
        dispatch(deleteAllAnimals());
    }, [filters]);

    useEffect(() => {
        getFilterAnimals();
        if (isGroup) {
            getPaginationInfoFilterAnimals();
        }
    }, [sorters]);

    useEffect(() => {
        dispatch(resetFiltersAnimals());
    }, [isGroup]);

    useEffect(() => {
        if (selectedAnimals.length === animalsId.length && animalsId.length > 0) {
            setIsSelectedAllAnimals(true);
        } else {
            setIsSelectedAllAnimals(false);
        }
    }, [selectedAnimals]);

    const onChangeTable = (
        newPagination: TablePaginationConfig,
        _tableFilters: Record<string, FilterValue | null>,
        sorter:
            | SorterResult<IDailyActionAnimalsTable>
            | SorterResult<IDailyActionAnimalsTable>[]
    ) => {
        if (!sorter || (!Array.isArray(sorter) && !sorter.field)) {
            dispatch(
                changeSortersAnimals({
                    page: newPagination.current,
                    column: 'TagNumber',
                    descending: 'false',
                })
            );
        } else {
            if (!Array.isArray(sorter)) {
                const field = sorter.field as string;
                dispatch(
                    changeSortersAnimals({
                        page: newPagination.current,
                        column: field.charAt(0).toUpperCase() + field.slice(1),
                        descending: sorter.order === 'descend',
                    })
                );
            }
        }
    };
    return (
        <>
            <Flex style={{ gap: '4px', rowGap: '12px' }} wrap>
                {keyTab !== '6' && (
                    <>
                        <CheckboxCustom
                            title='Групповое действие'
                            onChange={(e) => dispatch(changeIsGroup(e.target.checked))}
                        />
                        <CheckboxCustom
                            title='Только активные животные'
                            onChange={(e) =>
                                dispatch(
                                    changeFiltersAnimals({
                                        name: 'isActive',
                                        value: e.target.checked,
                                    })
                                )
                            }
                            defaultChecked={true}
                        />
                    </>
                )}
            </Flex>
            <FormFilter />
            {isGroup && (
                <>
                    <Flex
                        justify='flex-end'
                        style={{ width: '100%' }}
                        gap={16}
                        align='center'
                    >
                        <div
                            style={{ fontWeight: '500' }}
                        >{`Выбрано: ${selectedAnimals.length}`}</div>
                        <Checkbox
                            onChange={handlerChangeSelectedAllActions}
                            style={{
                                width: '148px',
                                padding: '8px 12px 10px',
                                border: '1px solid var(--grey-border)',
                                borderRadius: '2px',
                                background: 'var(--global-bg)',
                                height: '40px',
                            }}
                            checked={isSelectedAllAnimals}
                        >
                            Выбрать все
                        </Checkbox>
                    </Flex>
                    <Table<IDailyActionAnimalsTable>
                        style={{ width: '100%', overflowX: 'auto' }}
                        columns={columnsChoiceAnimalsTable}
                        dataSource={animals.map((animal) => ({
                            ...animal,
                            key: animal.id,
                        }))}
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
                            isLoadingGetFilterAnimals ||
                            isLoadingGetPaginationInfoFilterAnimals
                        }
                    />
                </>
            )}
        </>
    );
};
