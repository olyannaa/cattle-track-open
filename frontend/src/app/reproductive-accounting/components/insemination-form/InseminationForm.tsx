import { Button, DatePicker, Form, message, Radio, Select } from 'antd';
import React, { useEffect, useState } from 'react';
import { InputLabel } from '../../../../global-components/custom-inputs/input-label/InputLabel';
import TextArea from 'antd/es/input/TextArea';
import styles from '../../ReproductiveAccountingPage.module.css';
import { InseminationTypeForm } from './insemination-type-form/InseminationTypeForm';
import {
    RequestInsemination,
    useGetInseminationAnimalsQuery,
    useRegistrationInseminationBatchMutation,
    useRegistrationInseminationMutation,
} from '../../services/reproductive';
import { SelectDataType } from '../../../../utils/selectDataType';
import { isErrorType } from '../../../../utils/errorType';
import dayjs from 'dayjs';
import { SelectInputForm } from '../../../../global-components/custom-inputs/form-inputs/select-input-form/SelectInputForm';
import { useGetUsersQuery } from '../../../../app-service/services/general';
import { AiInseminationFormValues } from '../../../ai-event-input/aiFormMapping';

type Props = {
    aiFormValues?: AiInseminationFormValues;
};

export const InseminationForm = ({ aiFormValues }: Props) => {
    const { data: users } = useGetUsersQuery();
    const [messageApi, contextHolder] = message.useMessage();
    const requiredRule = [{ required: true, message: 'Обязательное поле' }];
    const { data, refetch } = useGetInseminationAnimalsQuery();
    const [cows, setCows] = useState<SelectDataType[]>([]);
    const [registerInsemination, { isLoading }] = useRegistrationInseminationMutation();
    const [registerInseminationBatch, { isLoading: isLoadingBatch }] =
        useRegistrationInseminationBatchMutation();
    const [form] = Form.useForm();

    useEffect(() => {
        refetch();
    }, []);

    useEffect(() => {
        if (data) {
            const selectOptions: SelectDataType[] = data.map((animal) => ({
                value: animal.animalId,
                label: animal.name,
            }));
            setCows(selectOptions);
        }
    }, [data]);

    useEffect(() => {
        if (!aiFormValues) return;

        form.setFieldsValue({
            cowIds: aiFormValues.cowIds,
            date: aiFormValues.date,
            inseminationType: aiFormValues.inseminationType,
            spermBatch: aiFormValues.spermBatch,
            spermManufacturer: aiFormValues.spermManufacturer,
            bullIds: aiFormValues.bullIds,
            embryoId: aiFormValues.embryoId,
            embryoManufacturer: aiFormValues.embryoManufacturer,
            technician: aiFormValues.technician,
            bullName: aiFormValues.bullName,
        });
    }, [aiFormValues, form]);

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

    const registerNewInsemination = async (values: RequestInsemination) => {
        values.date = dayjs(values.date).format('YYYY-MM-DD');
        values.technician =
            usersOptions.find((option) => values.technician === option.value)?.label ||
            values.technician;
        try {
            if (values.cowIds && values.cowIds.length > 1) {
                await registerInseminationBatch({
                    items: [values],
                }).unwrap();
            } else {
                await registerInsemination(values).unwrap();
            }
            messageApi.open({
                type: 'success',
                content: 'Осеменение зарегистрировано',
            });
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

    return (
        <React.Fragment>
            {contextHolder}
            <Form
                form={form}
                className='content-container'
                onFinish={registerNewInsemination}
            >
                <h2 className='form-title'>Регистрация осеменения</h2>
                <div>
                    <InputLabel label='Выберите животное из списка' required={true} />
                    <Form.Item name='cowIds' rules={requiredRule}>
                        <Select
                            className='form-input_default'
                            options={cows}
                            optionFilterProp='label'
                            mode='multiple'
                            allowClear
                        ></Select>
                    </Form.Item>
                </div>

                <div>
                    <InputLabel label='Дата осеменения' required={true} />
                    <Form.Item
                        className='form-input_default'
                        name='date'
                        initialValue={dayjs()}
                        rules={requiredRule}
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
                    <InputLabel label='Тип осеменения' required={true} />
                    <Form.Item name='inseminationType'>
                        <Radio.Group className={styles['reproductive__radio-group']}>
                            <div className={styles['reproductive__radio-container']}>
                                <div className='radio-border'>
                                    <Radio value='Искусственное'>Искусственное</Radio>
                                </div>
                                <div className='radio-border'>
                                    <Radio value='Естественное'>Естественное</Radio>
                                </div>
                                <div className='radio-border'>
                                    <Radio value='Эмбрион'>Эмбрион</Radio>
                                </div>
                            </div>
                        </Radio.Group>
                    </Form.Item>
                </div>
                <InseminationTypeForm />
                <SelectInputForm
                    label='Техник'
                    name='technician'
                    placeholder='Введите ФИО'
                    options={usersOptions}
                    styles={{ maxWidth: '100%' }}
                />
                <div>
                    <InputLabel label='Примечания' />
                    <Form.Item name='notes'>
                        <TextArea
                            className='form-input_default'
                            placeholder='Дополнительная информация'
                        ></TextArea>
                    </Form.Item>
                </div>
                <Button
                    type='primary'
                    htmlType='submit'
                    loading={isLoading || isLoadingBatch}
                    disabled={isLoading || isLoadingBatch}
                >
                    Зарегистрировать осеменение
                </Button>
            </Form>
        </React.Fragment>
    );
};
