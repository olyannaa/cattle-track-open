import { Table, TablePaginationConfig } from 'antd';
import { useEffect, useState } from 'react';
import {
    IResponsePaginationInfo,
    useLazyGetWeightsAnimalQuery,
    useLazyGetWeightsPaginationInfoQuery,
    weightInfo,
} from '../../../service/weightControl';
import { useAppDispatch, useAppSelector } from '../../../../../app-service/hooks';
import {
    changeSortersWeightControls,
    resetSortersWeightControls,
    selectSelectedAnimalWeightControl,
    selectSortersWeightControl,
} from '../../../service/weightControlSlice';

import { TableWeight } from '../../../data/types/tableWeight';
import { columnsTableWeights } from '../../../data/const/columnsTableWeights';
import { FilterValue, SorterResult } from 'antd/es/table/interface';

export const TableWeights = () => {
    const selectedAnimal = useAppSelector(selectSelectedAnimalWeightControl);
    const sortersWeight = useAppSelector(selectSortersWeightControl);
    const [paginationInfo, setPaginationInfo] = useState<IResponsePaginationInfo>();
    const [weights, setWeights] = useState<weightInfo[]>([]);

    const dispatch = useAppDispatch();

    const [getWeightsAnimalQuery, { isLoading: isLoadingGetWeights }] =
        useLazyGetWeightsAnimalQuery();
    const [getWeightsPaginationInfoQuery] = useLazyGetWeightsPaginationInfoQuery();

    const getWeightsAnimal = async () => {
        const response = (
            await getWeightsAnimalQuery({ id: selectedAnimal, sorters: sortersWeight })
        ).data;
        setWeights(response || []);
    };

    const getPaginationInfo = async () => {
        const response = (await getWeightsPaginationInfoQuery(selectedAnimal)).data;
        setPaginationInfo(response);
    };

    useEffect(() => {
        getWeightsAnimal();
    }, [sortersWeight]);

    useEffect(() => {
        getPaginationInfo();
        dispatch(resetSortersWeightControls());
    }, [selectedAnimal]);

    const onChangeTable = (
        newPagination: TablePaginationConfig,
        _filters: Record<string, FilterValue | null>,
        sorter: SorterResult<TableWeight> | SorterResult<TableWeight>[]
    ) => {
        if (!sorter || (!Array.isArray(sorter) && !sorter.field)) {
            dispatch(
                changeSortersWeightControls({
                    page: newPagination.current,
                    column: '',
                    descending: true,
                })
            );
        } else {
            if (!Array.isArray(sorter)) {
                const field = sorter.field as string;
                dispatch(
                    changeSortersWeightControls({
                        page: newPagination.current,
                        column: field.charAt(0).toUpperCase() + field.slice(1),
                        descending: sorter.order === 'descend',
                    })
                );
            }
        }
    };

    return (
        <Table<TableWeight>
            columns={columnsTableWeights(sortersWeight)}
            style={{ width: '100%', overflowX: 'auto' }}
            dataSource={weights.map((weight) => ({
                ...weight,
                key: weight.id,
            }))}
            loading={isLoadingGetWeights}
            pagination={{
                showSizeChanger: false,
                current: sortersWeight.page,
                total: paginationInfo?.count,
                pageSize: paginationInfo?.entriesPerPage,
                showTotal: (total, range) =>
                    `${range[0]}-${range[1]} из ${total} элементов`,
            }}
            onChange={onChangeTable}
        />
    );
};
