import { Button, Flex, Form, message } from 'antd';
import { DatePickerForm } from '../../../../global-components/custom-inputs/form-inputs/date-picker-form/DatePickerForm';
import { TextAreaForm } from '../../../../global-components/custom-inputs/form-inputs/text-area-form/TextAreaForm';
import {
    newDailyAction,
    useCreateDailyActionsMutation,
} from '../../../../app-service/services/dailyActions';
import { useAppSelector } from '../../../../app-service/hooks';
import {
    selectAnimals,
    selectIsGroup,
    selectSelectedAnimals,
} from '../../../../app-service/slices/animalsDailyActionsSlice';
import {
    useGetGroupQuery,
    useGetUsersQuery,
} from '../../../../app-service/services/general';
import dayjs from 'dayjs';
import { FormTypeTransfer } from '../../data/types/FormTypes';
import { useEffect, useState } from 'react';
import { SelectForm } from '../../../../global-components/custom-inputs/form-inputs/select-form/SelectForm';
import { FieldCustom } from '../../../../global-components/custom-inputs/field/Field';
import styles from '../../styles/form-styles.module.css';
import { SelectInputForm } from '../../../../global-components/custom-inputs/form-inputs/select-input-form/SelectInputForm';

type Props = {
    resetHistory: () => void;
};

export const FormAddTransfer = ({ resetHistory }: Props) => {
    const { data: users } = useGetUsersQuery();
    const [createDailyActions] = useCreateDailyActionsMutation();
    const isGroup = useAppSelector(selectIsGroup);
    const selectedAnimals = useAppSelector(selectSelectedAnimals);
    const [oldGroup, setOldGroup] = useState<string>('');
    const [form] = Form.useForm();
    const animals = useAppSelector(selectAnimals);
    const options =
        useGetGroupQuery().data?.map((group) => ({
            label: group.name,
            value: group.id,
        })) || [];

    useEffect(() => {
        if (!isGroup) {
            setOldGroup(
                animals.find((animal) => animal.id === selectedAnimals[0])?.groupId || ''
            );
        }
    }, [selectAnimals]);
    const [messageApi, contextHolder] = message.useMessage();

    const addAction = async (dataForm: FormTypeTransfer) => {
        const data: newDailyAction[] = selectedAnimals.map((selectAnimal) => ({
            animalId: selectAnimal,
            type: 'Перевод',
            date: dayjs(dataForm.dateTransfer).format('YYYY-MM-DD'),
            performedBy:
                usersOptions.find((option) => dataForm.name === option.value)?.label ||
                dataForm.name,
            notes: dataForm.note,
            newGroupId: dataForm.group,
            oldGroupId: animals.find((animal) => animal.id === selectAnimal)?.groupId,
        }));
        try{
            await createDailyActions(data);
            form.resetFields();
            resetHistory();
            messageApi.success('Перевод успешно сохранен');
        }catch{
            messageApi.error('Ошибка при сохранении перевода');
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
        <Form onFinish={addAction} form={form}>
            {contextHolder}
            <Flex className={styles['form-body']} wrap>
                <DatePickerForm
                    name='dateTransfer'
                    label='Дата перевода'
                    required
                    defaultValue={dayjs()}
                />
                {!isGroup && (
                    <FieldCustom
                        label='Старая группа'
                        value={
                            animals.find((animal) => animal.id === selectedAnimals[0])
                                ?.groupName || ' '
                        }
                    />
                )}
                <SelectForm
                    label='Новая группа'
                    name='group'
                    placeholder='Выберите группу '
                    options={
                        !isGroup
                            ? options?.filter((option) => option.value !== oldGroup)
                            : options
                    }
                    styles={{ maxWidth: '475px' }}
                    required
                />
                <SelectInputForm
                    label='Кто проводил перевод'
                    name='name'
                    placeholder='Введите ФИО'
                    options={usersOptions}
                    styles={{ maxWidth: '475px' }}
                />
                <TextAreaForm
                    name='note'
                    label='Примечание'
                    placeholder='Дополнительная информация'
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
    );
};
