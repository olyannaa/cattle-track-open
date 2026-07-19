import { Flex, Form } from 'antd';
import { useEffect, useState } from 'react';
import {
    useGetGroupQuery,
    useGetIdentificationsFieldsQuery,
} from '../../app-service/services/general';
import { useAppDispatch, useAppSelector } from '../../app-service/hooks';
import {
    addSelectedAnimal,
    changeFiltersAnimals,
    selectAnimals,
    selectFiltersAnimals,
    selectIsGroup,
    selectKeyTab,
    selectSelectedAnimals,
} from '../../app-service/slices/animalsDailyActionsSlice';
import {
    useLazyGetFilterAnimalsQuery,
    useLazyGetIdentificationValuesQuery,
} from '../../app-service/services/dailyActions';
import { SelectFilters } from '../custom-inputs/filter-inputs/SelectFilters';
import { InputSearch } from '../custom-inputs/filter-inputs/InputSearch';
import { RadioGroupWithSwitch } from '../custom-inputs/filter-inputs/RadioGroupWithSwitch';
import { getOptionsType } from '../../app/livestock-movement/data/const/optionsSelect';

export const FormFilter = () => {
    const keyTab = useAppSelector(selectKeyTab);
    const filters = useAppSelector(selectFiltersAnimals);
    const isGroup = useAppSelector(selectIsGroup);
    const [getFilterAnimalsQuery] = useLazyGetFilterAnimalsQuery();
    const [getIdentificationValuesQuery] = useLazyGetIdentificationValuesQuery();
    const [identificationValues, setIdentificationValues] =
        useState<{ label: string; value: string }[]>();
    const selectedAnimals = useAppSelector(selectSelectedAnimals);
    const [form] = Form.useForm();
    const dispatch = useAppDispatch();
    const indentificationFields = useGetIdentificationsFieldsQuery().data?.map(
        (field) => ({
            label: field.name,
            value: field.id,
        })
    );
    const groups = useGetGroupQuery().data?.map((field) => ({
        label: field.name,
        value: field.id,
    }));
    const animals = useAppSelector(selectAnimals).map((animal) => ({
        label: animal.tagNumber,
        value: animal.id,
    }));

    const handlerSearch = async (value: string) => {
        await getFilterAnimalsQuery({ filters: { ...filters, tagNumber: value } });
    };

    const getIdentificationValues = async () => {
        const response = (
            await getIdentificationValuesQuery({
                identificationId: filters.identificationFieldId!,
                groupId: filters.groupId,
                isActive: filters.isActive,
                type: filters.type,
            })
        ).data;
        setIdentificationValues(response?.map((el) => ({ label: el, value: el })));
        dispatch(
            changeFiltersAnimals({
                name: 'identificationFieldValue',
                value: '',
            })
        );
    };

    const handlerChangeAllTypes = () => {
        if (keyTab !== '7') {
            dispatch(
                changeFiltersAnimals({
                    name: 'type',
                    value: '',
                })
            );
        }
    };

    useEffect(() => {
        if (filters.identificationFieldId) {
            getIdentificationValues();
        }
    }, [filters.groupId, filters.identificationFieldId, filters.isActive, filters.type]);

    useEffect(() => {
        form.setFieldValue('identification_method', '');
        if (filters.identificationFieldId) {
            getIdentificationValues();
        }
    }, [filters.identificationFieldId]);

    return (
        <Form
            style={{ display: 'flex', flexWrap: 'wrap', gap: '16px', rowGap: '24px' }}
            form={form}
        >
            <SelectFilters
                label='Группа содержания'
                options={
                    groups?.length ? [{ label: 'Все группы', value: '' }, ...groups] : []
                }
                onChange={(value) =>
                    dispatch(
                        changeFiltersAnimals({
                            name: 'groupId',
                            value: value,
                        })
                    )
                }
                value={filters.groupId || ''}
            />
            <RadioGroupWithSwitch
                options={getOptionsType(keyTab)}
                label='Категория животного'
                isGroup={isGroup}
                onChange={(value) =>
                    dispatch(
                        changeFiltersAnimals({
                            name: 'type',
                            value: value,
                        })
                    )
                }
                onChangeAll={handlerChangeAllTypes}
                value={filters.type || ''}
            />
            {!isGroup && (
                <>
                    <Flex style={{ width: '100%', gap: '16px', rowGap: '24px' }} wrap>
                        <SelectFilters
                            label='Способ идентификации'
                            options={
                                indentificationFields?.length
                                    ? [
                                          { label: 'Все способы', value: '' },
                                          ...indentificationFields,
                                      ]
                                    : []
                            }
                            placeholder='Выберите способ'
                            onChange={(value) =>
                                dispatch(
                                    changeFiltersAnimals({
                                        name: 'identificationFieldId',
                                        value: value,
                                    })
                                )
                            }
                            value={filters.identificationFieldId || ''}
                        />
                        {filters.identificationFieldId && (
                            <SelectFilters
                                label={
                                    indentificationFields?.find(
                                        (field) =>
                                            filters.identificationFieldId === field.value
                                    )?.label || ''
                                }
                                options={identificationValues || []}
                                onChange={(value) =>
                                    dispatch(
                                        changeFiltersAnimals({
                                            name: 'identificationFieldValue',
                                            value: value,
                                        })
                                    )
                                }
                                value={filters.identificationFieldValue || ''}
                            />
                        )}
                    </Flex>
                    <InputSearch
                        label='Поиск по номеру'
                        placeholder='Введите номер животного для фильтрации списка'
                        onSearch={handlerSearch}
                    />
                    <SelectFilters
                        label='Выберите животное из списка'
                        options={animals}
                        value={
                            animals.length === 1 ? animals[0].value : selectedAnimals[0]
                        }
                        onChange={(value) => dispatch(addSelectedAnimal(value))}
                    />
                </>
            )}
        </Form>
    );
};
