import {
    Button,
    Flex,
    Table,
    TablePaginationConfig,
} from 'antd';
import styles from './AnimalAccountingPage.module.css';
import {
    useGetIdentificationFieldsNamesQuery,
    useLazyGetAnimalsQuery,
    useLazyGetPaginationInfoQuery,
} from './services/animals';
import { useEffect, useRef, useState } from 'react';
import { downloadXlsxAnimals } from '../../functions/fetchFiles';
import { IAnimalTable } from './data/interfaces/animalTable';
import { getColumns } from './data/const/tableAnimal';
import { IAnimal, IResponsePaginationInfo } from './data/types/animal';
import { FilterValue, SorterResult } from 'antd/es/table/interface';
import { Statistics } from './components/statistics/Statistics';
import FilterModal, { FilterModalRef } from './components/filter-modal/FilterModal';
import { CloseCircleOutlined, FilterOutlined, VerticalAlignBottomOutlined } from '@ant-design/icons';
import { IFilters } from './data/interfaces/animal-filters-params';

const DEFAULT_SORT_COLUMN = 'Status';
const ACTIVE_STATUS = 'Активное';

const getStatusPriority = (status?: string | null) =>
    status === ACTIVE_STATUS ? 0 : 1;

const sortActiveAnimalsFirst = (animals: IAnimal[]) =>
    [...animals].sort((first, second) => {
        const statusPriority =
            getStatusPriority(first.status) - getStatusPriority(second.status);

        if (statusPriority !== 0) {
            return statusPriority;
        }

        return String(first.tagNumber ?? '').localeCompare(
            String(second.tagNumber ?? ''),
            'ru',
            { numeric: true }
        );
    });

export const AnimalAccountingPage = () => {
    const isEditTable = false;
    const [animals, setAnimals] = useState<IAnimal[]>([]);
    const [currentPage, setCurrentPage] = useState<number>(1);
    const [sortedColumn, setSortedColumn] = useState<string | null>(DEFAULT_SORT_COLUMN);
    const [descending, setDescending] = useState<boolean>(false);
    const [paginationInfo, setPaginationInfo] = useState<IResponsePaginationInfo>();
    const [getPageCountQuery] = useLazyGetPaginationInfoQuery();
    const [getAnimalsQuery] = useLazyGetAnimalsQuery();
    /** Во время повторного вызова getAnimalsQuery или getPageCountQuery состояние загрузки не отслеживается, поэтому пришлось делать отдельный флаг */
    const [isLoadingTable, setIsLoadingTable] = useState(false);
    const { data } = useGetIdentificationFieldsNamesQuery();
    const [openFilter, setOpenFilter] = useState<boolean>(false);
    const [filters, setFilters] = useState<IFilters>({});
    const filterModalRef = useRef<FilterModalRef>(null);


    const getCountAnimals = async () => {
        const res = (
            await getPageCountQuery({
            filters: filters,
        })
        ).data;
        setPaginationInfo(res);
    };

    const onChangeTable = (
        newPagination: TablePaginationConfig,
        filters: Record<string, FilterValue | null>,
        sorter: SorterResult<IAnimalTable> | SorterResult<IAnimalTable>[]
    ) => {
        // eslint-disable-next-line @typescript-eslint/no-unused-expressions
        newPagination;
        // eslint-disable-next-line @typescript-eslint/no-unused-expressions
        filters;
        if (!sorter || (!Array.isArray(sorter) && !sorter.field)) {
            setSortedColumn(DEFAULT_SORT_COLUMN);
            setDescending(false);
        } else {
            if (!Array.isArray(sorter)) {
                const field = sorter.field as string;
                setSortedColumn(field.charAt(0).toUpperCase() + field.slice(1));
                setDescending(sorter.order === 'descend');
            }
        }
    };

    const getAnimals = async (page = 1) => {
        const response = (
            await getAnimalsQuery({
                page: page,
                filters: {
                    ...filters
                },
                sortInfo: {
                    column: sortedColumn ?? DEFAULT_SORT_COLUMN,
                    descending: descending,
                }
            })
        ).data;
        const nextAnimals = response || [];
        setAnimals(
            sortedColumn === DEFAULT_SORT_COLUMN && !descending
                ? sortActiveAnimalsFirst(nextAnimals)
                : nextAnimals
        );
    };

    const loadAnimalsPage = async (page = 1, withCount = true) => {
        setIsLoadingTable(true);
        try {
            if (withCount) {
                await Promise.all([getAnimals(page), getCountAnimals()]);
            } else {
                await getAnimals(page);
            }
        } finally {
            setIsLoadingTable(false);
        }
    };

    useEffect(() => {
        setCurrentPage(1);
        loadAnimalsPage(1);
    }, [sortedColumn, descending, filters]);

     const handleFiltersChange = (newFilters: IFilters) => {
        setFilters(newFilters);
    };

    const handlerExportXlsx = async () => {
        await downloadXlsxAnimals({
            filters: filters,
            sortInfo: {
                    column: sortedColumn ?? DEFAULT_SORT_COLUMN,
                    descending,
                }
        });
    };

    const handlerChangeCurrentPagination = (page: number) => {
        setCurrentPage(page);
        loadAnimalsPage(page, false);
    };


    return (
        <Flex vertical gap={'16px'}>
            <Statistics />
            <Flex vertical className='header-container header-container__full'>
                <h2 className='header-title'>Учет животных</h2>
                <Flex gap={8} className={styles['table__buttons']}>
                    <div>
                        <Button icon={<FilterOutlined />} onClick={() => setOpenFilter(!openFilter)}>
                            Фильтры
                            {Object.keys(filters || {}).length > 0 && (
                            <span 
                                style={{
                                    position: 'absolute',
                                    top: '-4px',
                                    right: '-4px',
                                    width: '8px',
                                    height: '8px',
                                    borderRadius: '50%',
                                    backgroundColor: '#FF4218',
                                }}
                            />
                            )}
                        </Button>
                         {Object.keys(filters || {}).length > 0 && (
                            <CloseCircleOutlined
                                onClick={() => {
                                    setFilters({});
                                    filterModalRef.current?.resetForm();
                                }}
                                style={{
                                    marginLeft: '4px',
                                    color: '#FF4218'
                                }}
                                title="Очистить фильтры" 
                            />
                        )}
                    </div>
                    <Button icon={<VerticalAlignBottomOutlined />} onClick={handlerExportXlsx}>
                        Сохранить таблицу
                    </Button>
                </Flex>
            </Flex>
            <Flex
                className={styles['table']}
                vertical
            >
                <Table<IAnimalTable>
                    columns={getColumns(isEditTable, data || [], {
                        column: sortedColumn,
                        descending,
                    })}
                    style={{
                        width: '100%',
                    }}
                    dataSource={animals.map((animal) => ({
                        ...animal,
                        key: animal.id,
                    }))}
                    loading={isLoadingTable}
                    pagination={{
                        showSizeChanger: false,
                        current: currentPage,
                        total: paginationInfo?.count,
                        pageSize: paginationInfo?.entriesPerPage,
                        onChange: (page) => handlerChangeCurrentPagination(page),
                        showTotal: (total, range) =>
                            `${range[0]}-${range[1]} из ${total} элементов`,
                        className: styles['table__pagination'],
                    }}
                    onChange={onChangeTable}
                />
            </Flex>
            <FilterModal ref={filterModalRef} open={openFilter} setOpen={setOpenFilter} onFiltersChange={handleFiltersChange}
            initialFilters={filters} isLoading={isLoadingTable}></FilterModal>
        </Flex>
    );
};
