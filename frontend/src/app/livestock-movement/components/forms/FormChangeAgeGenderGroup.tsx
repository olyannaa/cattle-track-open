import { Button, Flex, Form, message } from 'antd';
import { DatePickerForm } from '../../../../global-components/custom-inputs/form-inputs/date-picker-form/DatePickerForm';
import {
    newDailyAction,
    useCreateDailyActionsMutation,
} from '../../../../app-service/services/dailyActions';
import { useAppSelector } from '../../../../app-service/hooks';
import {
    selectFiltersAnimals,
    selectIsGroup,
    selectSelectedAnimals,
} from '../../../../app-service/slices/animalsDailyActionsSlice';
import dayjs from 'dayjs';
import { FormTypeChangeAgeGenderGroup } from '../../data/types/FormTypes';
import { TextAreaForm } from '../../../../global-components/custom-inputs/form-inputs/text-area-form/TextAreaForm';
import { FieldCustom } from '../../../../global-components/custom-inputs/field/Field';
import styles from '../../styles/form-styles.module.css';
import { useGetUsersQuery } from '../../../../app-service/services/general';
import { SelectInputForm } from '../../../../global-components/custom-inputs/form-inputs/select-input-form/SelectInputForm';

type Props = {
    resetHistory: () => void;
};

export const FormChangeAgeGenderGroup = ({ resetHistory }: Props) => {
    const isGroup = useAppSelector(selectIsGroup);
    const [createDailyActions] = useCreateDailyActionsMutation();
    const selectedAnimals = useAppSelector(selectSelectedAnimals);
    const filters = useAppSelector(selectFiltersAnimals);
    const [form] = Form.useForm();
    const { data: users } = useGetUsersQuery();
    const [messageApi, contextHolder] = message.useMessage();
    const addAction = async (dataForm: FormTypeChangeAgeGenderGroup) => {
        const data: newDailyAction[] = selectedAnimals.map((animal) => ({
            animalId: animal,
            type: 'Изменение половозрастной группы',
            date: dayjs(dataForm.date).format('YYYY-MM-DD'),
            performedBy:
                usersOptions.find((option) => dataForm.name === option.value)?.label ||
                dataForm.name,
            notes: dataForm.notes,
            oldType: filters.type,
            newType: filters.type === 'Телка' ? 'Корова' : 'Бык',
        }));
        try{
            await createDailyActions(data);
            form.resetFields();
            resetHistory();
            messageApi.success('Изменение половозрастной группы успешно сохранено');
        }catch{
            messageApi.error('Ошибка при сохранении изменения половозрастной группы');
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
                    name='dateCulling'
                    label='Дата перевода'
                    required
                    defaultValue={dayjs()}
                />
                <FieldCustom
                    label='Старая половозрастная группа'
                    value={filters.type || ''}
                />
                <FieldCustom
                    label='Новая половозрастная группа'
                    value={filters.type === 'Телка' ? 'Корова' : 'Бык'}
                />
                <SelectInputForm
                    label='Кто проводил перевод'
                    name='name'
                    placeholder='Введите ФИО'
                    options={usersOptions}
                    styles={{ maxWidth: '475px' }}
                />
                <TextAreaForm
                    name='notes'
                    label='Примечания'
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
