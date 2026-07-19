import { Flex, Form } from 'antd';
import { InputSearch } from '../../../../global-components/custom-inputs/filter-inputs/InputSearch';
import { SelectFilters } from '../../../../global-components/custom-inputs/filter-inputs/SelectFilters';
import {
    changeFiltersWeightControl,
    changeSelectedAnimal,
    selectAnimalsWeightControl,
    selectFiltersWeightControl,
    selectSelectedAnimalWeightControl,
} from '../../service/weightControlSlice';
import { useAppDispatch, useAppSelector } from '../../../../app-service/hooks';
import {
    useGetGroupQuery,
    useGetIdentificationsFieldsQuery,
} from '../../../../app-service/services/general';
import { useEffect, useState } from 'react';
import { RadioGroupWithSwitch } from '../../../../global-components/custom-inputs/filter-inputs/RadioGroupWithSwitch';
import {
    useLazyGetFilterAnimalsQuery,
    useLazyGetIdentificationValuesQuery,
} from '../../service/weightControl';

export const FormFilters = () => {
    const filters = useAppSelector(selectFiltersWeightControl);
    const selectedAnimal = useAppSelector(selectSelectedAnimalWeightControl);
    const animals = useAppSelector(selectAnimalsWeightControl).map((animal) => ({
        label: animal.tagNumber,
        value: animal.id,
    }));
    const [identificationValues, setIdentificationValues] =
        useState<{ label: string; value: string }[]>();

    const dispatch = useAppDispatch();
    const [form] = Form.useForm();

    const groups = useGetGroupQuery().data?.map((field) => ({
        label: field.name,
        value: field.id,
    }));

    const indentificationFields = useGetIdentificationsFieldsQuery().data?.map(
        (field) => ({
            label: field.name,
            value: field.id,
        })
    );

    const [getIdentificationValuesQuery] = useLazyGetIdentificationValuesQuery();
    const [getFilterAnimalsQuery] = useLazyGetFilterAnimalsQuery();

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
            changeFiltersWeightControl({
                name: 'identificationFieldValue',
                value: '',
            })
        );
    };

    const handlerSearch = async (value: string) => {
        await getFilterAnimalsQuery({ filters: { ...filters, tagNumber: value } });
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
                        changeFiltersWeightControl({
                            name: 'groupId',
                            value: value,
                        })
                    )
                }
                value={filters.groupId || ''}
                styles={{ maxWidth: '432px' }}
            />
            <RadioGroupWithSwitch
                options={['Телка', 'Нетель', 'Корова', 'Бычок', 'Бык']}
                label='Категория животного'
                isGroup={false}
                onChange={(value) =>
                    dispatch(
                        changeFiltersWeightControl({
                            name: 'type',
                            value: value,
                        })
                    )
                }
                value={filters.type || ''}
                styles={{ maxWidth: '432px' }}
            />
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
                            changeFiltersWeightControl({
                                name: 'identificationFieldId',
                                value: value,
                            })
                        )
                    }
                    value={filters.identificationFieldId || ''}
                    styles={{ maxWidth: '432px' }}
                />
                {filters.identificationFieldId && (
                    <SelectFilters
                        label={
                            indentificationFields?.find(
                                (field) => filters.identificationFieldId === field.value
                            )?.label || ''
                        }
                        options={identificationValues || []}
                        onChange={(value) =>
                            dispatch(
                                changeFiltersWeightControl({
                                    name: 'identificationFieldValue',
                                    value: value,
                                })
                            )
                        }
                        value={filters.identificationFieldValue || ''}
                        styles={{ maxWidth: '432px' }}
                    />
                )}
            </Flex>
            <InputSearch
                label='Поиск по номеру'
                placeholder='Введите номер животного для фильтрации списка'
                onSearch={handlerSearch}
                styles={{ maxWidth: '432px' }}
            />
            <SelectFilters
                label='Выберите животное из списка'
                options={animals}
                value={selectedAnimal}
                onChange={(value) => dispatch(changeSelectedAnimal(value))}
                styles={{ maxWidth: '432px' }}
            />
        </Form>
    );
};
