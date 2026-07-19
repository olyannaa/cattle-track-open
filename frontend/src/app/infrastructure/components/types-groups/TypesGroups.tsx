import { Alert, Button, Form, Input, message } from 'antd';
import React from 'react';
import { InputLabel } from '../../../../global-components/custom-inputs/input-label/InputLabel';
import {
    NewInfrastructure,
    useAddNewGroupTypeMutation,
    useDeleteGroupTypeMutation,
    useGetGroupsTypesQuery,
} from '../../services/infrastructure-service';
import { InfrastructureTypes } from '../../data/enums/infrastructureTypes';
import { InfrastructureList } from '../infrastructure-list/InfrastructureList';
import { isErrorType } from '../../../../utils/errorType';

export const TypesGroups = () => {
    const {
        data: typesGroups,
        isFetching,
        isLoading,
    } = useGetGroupsTypesQuery();
    const [deleteGroupType] = useDeleteGroupTypeMutation();
    const [addTypeGroup, {isLoading: addLoading}] = useAddNewGroupTypeMutation();
    const [messageApi, contextHolder] = message.useMessage();
    const showAlert: boolean =
        !isFetching && !isLoading && (!typesGroups || typesGroups.length === 0);
    const [form] = Form.useForm();

    const addNewTypeGroup = async (value: NewInfrastructure) => {
        try {
            await addTypeGroup(value).unwrap();
            messageApi.open({
                type: 'success',
                content: 'Тип группы успешно создан',
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
                onFinish={addNewTypeGroup}
            >
                <h2 className='form-title'>Управление типами групп</h2>
                <div>
                    <InputLabel label='Название группы' required={true}/>
                    <Form.Item
                        name='name'
                        rules={[
                            { required: true, message: 'Обязательное поле' },
                        ]}
                    >
                        <Input
                            className='form-input_default'
                            placeholder='Введите название'
                        ></Input>
                    </Form.Item>
                </div>
                <Button
                    type='primary'
                    htmlType='submit'
                    className='form-button_default'
                    size='large'
                    loading={addLoading}
                    disabled={addLoading}
                >
                    Создать группу
                </Button>
                {showAlert && (
                    <Alert
                        className='alert top-margin-xl'
                        message='У вас пока нет созданных типов групп'
                        type='info'
                        showIcon
                    />
                )}
            </Form>

            {typesGroups && (
                <InfrastructureList
                    list={typesGroups}
                    partInfrastructure={InfrastructureTypes.typeGroup}
                    onDelete={deleteGroupType}
                    title='Доступные типы групп'
                />
            )}
        </React.Fragment>
    );
};
