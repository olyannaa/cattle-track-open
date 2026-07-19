import { DatePicker, Form, Radio } from 'antd';
import { InputLabel } from '../../../../../../global-components/custom-inputs/input-label/InputLabel';
import TextArea from 'antd/es/input/TextArea';
import dayjs from 'dayjs';
import styles from '../ManualRegistration.module.css';
import netelStyles from './NetelForm.module.css';
import useFormInstance from 'antd/es/form/hooks/useFormInstance';
import { InseminationTypeFormReg } from './insemination-type-form/InseminationTypeForm';
import { SelectInputForm } from '../../../../../../global-components/custom-inputs/form-inputs/select-input-form/SelectInputForm';
import { useGetUsersQuery } from '../../../../../../app-service/services/general';

export const NetelFormRegister = () => {
    const { data: users } = useGetUsersQuery();
    const requiredRule = [{ required: true, message: 'Обязательное поле' }];
    const form = useFormInstance();

    const handleInseminationDateChange = (date: dayjs.Dayjs | null) => {
        if (date) {
            const expectedDate = date.add(285, 'day');
            form.setFieldValue('ExpectedCalvingDate', expectedDate);
        } else {
            form.setFieldValue('ExpectedCalvingDate', null);
        }
    };
    const usersOptions = [
        {
            label: '',
            value: '',
        },
        ...(users?.map((user) => ({
            label: user.name,
            value: user.id,
        })) || []),
    ];
    return (
        <Form.Item className={styles['manual-register__additional-form']}>
            <InputLabel label='Для регистрации нетеля необходимо заполнить данные об осеменении' />
            <div className={styles['manual-register__changed-form']}>
                <div className={styles['manual-register__changed-input']}>
                    <InputLabel label='Дата осеменения' />
                    <Form.Item
                        name='InseminationDate'
                        rules={requiredRule}
                        className={styles['manual-register__changed-input']}
                        initialValue={dayjs()}
                    >
                        <DatePicker
                            format='DD.MM.YYYY'
                            type='date'
                            className='form-input_default date'
                            placeholder='xx.xx.xxxx'
                            onChange={handleInseminationDateChange}
                        ></DatePicker>
                    </Form.Item>
                </div>
                <div className={styles['manual-register__changed-input']}>
                    <InputLabel label='Ожидаемая дата отела' />
                    <Form.Item
                        className='form-input_default'
                        rules={requiredRule}
                        name='ExpectedCalvingDate'
                        initialValue={dayjs().add(285, 'day')}
                    >
                        <DatePicker
                            format='DD.MM.YYYY'
                            type='date'
                            className='form-input_default date'
                            placeholder='xx.xx.xxxx'
                        ></DatePicker>
                    </Form.Item>
                </div>
            </div>
            <InputLabel label='Тип осеменения' />
            <Form.Item name='InseminationType' style={{ maxWidth: 412 }}>
                <Radio.Group>
                    <div
                        className={styles['manual-register__origin']}
                        style={{ maxWidth: 412 }}
                    >
                        <div
                            className={`${netelStyles['netel-form__radio1']} radio-border`}
                        >
                            <Radio value='Искусственное'>Искусственное</Radio>
                        </div>
                        <div
                            className={`${netelStyles['netel-form__radio2']} radio-border`}
                        >
                            <Radio value='Естественное'>Естественное</Radio>
                        </div>
                        <div
                            className={`${netelStyles['netel-form__radio3']} radio-border`}
                        >
                            <Radio value='Эмбрион'>Эмбрион</Radio>
                        </div>
                    </div>
                </Radio.Group>
            </Form.Item>
            <InseminationTypeFormReg />
            <SelectInputForm
                label='Техник осеменения'
                name='Technician'
                placeholder='Введите ФИО'
                options={usersOptions}
                styles={{ maxWidth: '412px' }}
            />

            <InputLabel label='Примечание к осеменению' />
            <Form.Item name='Notes' className={styles['manual-register__changed-input']}>
                <TextArea rows={4} placeholder='Дополнительная информация'></TextArea>
            </Form.Item>
        </Form.Item>
    );
};
