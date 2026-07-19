import { Button, Flex, Form, message } from 'antd';
import { DatePickerForm } from '../../../../../global-components/custom-inputs/form-inputs/date-picker-form/DatePickerForm';
import { TextAreaForm } from '../../../../../global-components/custom-inputs/form-inputs/text-area-form/TextAreaForm';
import { InputForm } from '../../../../../global-components/custom-inputs/form-inputs/input-form/InputForm';
import { useAppSelector } from '../../../../../app-service/hooks';
import dayjs from 'dayjs';
import { FormTypeTreatment } from '../../../data/types/FormTypes';
import styles from '../../../styles/form-styles.module.css';
import { useGetUsersQuery } from '../../../../../app-service/services/general';
import { SelectInputForm } from '../../../../../global-components/custom-inputs/form-inputs/select-input-form/SelectInputForm';
import {
    IRequestCreateActionsWithMedicine,
    useCreateDailyActionsWithMedicineMutation,
    useCreateDailyActionsWithoutResetFiltersWithMedicineMutation,
} from '../../../../../app-service/services/dailyActions';
import { selectSelectedAnimals } from '../../../../../app-service/slices/animalsDailyActionsSlice';
import { optionsTypesTreatment } from '../../../data/const/optionsSelect';
import { SelectForm } from '../../../../../global-components/custom-inputs/form-inputs/select-form/SelectForm';
import { useGetDrugsQuery } from '../../../services/drugs';
import { useEffect } from 'react';
import { AiTreatmentFormValues } from '../../../../ai-event-input/aiFormMapping';

type Props = {
    resetHistory: () => void;
    isGroup: boolean;
    num: number;
    formsIdLength: number;
    setFormsId: React.Dispatch<React.SetStateAction<string[]>>;
    idForm: string;
    aiFormValues?: AiTreatmentFormValues;
};

export const FormAddTreatment = ({
    resetHistory,
    isGroup,
    num,
    formsIdLength,
    setFormsId,
    idForm,
    aiFormValues,
}: Props) => {
    const { data: users } = useGetUsersQuery();
    const [createDailyActions] = useCreateDailyActionsWithMedicineMutation();
    const [createDailyActionsWithoutResetFilters] =
        useCreateDailyActionsWithoutResetFiltersWithMedicineMutation();
    const { data: drugs } = useGetDrugsQuery();
    const selectedAnimals = useAppSelector(selectSelectedAnimals);
    const [form] = Form.useForm();
    const [messageApi, contextHolder] = message.useMessage();
    const addAction = async (dataForm: FormTypeTreatment) => {
        const data: IRequestCreateActionsWithMedicine = {
            animalIds: selectedAnimals,
            actions: [
                {
                    type: 'Обработка',
                    subtype: dataForm.subtype,
                    performedBy:
                        usersOptions.find((option) => dataForm.name === option.value)
                            ?.label || dataForm.name,
                    date: dayjs(dataForm.dateStartTreatment).format('YYYY-MM-DD'),
                    medicine: dataForm.medicine,
                    dose: dataForm.dose,
                    notes: dataForm.note,
                    nextDate: dataForm.dateNextTreatment
                        ? dayjs(dataForm.dateNextTreatment).format('YYYY-MM-DD')
                        : null,
                    result: dataForm.diagnosis || '',
                },
            ],
        };
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
            messageApi.success('Обработка успешно добавлена');
        } catch {
            messageApi.error('Ошибка при добавлении обработки');
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

    const selectedMedicineId = Form.useWatch('medicine', form);

    useEffect(() => {
        if (!selectedMedicineId || !drugs) return;

        const selectedDrug = drugs.find((drug) => drug.id === selectedMedicineId);
        if (!selectedDrug) return;

        if (selectedDrug.drugEliminatior) {
            form.setFieldsValue({ withdrawalPeriod: selectedDrug.drugEliminatior });
        }
    }, [selectedMedicineId, drugs, form]);

    useEffect(() => {
        if (!aiFormValues) return;

        form.setFieldsValue({
            dateStartTreatment: aiFormValues.dateStartTreatment,
            name: aiFormValues.name,
            subtype: aiFormValues.subtype,
            medicine: aiFormValues.medicine,
            diagnosis: aiFormValues.diagnosis,
            dose: aiFormValues.dose,
            dateNextTreatment: aiFormValues.dateNextTreatment,
        });
    }, [aiFormValues, form]);

    return (
        <Form onFinish={addAction} form={form}>
            {contextHolder}
            <Flex className={styles['form-body']} wrap>
                <DatePickerForm
                    name='dateStartTreatment'
                    label='Дата обработки'
                    required
                    defaultValue={dayjs()}
                />
                <SelectInputForm
                    label='Кто проводил лечение'
                    name='name'
                    placeholder='Введите ФИО'
                    options={usersOptions}
                    styles={{ maxWidth: '475px' }}
                />
                <SelectForm
                    label='Тип обработки'
                    name='subtype'
                    options={optionsTypesTreatment}
                    styles={{ maxWidth: '475px' }}
                    defaultValue='Вакцинация'
                    required
                />
                <SelectInputForm
                    label='Препарат'
                    name='medicine'
                    options={
                        drugs?.map((drug) => ({
                            label: drug.name,
                            value: drug.id,
                        })) || []
                    }
                    placeholder='Выберите препарат'
                    styles={{ maxWidth: '475px' }}
                    required
                />
                <Form.Item
                    noStyle
                    shouldUpdate={(prev, next) => prev.subtype !== next.subtype}
                >
                    {() =>
                        form.getFieldValue('subtype') === 'Лечение' ? (
                            <InputForm
                                label='Диагноз'
                                name='diagnosis'
                                placeholder='Укажите диагноз'
                                required
                                styles={{ maxWidth: '100%' }}
                            />
                        ) : null
                    }
                </Form.Item>
                <InputForm label='Доза' name='dose' placeholder='Напр.: 2 мл' required />
                <InputForm
                    label='Срок выведения'
                    name='withdrawalPeriod'
                    placeholder='Напр.: 14 дней'
                />

                <DatePickerForm
                    name='dateNextTreatment'
                    label='Дата следующей обработки'
                    required
                    defaultValue={dayjs()}
                />
                <TextAreaForm
                    name='note'
                    label='Примечание'
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
                        Добавить еще одно групповое действие
                    </Button>
                )}
            </Flex>
        </Form>
    );
};
