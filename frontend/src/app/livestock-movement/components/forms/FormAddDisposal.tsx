import { Button, Flex, Form, message } from 'antd';
import { DatePickerForm } from '../../../../global-components/custom-inputs/form-inputs/date-picker-form/DatePickerForm';
import {
    newDailyAction,
    useCreateDailyActionsMutation,
} from '../../../../app-service/services/dailyActions';
import { useAppSelector } from '../../../../app-service/hooks';
import {
    selectIsGroup,
    selectSelectedAnimals,
} from '../../../../app-service/slices/animalsDailyActionsSlice';
import dayjs from 'dayjs';
import { FormTypeDisposal } from '../../data/types/FormTypes';
import { optionsDisposal } from '../../data/const/optionsSelect';
import { SelectForm } from '../../../../global-components/custom-inputs/form-inputs/select-form/SelectForm';
import styles from '../../styles/form-styles.module.css';
import { useGetUsersQuery } from '../../../../app-service/services/general';
import { SelectInputForm } from '../../../../global-components/custom-inputs/form-inputs/select-input-form/SelectInputForm';

type Props = {
    resetHistory: () => void;
};

export const FormAddDisposal = ({ resetHistory }: Props) => {
    const { data: users } = useGetUsersQuery();
    const isGroup = useAppSelector(selectIsGroup);
    const [createDailyActions] = useCreateDailyActionsMutation();
    const selectedAnimals = useAppSelector(selectSelectedAnimals);
    const [form] = Form.useForm();
    const [messageApi, contextHolder] = message.useMessage();
    const addAction = async (dataForm: FormTypeDisposal) => {
        const data: newDailyAction[] = selectedAnimals.map((animal) => ({
            animalId: animal,
            type: 'Выбытие',
            date: dayjs(dataForm.dateCulling).format('YYYY-MM-DD'),
            performedBy:
                usersOptions.find((option) => dataForm.name === option.value)?.label ||
                dataForm.name,
            subtype: dataForm.reason,
        }));
        try{
            await createDailyActions(data);
            form.resetFields();
            resetHistory();
            messageApi.success('Выбытие успешно сохранено');
        }catch{
            messageApi.error('Ошибка при сохранении выбытия');
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
                <div style={{ width: '100%' }}>
                    <DatePickerForm
                        name='dateCulling'
                        label='Дата выбытия'
                        required
                        defaultValue={dayjs()}
                    />
                </div>
                <SelectInputForm
                    label='Кто проводил выбытие'
                    name='name'
                    placeholder='Введите ФИО'
                    options={usersOptions}
                    showSearch
                    styles={{ maxWidth: '475px' }}
                />
                <SelectForm
                    label='Причина выбытия'
                    name='reason'
                    options={optionsDisposal}
                    styles={{ maxWidth: '475px' }}
                    placeholder='Выберите причину'
                    required
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
