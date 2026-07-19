import { Form, Input, Select } from 'antd';
import { InputLabel } from '../../../../../global-components/custom-inputs/input-label/InputLabel';
import { useGetBullsQuery } from '../../../services/reproductive';
import { useEffect, useState } from 'react';
import { SelectDataType } from '../../../../../utils/selectDataType';

export const InseminationTypeForm = () => {
    const inseminationType = Form.useWatch('inseminationType');
    const { data } = useGetBullsQuery();
    const [bulls, setBulls] = useState<SelectDataType[]>([]);
    console.log(bulls);
    useEffect(() => {
        if (data) {
            const selectOptions: SelectDataType[] = data.map((animal) => ({
                value: animal.id,
                label: `№${animal.tagNumber}, ${animal.type}`,
            }));
            setBulls(selectOptions);
        }
    }, [data]);

    if (!inseminationType) {
        return null;
    }

    return (
        <div className='form-additional'>
            {inseminationType === 'Искусственное' && (
                <div className='form-grid-inputs'>
                    <div className='form-input_default'>
                        <InputLabel label='Номер партии спермы' />
                        <Form.Item name='spermBatch'>
                            <Input placeholder='Введите номер' />
                        </Form.Item>
                    </div>
                    <div className='form-input_default'>
                        <InputLabel label='Производитель' />
                        <Form.Item name='spermManufacturer'>
                            <Input
                                className='form-input_default'
                                placeholder='Введите название'
                            />
                        </Form.Item>
                    </div>
                    <div className='form-input_default'>
                        <InputLabel label='Имя отца' />
                        <Form.Item name='bullName'>
                            <Input
                                className='form-input_default'
                                placeholder='Введите имя'
                            />
                        </Form.Item>
                    </div>
                </div>
            )}

            {inseminationType === 'Естественное' && (
                <div>
                    <InputLabel label='Вероятные отцы' />
                    <Form.Item name='bullIds'>
                        <Select
                            className='form-input_default'
                            placeholder='Выберите быка'
                            options={bulls}
                            mode='multiple'
                            optionFilterProp='label'
                            allowClear
                        ></Select>
                    </Form.Item>
                </div>
            )}

            {inseminationType === 'Эмбрион' && (
                <div className='form-row-inputs'>
                    <div className='form-input_default'>
                        <InputLabel label='Номер эмбриона' />
                        <Form.Item name='embryoId'>
                            <Input placeholder='Введите номер' />
                        </Form.Item>
                    </div>
                    <div className='form-input_default form-input_default form-row-input__with-margin-l'>
                        <InputLabel label='Производитель эмбриона' />
                        <Form.Item name='embryoManufacturer'>
                            <Input placeholder='Введите название' />
                        </Form.Item>
                    </div>
                </div>
            )}
        </div>
    );
};
