import { Button, Flex, Form, message } from 'antd';
import { DatePickerForm } from '../../../../global-components/custom-inputs/form-inputs/date-picker-form/DatePickerForm';
import { InputForm } from '../../../../global-components/custom-inputs/form-inputs/input-form/InputForm';
import {
    IAnimal,
    newDailyAction,
    useCreateDailyActionsMutation,
    useLazyGetAnimalByIdQuery,
} from '../../../../app-service/services/dailyActions';
import { useAppSelector } from '../../../../app-service/hooks';
import {
    selectIsGroup,
    selectSelectedAnimals,
} from '../../../../app-service/slices/animalsDailyActionsSlice';
import { useEffect, useState } from 'react';
import {
    useGetIdentificationsFieldsQuery,
    useGetUsersQuery,
} from '../../../../app-service/services/general';
import dayjs from 'dayjs';
import { FormTypeAssigmentNumber } from '../../data/types/FormTypes';
import { SelectForm } from '../../../../global-components/custom-inputs/form-inputs/select-form/SelectForm';
import styles from '../../styles/form-styles.module.css';
import { SelectInputForm } from '../../../../global-components/custom-inputs/form-inputs/select-input-form/SelectInputForm';

type Props = {
    resetHistory: () => void;
};

export const FormAddAssigmentNumber = ({ resetHistory }: Props) => {
    const { data: users } = useGetUsersQuery();
    const [createDailyActions] = useCreateDailyActionsMutation();
    const isGroup = useAppSelector(selectIsGroup);
    const selectedAnimals = useAppSelector(selectSelectedAnimals);
    const [animal, setAnimal] = useState<IAnimal>();
    const [form] = Form.useForm();
    const [getAnimalByIdQuery] = useLazyGetAnimalByIdQuery();
    const [messageApi, contextHolder] = message.useMessage();

    const identificationFields =
        useGetIdentificationsFieldsQuery().data?.map((field) => ({
            label: field.name,
            value: field.id,
        })) || [];
    const [selectedField, setSelectedField] = useState<string>(
        identificationFields[0]?.value
    );
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
    const addAction = async (dataForm: FormTypeAssigmentNumber) => {
        const data: newDailyAction[] = selectedAnimals.map((animal) => ({
            animalId: animal,
            type: 'Присвоение номеров',
            date: dayjs(dataForm.date).format('YYYY-MM-DD'),
            performedBy:
                usersOptions.find((option) => dataForm.name === option.value)?.label ||
                dataForm.name,
            subtype: identificationFields.find((field) => field.value === selectedField)
                ?.label,
            identificationValue: dataForm.value,
        }));

        try{
            await createDailyActions(data);
            form.resetFields();
            resetHistory();
            messageApi.success('Номер успешно присвоен');
        }catch{
            messageApi.error('Ошибка при присвоении номера');
        }
    };

    const getAnimalById = async () => {
        const response = (await getAnimalByIdQuery(selectedAnimals[0])).data;
        setAnimal(response);
    };

    useEffect(() => {
        getAnimalById();
    }, [selectedAnimals[0]]);

    console.log('animal', animal);
    return (
        <Flex vertical gap={24}>
            {contextHolder}
            <Flex
                vertical
                style={{
                    padding: '16px',
                    border: '1px solid #D9D9D9',
                    borderRadius: '2px',
                }}
                gap={16}
            >
                <div style={{ fontSize: '16px', fontWeight: '500' }}>
                    Информация о выбранном животном
                </div>
                <Flex vertical gap={12} style={{ marginLeft: '20px' }}>
                    {animal ? (
                        <>
                            <div>
                                {' '}
                                {`Животное: ${animal?.tagNumber}, ${
                                    animal?.type
                                }, группа: ${animal?.groupName || 'Не назначена'}`}
                            </div>
                            {animal?.identificationFields.map(
                                (field) =>
                                    field.value && (
                                        <div key={field.name}>{`${field.name}: ${
                                            field.value || 'Не назначено'
                                        }`}</div>
                                    )
                            )}
                        </>
                    ) : (
                        'Животное не выбрано'
                    )}
                </Flex>
            </Flex>
            <Form onFinish={addAction} form={form}>
                <Flex className={styles['form-body']} wrap>
                    <SelectForm
                        label='Тип идентификации'
                        name='type'
                        options={identificationFields}
                        defaultValue={identificationFields[0]?.value || ''}
                        styles={{ maxWidth: '475px' }}
                        placeholder='Выберите причину'
                        required
                        onChange={(value) => setSelectedField(value)}
                    />
                    <InputForm
                        label='Значение'
                        name='value'
                        placeholder='Укажите новое значение'
                        required
                    />
                    <div style={{ width: '100%', marginBottom: '16px' }}>
                        <Flex
                            style={{
                                padding: '0 11px',
                                background: '#FFFFFF',
                                border: '1px solid #D9D9D9',
                                height: '40px',
                                fontSize: '16px',
                                color: '#00000040',
                                maxWidth: '475px',
                                width: '100%',
                            }}
                            align='center'
                        >
                            {`${
                                identificationFields.find(
                                    (field) => field.value === selectedField
                                )?.label
                            }: ${
                                animal?.identificationFields.find(
                                    (field) =>
                                        field.name ===
                                        identificationFields.find(
                                            (field) => field.value === selectedField
                                        )?.label
                                )?.value || ''
                            }`}
                        </Flex>
                    </div>

                    <DatePickerForm name='date' label='Дата присвоения' required />
                    <SelectInputForm
                        label='Кто присвоил'
                        name='name'
                        placeholder='Введите ФИО'
                        options={usersOptions}
                        styles={{ maxWidth: '475px' }}
                    />
                </Flex>
                <Button
                    type='primary'
                    size='large'
                    color='default'
                    variant='solid'
                    htmlType='submit'
                    disabled={selectedAnimals.length === 0}
                >
                    {isGroup ? 'Сохранить для выбранных животных' : 'Сохранить'}
                </Button>
            </Form>
        </Flex>
    );
};
