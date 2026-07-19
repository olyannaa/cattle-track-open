import { Form, Input, Select } from 'antd';
import { useEffect, useState } from 'react';
import { SelectDataType } from '../../../../../../../utils/selectDataType';
import { useGetBullsQuery } from '../../../../../../reproductive-accounting/services/reproductive';
import { InputLabel } from '../../../../../../../global-components/custom-inputs/input-label/InputLabel';
import styles from '../../ManualRegistration.module.css';

export const InseminationTypeFormReg = () => {
    const inseminationType = Form.useWatch('InseminationType');
    const { data } = useGetBullsQuery();
    const [bulls, setBulls] = useState<SelectDataType[]>([]);

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
        <div>
            {inseminationType === 'Искусственное' && (
                <div className='form-row-inputs'>
                    <div className='form-input_default'>
                        <InputLabel label='Номер партии спермы' />
                        <Form.Item name='SpermBatch'>
                            <Input placeholder='Введите номер' />
                        </Form.Item>
                    </div>
                    <div className='form-input_default form-input_default form-row-input__with-margin-l'>
                        <InputLabel label='Производитель' />
                        <Form.Item name='SpermManufacturer'>
                            <Input className='form-input_default' placeholder='Введите название' />
                        </Form.Item>
                    </div>
                </div>
            )}

            {inseminationType === 'Естественное' && (
                <div className={styles['manual-register__changed-input']}>
                    <InputLabel label='Выберите быка' />
                    <Form.Item name='BullId'>
                        <Select className='form-input_default' placeholder='Выберите быка' options={bulls}></Select>
                    </Form.Item>
                </div>
            )}

            {inseminationType === 'Эмбрион' && (
                <div className='form-row-inputs'>
                    <div className='form-input_default'>
                        <InputLabel label='Номер эмбриона' />
                        <Form.Item name='EmbryoTag'>
                            <Input placeholder='Введите номер' />
                        </Form.Item>
                    </div>
                    <div className='form-input_default form-input_default form-row-input__with-margin-l'>
                        <InputLabel label='Производитель эмбриона' />
                        <Form.Item name='EmbryoManufacturer'>
                            <Input placeholder='Введите название' />
                        </Form.Item>
                    </div>
                </div>
            )}
        </div>
    );
};
