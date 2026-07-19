import { Button, Flex, Form, message } from 'antd';
import { DatePickerForm } from '../../../../../global-components/custom-inputs/form-inputs/date-picker-form/DatePickerForm';
import { InputForm } from '../../../../../global-components/custom-inputs/form-inputs/input-form/InputForm';
import { TextAreaForm } from '../../../../../global-components/custom-inputs/form-inputs/text-area-form/TextAreaForm';
import { CheckboxCustom } from '../../../../../global-components/custom-inputs/checkbox/Checkbox';
import { useAppSelector } from '../../../../../app-service/hooks';
import dayjs from 'dayjs';
import { FormTypeResearch } from '../../../data/types/FormTypes';
import { optionsResearch } from '../../../data/const/optionsSelect';
import { SelectForm } from '../../../../../global-components/custom-inputs/form-inputs/select-form/SelectForm';
import styles from '../../../styles/form-styles.module.css';
import { useGetUsersQuery } from '../../../../../app-service/services/general';
import { SelectInputForm } from '../../../../../global-components/custom-inputs/form-inputs/select-input-form/SelectInputForm';
import { selectSelectedAnimals } from '../../../../../app-service/slices/animalsDailyActionsSlice';
import {
    newDailyAction,
    useCreateDailyActionsMutation,
    useCreateDailyActionsWithoutResetFiltersMutation,
} from '../../../../../app-service/services/dailyActions';

type Props = {
    isGroup: boolean;
    resetHistory: () => void;
    num: number;
    formsIdLength: number;
    setFormsId: React.Dispatch<React.SetStateAction<string[]>>;
    idForm: string;
};

export const FormAddResearch = ({
    isGroup,
    resetHistory,
    num,
    setFormsId,
    formsIdLength,
    idForm,
}: Props) => {
    const { data: users } = useGetUsersQuery();
    const [createDailyActions] = useCreateDailyActionsMutation();
    const [createDailyActionsWithoutResetFilters] =
        useCreateDailyActionsWithoutResetFiltersMutation();
    const selectedAnimals = useAppSelector(selectSelectedAnimals);
    const [messageApi, contextHolder] = message.useMessage();
    const [form] = Form.useForm();
    const addAction = async (dataForm: FormTypeResearch) => {
        const data: newDailyAction[] = selectedAnimals.map((animal) => ({
            animalId: animal,
            type: 'Исследования',
            date: dayjs(dataForm.date).format('YYYY-MM-DD'),
            performedBy:
                usersOptions.find((option) => dataForm.performedBy === option.value)
                    ?.label || dataForm.performedBy,
            notes: dataForm.notes,
            materialType: dataForm.materialType,
            result: dataForm.result?.target.checked ? 'true' : 'false',
            researchName: dataForm.researchName,
        }));
        try {
            if (formsIdLength > 1) {
                await createDailyActionsWithoutResetFilters(data);
            } else {
                await createDailyActions(data);
            }
            form.resetFields();
            resetHistory();
            setFormsId((last) => {
                const filtered = last.filter((id) => id !== idForm);
                return filtered.length
                    ? filtered
                    : [Date.now().toString(36) + Math.random().toString(36).substring(2)];
            });
            messageApi.success('Исследование успешно добавлено');
        } catch {
            messageApi.error('Ошибка при добавлении исследования');
        }
    };

    const addForm = () => {
        setFormsId((last) => [
            ...last,
            Date.now().toString(36) + Math.random().toString(36).substring(2),
        ]);
    };

    const deleteForms = () => {
        setFormsId((last) => [last[0]]);
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
                <InputForm
                    label='Название исследования'
                    name='researchName'
                    placeholder='Введите название'
                    required
                />
                <DatePickerForm
                    name='date'
                    label='Дата забора материала'
                    required
                    defaultValue={dayjs()}
                />
                <SelectForm
                    label='Вид материала'
                    name='materialType'
                    options={optionsResearch}
                    styles={{ maxWidth: '475px' }}
                    required
                />
                <SelectInputForm
                    label='Кто проводил взятие'
                    name='performedBy'
                    placeholder='Введите ФИО'
                    options={usersOptions}
                    styles={{ maxWidth: '475px' }}
                />
                <Form.Item name='result' style={{ maxWidth: '475px', width: '100%' }}>
                    <CheckboxCustom
                        title='Положительный результат'
                        style={{ maxWidth: '475px' }}
                    />
                </Form.Item>
                <TextAreaForm
                    name='notes'
                    label='Примечания'
                    placeholder='Дополнительная информация'
                />
            </Flex>
            <Flex gap={16}>
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
                {formsIdLength > 1 && formsIdLength === num && (
                    <Button size='large' onClick={deleteForms}>
                        Сбросить все групповые формы
                    </Button>
                )}
                {num === 1 && isGroup && (
                    <Button size='large' onClick={addForm}>
                        Добавить еще одно групповое исследование
                    </Button>
                )}
            </Flex>
        </Form>
    );
};
