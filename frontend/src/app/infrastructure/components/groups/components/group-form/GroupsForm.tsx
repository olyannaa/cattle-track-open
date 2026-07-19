import { Alert, Button, Form, Input, message, Select } from 'antd';
import { InputLabel } from '../../../../../../global-components/custom-inputs/input-label/InputLabel';
import TextArea from 'antd/es/input/TextArea';
import React, { useEffect, useState } from 'react';
import {
    IGroup,
    useAddGroupMutation,
    useGetGroupsTypesQuery,
} from '../../../../services/infrastructure-service';
import { formatDataForSelectInput } from '../../../../../../utils/data-functions/formatting-data';
import { SelectDataType } from '../../../../../../utils/selectDataType';
import { isErrorType } from '../../../../../../utils/errorType';

export const GroupsForm = ({ isEmpty }: { isEmpty: boolean }) => {
    const { data } = useGetGroupsTypesQuery();
    const [groupsTypes, setGroupsTypes] = useState<SelectDataType[]>([]);
    const [addGroup, { isLoading }] = useAddGroupMutation();
    const [messageApi, contextHolder] = message.useMessage();
    const [form] = Form.useForm();
    const rules = [{ required: true, message: 'Обязательное поле' }];

    useEffect(() => {
        if (data) {
            setGroupsTypes(formatDataForSelectInput(data));
        }
    }, [data]);

    const addNewGroup = async (values: IGroup) => {
        try {
            addGroup(values).unwrap();
            messageApi.open({
                type: 'success',
                content: 'Изменения успешно сохранены',
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
                onFinish={addNewGroup}
            >
                <h2 className='form-title'>Управление группами</h2>
                <div className='form-row-inputs'>
                    <div className='form-input_default'>
                        <InputLabel label='Название группы' required={true}/>
                        <Form.Item
                            name='name'
                            rules={rules}
                        >
                            <Input placeholder='Введите название'></Input>
                        </Form.Item>
                    </div>
                    <div className='form-input_default form-row-input__with-margin-xl'>
                        <InputLabel label='Тип группы' required={true}/>
                        <Form.Item name='typeId' rules={rules}>
                            <Select
                                options={groupsTypes}
                                placeholder='Выберите тип'
                            ></Select>
                        </Form.Item>
                    </div>
                </div>
                <div>
                    <InputLabel label='Расположение' />
                    <Form.Item name='location'>
                        <Input
                            className='form-input_default'
                            placeholder='Укажите расположение'
                        ></Input>
                    </Form.Item>
                </div>
                <div>
                    <InputLabel label='Описание' />
                    <Form.Item
                        className='form-input_default'
                        name='description'
                    >
                        <TextArea
                            rows={4}
                            placeholder='Дополнительная информация'
                            defaultValue={''}
                        ></TextArea>
                    </Form.Item>
                </div>
                <Button
                    type='primary'
                    htmlType='submit'
                    className='form-button_default'
                    size='large'
                    loading={isLoading}
                    disabled={isLoading}
                >
                    Создать группу
                </Button>
                {isEmpty && (
                    <Alert
                        className='alert top-margin-xl'
                        message='У вас пока нет созданных групп'
                        type='info'
                        showIcon
                    />
                )}
            </Form>
        </React.Fragment>
    );
};
