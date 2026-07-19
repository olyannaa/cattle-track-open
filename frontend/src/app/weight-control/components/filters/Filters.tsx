import { useAppDispatch, useAppSelector } from '../../../../app-service/hooks';
import {
    changeFiltersWeightControl,
    changeSelectedAnimal,
    resetFiltersWeightControl,
    selectFiltersWeightControl,
} from '../../service/weightControlSlice';
import { FormFilters } from './FormFilters';
import {
    IRequestGetFilterAnimals,
    useLazyGetFilterAnimalsQuery,
} from '../../service/weightControl';
import { useEffect } from 'react';
import { Checkbox } from 'antd';

type Props = {
    keyTab: string;
};

export const Filters = ({ keyTab }: Props) => {
    const filters = useAppSelector(selectFiltersWeightControl);

    const dispatch = useAppDispatch();

    const [getFilterAnimalsQuery] = useLazyGetFilterAnimalsQuery();

    const getFilterAnimals = async (
        data: IRequestGetFilterAnimals = {
            filters: filters,
            sorters: {
                column: 'TagNumber',
                descending: true,
                page: 0,
            },
        }
    ) => {
        await getFilterAnimalsQuery(data);
    };

    useEffect(() => {
        getFilterAnimals({
            filters: filters,
        });
        dispatch(changeSelectedAnimal(''));
    }, [filters]);

    useEffect(() => {
        dispatch(resetFiltersWeightControl());
    }, [keyTab]);

    return (
        <>
            <Checkbox
                onChange={(e) =>
                    dispatch(
                        changeFiltersWeightControl({
                            name: 'isActive',
                            value: e.target.checked,
                        })
                    )
                }
                checked={filters.isActive}
                style={{
                    width: '100%',
                    maxWidth: '432px',
                    padding: '8px 12px 10px',
                    border: '1px solid var(--grey-border)',
                    borderRadius: '2px',
                    background: 'var(--global-bg)',
                    height: '40px',
                }}
            >
                Только активные животные
            </Checkbox>
            <FormFilters />
        </>
    );
};
