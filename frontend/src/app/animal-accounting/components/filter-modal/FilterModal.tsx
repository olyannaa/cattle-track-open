import React, { forwardRef, useEffect, useImperativeHandle, useState } from 'react';
import { Button, Checkbox, Divider, Drawer, Flex, Form, Input, Select } from 'antd';
import { InputLabel } from '../../../../global-components/custom-inputs/input-label/InputLabel';
import { animalCatTable } from '../../../../const/animal-cat';
import { SelectResponseType, useGetAnimalGroupsQuery, useGetBreedQuery } from '../../../register-animal/services/registration-animal';
import { formatDataForSelectInput, SelectDataType } from '../../../../utils/formatting-data';
import { optionsStatusFilter } from '../../../../const/animal-status -options';
import { CloseOutlined } from '@ant-design/icons';
import styles from './FilterModal.module.css';
import { useGetOriginsQuery, useGetPlacesOriginQuery } from '../../services/animals';
import { IFilters } from '../../data/interfaces/animal-filters-params';

interface FilterModalProps {
  open: boolean;
  setOpen: React.Dispatch<React.SetStateAction<boolean>>;
  onFiltersChange: (filters: IFilters) => void;
  isLoading: boolean,
  initialFilters?: IFilters;
}

export interface FilterModalRef {
    resetForm: () => void;
}

