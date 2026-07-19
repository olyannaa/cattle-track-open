import { Alert, Button, Flex, Form, message, Modal } from 'antd';
import { InputForm } from '../../../../global-components/custom-inputs/form-inputs/input-form/InputForm';
import { TextAreaForm } from '../../../../global-components/custom-inputs/form-inputs/text-area-form/TextAreaForm';
import { DietComponentTable } from './diet-components-table/DietComponentsTable';
import { useCreateDietMutation, useGetRationsWithComponentsQuery } from '../../services/feeding-record';
import { NewDiet } from '../../data/new-diet';
import { ErrorHandlerMessage } from '../../../../utils/errorHandlerMessage';
import { useWatch } from 'antd/es/form/Form';

type CreateDietModalProps = {
    open: boolean;
    onCancel: () => void;
    onSuccess?: () => void;
};

export const CreateDietModal = ({ open, onCancel }: CreateDietModalProps) => {
    const { refetch: refetchRations } = useGetRationsWithComponentsQuery();
    const [createDietForm] = Form.useForm();
    const [createDiet, { isLoading }] = useCreateDietMutation();
    const [messageApi, contextHolder] = message.useMessage();
    const watchedComponents = useWatch('components', createDietForm);

    const handleSubmit = async (values: NewDiet) => {
        try {
            await createDiet(values).unwrap();
            messageApi.open({
                type: 'success',
                content: 'Рацион успешно создан!',
            });
            refetchRations();
            createDietForm.resetFields();
        } catch (err) {
            messageApi.open({
                type: 'error',
                content: ErrorHandlerMessage(err),
            });
        }
    };

    const handleClose = () => {
        onCancel();
        createDietForm.resetFields();
    };

    return (
        <Modal open={open} onCancel={handleClose} width={'75%'} footer={null} style={{ margin: 'auto' }}>
            {contextHolder}
            <Form form={createDietForm} style={{ width: '100%' }} onFinish={handleSubmit}>
                <Flex vertical style={{ maxWidth: '920px' }}>
                    <Flex className='form-row-inputs'>
                        <InputForm
                            placeholder='Введите название рациона'
                            label={'Название рациона'}
                            name={'rationName'}
                            required={true}
                        />
                    </Flex>
                    <TextAreaForm name='description' placeholder='Описание рациона' label='Описание' />
                </Flex>
                <DietComponentTable />
                {!watchedComponents?.length && (
                    <Alert
                        className='alert'
                        type='info'
                        showIcon
                        message='В рационе должен быть хотя бы один компонент'
                    ></Alert>
                )}
                <Flex justify='end' style={{ marginTop: 24 }}>
                    <Button
                        htmlType='submit'
                        type='primary'
                        disabled={!watchedComponents?.length || isLoading}
                        loading={isLoading}
                    >
                        Создать рацион
                    </Button>
                </Flex>
            </Form>
        </Modal>
    );
};
