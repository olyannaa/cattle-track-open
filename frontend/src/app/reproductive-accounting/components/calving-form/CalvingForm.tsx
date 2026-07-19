import { Button, DatePicker, Form, Input, message, Radio, Select } from 'antd';
import React, { useEffect, useState } from 'react';
import { InputLabel } from '../../../../global-components/custom-inputs/input-label/InputLabel';
import TextArea from 'antd/es/input/TextArea';
import styles from '../../ReproductiveAccountingPage.module.css';
import { ConfirmCalvingModal, ResultCalvingModal } from './modal/ConfirmCalvingModal';
import {
    RequestCalving,
    useGetCalvingQuery,
    useRegisterCalvingMutation,
} from '../../services/reproductive';
import { SelectDataType } from '../../../../utils/selectDataType';
import { isErrorType } from '../../../../utils/errorType';
import dayjs from 'dayjs';
import { SelectInputForm } from '../../../../global-components/custom-inputs/form-inputs/select-input-form/SelectInputForm';
import { useGetUsersQuery } from '../../../../app-service/services/general';

export const CalvingForm = () => {
    const { data: users } = useGetUsersQuery();
    const [messageApi, contextHolder] = message.useMessage();
    const rules = [{ required: true, message: 'Обязательное поле' }];
    const [form] = Form.useForm();
    const calfType = Form.useWatch('type', form);
    const [isModalOpen, setIsModalOpen] = useState(false);

    const { data, refetch } = useGetCalvingQuery();
    const [cows, setCows] = useState<SelectDataType[]>([]);
    const [registerCalving, { isLoading: loading }] = useRegisterCalvingMutation();
    const [modalResult, setModalResult] = useState<ResultCalvingModal | null>(null);

    useEffect(() => {
        refetch();
    }, []);

    useEffect(() => {
        if (data) {
            const selectOptions: SelectDataType[] = data.map((animal) => ({
                value: animal.id,
                label: animal.name,
            }));
            setCows(selectOptions);
        }
    }, [data]);

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

    const registerNewCalving = async (values: RequestCalving) => {
        try {
            const value = data?.find((animal) => animal.id === values.cowId);
            if (value?.cowId) {
                values.cowId = value.cowId;
                values.inseminationId = value.inseminationId;
            }
            values.bullIds = value?.bullIds;
            values.cowTagNumber = value?.cowTagNumber;
            values.date = dayjs(values.date).format('YYYY-MM-DD');
            values.veterinar =
                usersOptions.find((option) => values.veterinar === option.value)?.label ||
                values.veterinar;
            await registerCalving(values).unwrap();
            if (values.type !== 'Аборт') {
                setModalResult({
                    mother: values.cowTagNumber,
                    calfType: values.method,
                    date: values.date.toString(),
                });
                setIsModalOpen(true);
            } else {
                messageApi.open({
                    type: 'success',
                    content: 'Отёл зарегистрирован',
                });
            }
            refetch();
            form.resetFields();
        } catch (err) {
            if (isErrorType(err) && err?.data?.errorText) {
                messageApi.open({
                    type: 'error',
                    content: err.data.errorText,
                });
            } else {
                messageApi.open({
                    type: 'error',
                    content: 'Сервис временно не доступен. Попробуйте позже',
                });
            }
        }
    };

    const closeModal = () => {
        setIsModalOpen(false);
        setModalResult(null);
    };

    return (
        <React.Fragment>
            {contextHolder}
            <Form form={form} onFinish={registerNewCalving}>
                <div className='content-container'>
                    <h2 className='form-title'>Регистрация отёлов</h2>
                    <div>
                        <InputLabel label='Выберите корову' required={true} />
                        <Form.Item name='cowId' rules={rules}>
                            <Select
                                className='form-input_default'
                                options={cows}
                                showSearch
                                optionFilterProp='label'
                            ></Select>
                        </Form.Item>
                    </div>
                    <div>
                        <InputLabel label='Дата отёла' required={true} />
                        <Form.Item
                            className='form-input_default'
                            name='date'
                            rules={rules}
                            initialValue={dayjs()}
                        >
                            <DatePicker
                                format='DD.MM.YYYY'
                                type='date'
                                className='form-input_default date'
                                placeholder='xx.xx.xxxx'
                            ></DatePicker>
                        </Form.Item>
                    </div>
                    <div>
                        <InputLabel label='Тяжесть отёла' required={true} />
                        <Form.Item name='complication' rules={rules}>
                            <Radio.Group className={styles['reproductive__radio-group']}>
                                <div className={styles['reproductive__radio-container']}>
                                    <div className='radio-border'>
                                        <Radio value='Нормальный'>Нормальный</Radio>
                                    </div>
                                    <div className='radio-border'>
                                        <Radio value='Сложный'>Сложный</Radio>
                                    </div>
                                    <div className='radio-border'>
                                        <Radio value='Кесарево'>Кесарево</Radio>
                                    </div>
                                    <div className='radio-border'>
                                        <Radio value='Другое'>Другое</Radio>
                                    </div>
                                </div>
                            </Radio.Group>
                        </Form.Item>
                    </div>
                    <div>
                        <InputLabel label='Тип отёла' required={true} />
                        <Form.Item name='type' rules={rules}>
                            <Radio.Group className={styles['reproductive__radio-group']}>
                                <div className={styles['reproductive__radio-container']}>
                                    <div className='radio-border'>
                                        <Radio value='Живой'>Живой</Radio>
                                    </div>
                                    <div className='radio-border'>
                                        <Radio value='Мертворожденный'>
                                            Мертворожденный
                                        </Radio>
                                    </div>
                                    <div className='radio-border'>
                                        <Radio value='Аборт'>Аборт</Radio>
                                    </div>
                                </div>
                            </Radio.Group>
                        </Form.Item>
                    </div>
                    <SelectInputForm
                        label='Ветеринар'
                        name='veterinar'
                        placeholder='Введите ФИО'
                        options={usersOptions}
                        styles={{ maxWidth: '432px' }}
                    />
                    <div>
                        <InputLabel label='Назначенное лечение' />
                        <Form.Item name='treatments'>
                            <TextArea
                                rows={3}
                                className='form-input_default'
                                placeholder='Дополнительная информация'
                            />
                        </Form.Item>
                    </div>
                    <div>
                        <InputLabel label='Патологии' />
                        <Form.Item name='pathology'>
                            <TextArea
                                rows={3}
                                className='form-input_default'
                                placeholder='Введите патологии'
                            />
                        </Form.Item>
                    </div>
                    {calfType !== 'Живой' && (
                        <Button
                            type='primary'
                            htmlType='submit'
                            className='form-button form-button__margin-top-xl'
                            loading={loading}
                            disabled={loading}
                        >
                            Зарегистрировать отёл
                        </Button>
                    )}
                </div>
                {(calfType === 'Живой' || calfType === 'Мертворожденный') && (
                    <div className='content-container'>
                        <h2 className='form-title'>Регистрация теленка</h2>
                        <div className='form-input_default'>
                            <InputLabel label='Номер бирки' required={true} />
                            <Form.Item name='calfTagNumber' rules={rules}>
                                <Input placeholder='Введите номер бирки' />
                            </Form.Item>
                        </div>
                        <div style={{ maxWidth: '432px' }}>
                            <InputLabel label='Теленок' required={true} />
                            <Form.Item name='method' rules={rules}>
                                <Radio.Group
                                    className={styles['reproductive__radio-group']}
                                >
                                    <div
                                        className={
                                            styles['reproductive__radio-container']
                                        }
                                    >
                                        <div className='radio-border'>
                                            <Radio value='Телка'>Телка</Radio>
                                        </div>
                                        <div className='radio-border'>
                                            <Radio value='Бычок'>Бычок</Radio>
                                        </div>
                                    </div>
                                </Radio.Group>
                            </Form.Item>
                        </div>
                        <div>
                            <InputLabel label='Вес при рождении' />
                            <Form.Item name='weight'>
                                <Input
                                    className='form-input_default'
                                    placeholder='Введите вес, кг'
                                />
                            </Form.Item>
                        </div>
                        <Button
                            type='primary'
                            htmlType='submit'
                            className='form-button form-button__margin-top-xl'
                            loading={loading}
                            disabled={loading}
                        >
                            Зарегистрировать отёл
                        </Button>
                    </div>
                )}
            </Form>
            {modalResult && (
                <ConfirmCalvingModal
                    open={isModalOpen}
                    onClose={closeModal}
                    data={modalResult}
                />
            )}
        </React.Fragment>
    );
};