const FilterModal =  forwardRef<FilterModalRef, FilterModalProps>(({ open, setOpen, onFiltersChange, isLoading }, ref) => {
    const [form] = Form.useForm();
    const { data: breed } = useGetBreedQuery();
    const [breeds, setBreeds] = useState<SelectDataType[]>([]);
    const { data } = useGetAnimalGroupsQuery();
    const [animalGroups, setAnimalGroups] = useState<SelectDataType[]>([]);
    const { data: origins } = useGetOriginsQuery();
    const { data: locationsOrigin } = useGetPlacesOriginQuery();
    const [originOptions, setOriginOptions] = useState<SelectDataType[]>([]);
    const [locationOptions, setLocationOptions] = useState<SelectDataType[]>([]);

    useEffect(() => {
        if (origins?.length) {
            const preparedData: SelectResponseType[] = origins.filter((i) => i.trim().length).map((i: string) => ({ 
                id: i, 
                name: i 
            }));
            setOriginOptions(formatDataForSelectInput(preparedData))
        }

        if (locationsOrigin?.length) {
            const preparedData: SelectResponseType[] = locationsOrigin.filter((i) => i.trim().length).map((i: string) => ({ 
                id: i, 
                name: i 
            }));
            setLocationOptions(formatDataForSelectInput(preparedData))
        }

    }, [origins, locationsOrigin])

    useEffect(() => {
        if (data) {
            setAnimalGroups(formatDataForSelectInput(data));
        }
        if (breed) {
            setBreeds([
                ...formatDataForSelectInput(breed)
            ]);
        }
    }, [data, breed]);

    useImperativeHandle(ref, () => ({
        resetForm() {
            form.resetFields();
        },
    }));

    const onClose = () => {
        setOpen(false);
    };

    const mapIdsToLabels = (ids: string[] = [], options: SelectDataType[] = []) => {
        return ids
            .map(id => options.find(opt => opt.value === id)?.label)
            .filter(Boolean) as string[];
};

   const handleApplyFilters = async () => {
        try {
            const values = await form.validateFields();
            const groupNames = mapIdsToLabels(values.GroupNames, animalGroups);
            const breedNames = mapIdsToLabels(values.Breeds, breeds);
            const filters: IFilters = {
                tagNumber: values.TagNumber || '',
                types: values.Types || [],
                breeds: breedNames || [],
                groupNames: groupNames,
                statuses: values.Statuses || [],
                origins: values.Origins || [], 
                originLocations: values.OriginLocations || [],
                motherTagNumber: values.MotherTagNumber || '',
                fatherTagNumber: values.FatherTagNumber || '',
                birthDateFrom: values.BirthDateFrom,
                birthDateTo: values.BirthDateTo
            };

            onFiltersChange(filters);
            onClose();
        } catch (error) {
            console.error('Ошибка при применении фильтров:', error);
        }
    };

    const resetFilter = () => {
        form.resetFields();
        onFiltersChange({});
    }

  return (
    <>
      <Drawer
        placement='right'
        closable={false}
        onClose={onClose}
        open={open}
        className={styles['filter-modal']}
        styles={{ body: { paddingBottom: 0 } }}
      >
        <div className={styles['filter-modal__header']}>
            <Flex className={styles['filter-modal__close']}>
                <CloseOutlined onClick={onClose}/>
            </Flex>
            <h2>Фильтры</h2>
            <p className={styles['filter-modal__desc']}>Настройте фильтры для отображения нужных животных</p>
            <Button className={styles['filter-modal__btn']} icon={<CloseOutlined />} onClick={resetFilter}>Сбросить все фильтры</Button>
            <Divider />
        </div>
        <Form form={form} className={styles['filter-modal__content']} onFinish={handleApplyFilters}>
            <div>
                <InputLabel label='№ бирки' />
                <Form.Item name='TagNumber'>
                    <Input
                            placeholder='Содержит...'
                    ></Input>
                </Form.Item>
            </div>
            <div>
                <InputLabel label='Тип' />
                <Form.Item name='Types'>
                    <Checkbox.Group options={animalCatTable} />
                </Form.Item>
            </div>
            <div>
                <InputLabel label='Дата рождения' />
                <Form.Item name='BirthDateFrom' label='От'>
                    <Input
                        type='date'
                        placeholder='дд.мм.гг'
                    ></Input>
                </Form.Item>
                <Form.Item name='BirthDateTo' label='До'>
                    <Input
                        type='date'
                        placeholder='дд.мм.гг'
                    ></Input>
                </Form.Item>
            </div>
            <div>
                <InputLabel label='Порода' />
                <Form.Item name='Breeds'>
                    <Select
                        options={breeds}
                        placeholder='Содержит'
                        mode="multiple"
                    ></Select>
                </Form.Item>
            </div>
            <div>
                <InputLabel label='Группа' />
                <Form.Item name='GroupNames'>
                    <Select
                        mode="multiple"
                        placeholder='Содержит'
                        options={animalGroups}
                    ></Select>
                </Form.Item>
            </div>
            <div>
                <InputLabel label='Статус' />
                <Form.Item name='Statuses'>
                     <Checkbox.Group options={optionsStatusFilter} />
                </Form.Item>
            </div>
            <div>
                <InputLabel label='Происхождение' />
                <Form.Item name='Origins'>
                    <Select
                        options={originOptions}
                        placeholder='Содержит'
                        mode="multiple"
                    ></Select>
                </Form.Item>
            </div>
            <div>
                <InputLabel label='Место происхождения' />
                <Form.Item name='OriginLocations'>
                    <Select
                        options={locationOptions}
                        placeholder='Содержит'
                        mode="multiple"
                    ></Select>
                </Form.Item>
            </div>
            <div>
                <InputLabel label='№ отца' />
                <Form.Item name='FatherTagNumber'>
                    <Input
                            placeholder='Содержит...'
                    ></Input>
                </Form.Item>
            </div>
            <div>
                <InputLabel label='№ матери' />
                <Form.Item name='MotherTagNumber'>
                    <Input
                            placeholder='Содержит...'
                    ></Input>
                </Form.Item>
            </div>
            <Divider />
            <div className={styles['filter-modal__footer']} >
                <Button className={styles['filter-modal__button']}
                loading={isLoading}
                type='primary' htmlType='submit'>Применить фильтры</Button>
            </div>
        </Form>
      </Drawer>
    </>
  );
});

export default FilterModal;
