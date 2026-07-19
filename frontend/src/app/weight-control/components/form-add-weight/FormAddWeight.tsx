import { Button, Flex, Form } from 'antd';
import { DatePickerForm } from '../../../../global-components/custom-inputs/form-inputs/date-picker-form/DatePickerForm';
import dayjs from 'dayjs';
import { SelectForm } from '../../../../global-components/custom-inputs/form-inputs/select-form/SelectForm';
import { optionsMethodWeights } from '../../data/const/optionsSelect';
import { TextAreaForm } from '../../../../global-components/custom-inputs/form-inputs/text-area-form/TextAreaForm';
import { FormTypeWeight } from '../../data/types/formType';
import {
    newWeight,
    useCreateWeightMutation,
    useLazyGetLastWeightAnimalQuery,
} from '../../service/weightControl';
import { useAppDispatch, useAppSelector } from '../../../../app-service/hooks';
import {
    changeSelectedAnimal,
    selectSelectedAnimalWeightControl,
} from '../../service/weightControlSlice';
import { NumberInputForm } from '../../../../global-components/custom-inputs/form-inputs/number-input-form/NumberInputForm';
import { useEffect, useState } from 'react';
import { AiWeightFormValues } from '../../../ai-event-input/aiFormMapping';

type Props = {
    success: (message: string) => void;
    aiFormValues?: AiWeightFormValues;
};

export const FormAddWeight = ({ success, aiFormValues }: Props) => {
    const [form] = Form.useForm();
    const [createWeight] = useCreateWeightMutation();
    const selectedAnimal = useAppSelector(selectSelectedAnimalWeightControl);
    const dispatch = useAppDispatch();
    const [lastWeight, setLastWeight] = useState<string>('');
    const [getLastWeightAnimalQuery] = useLazyGetLastWeightAnimalQuery();

    const getLastWeightAnimal = async () => {
        const response = (await getLastWeightAnimalQuery(selectedAnimal)).data;
        if (response) {
            setLastWeight(`${response.weight} (${response.date})`);
        } else {
            setLastWeight('');
        }
    };

    const addWeight = async (dataForm: FormTypeWeight) => {
        const data: newWeight = {
            ...dataForm,
            animalId: selectedAnimal,
            date: dayjs(dataForm.date).format('YYYY-MM-DD'),
        };
        const response = (await createWeight(data)).data;
        if (response?.message) {
            success(response.message);
        }
        form.resetFields();
    };

    useEffect(() => {
        if (selectedAnimal) {
            getLastWeightAnimal();
        }
    }, [selectedAnimal]);

    useEffect(() => {
        if (!aiFormValues) return;

        if (aiFormValues.animalId) {
            dispatch(changeSelectedAnimal(aiFormValues.animalId));
        }

        form.setFieldsValue({
            date: aiFormValues.date,
            weight: aiFormValues.weight,
            method: aiFormValues.method,
        });
    }, [aiFormValues, dispatch, form]);

    return (
        selectedAnimal && (
            <>
                <Flex
                    style={{
                        padding: '15px 16px',
                        background: '#F5F5F5',
                        marginBottom: '24px',
                    }}
                    vertical
                >
                    {lastWeight && (
                        <Flex
                            style={{
                                boxShadow: '0px -1px 0px 0px #F0F0F0 inset',
                                width: '100%',
                                height: '38px',
                                background: '#FFFFFF',
                                padding: '0 16px',
                                marginBottom: '24px',
                            }}
                            align='center'
                        >
                            {`Последнее взвешивание: ${lastWeight}`}
                        </Flex>
                    )}
                    <Form
                        onFinish={addWeight}
                        form={form}
                        style={{ display: 'flex', flexWrap: 'wrap', columnGap: '16px' }}
                    >
                        <DatePickerForm
                            name='date'
                            label='Дата взвешивания'
                            required
                            defaultValue={dayjs()}
                            styles={{ maxWidth: '416px' }}
                        />
                        <NumberInputForm
                            label='Вес, кг'
                            name='weight'
                            defaultValue={400}
                            required
                        />

                        <SelectForm
                            label='Метод взвешивания'
                            name='method'
                            options={optionsMethodWeights}
                            defaultValue={optionsMethodWeights[0].value}
                            styles={{ maxWidth: '416px' }}
                            required
                        />

                        <TextAreaForm
                            name='note'
                            label='Примечание'
                            placeholder='Дополнительная информация'
                        />
                        <Button
                            type='primary'
                            size='large'
                            color='default'
                            variant='solid'
                            htmlType='submit'
                        >
                            Сохранить результат взвешивания
                        </Button>
                    </Form>
                </Flex>
            </>
        )
    );
};
