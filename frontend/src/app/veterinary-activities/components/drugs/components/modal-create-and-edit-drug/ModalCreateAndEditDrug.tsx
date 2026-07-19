import { Button, Drawer, Flex, Form } from 'antd';
import { InputForm } from '../../../../../../global-components/custom-inputs/form-inputs/input-form/InputForm';
import {
    IDrug,
    useCreateDrugMutation,
    useEditDrugMutation,
} from '../../../../services/drugs';
import { IDrugsTable } from '../../../../data/interface/IDrugsTable';
import { MessageInstance } from 'antd/es/message/interface';
import styles from './ModalCreateAndEditDrug.module.css';
import { useFuseSearch } from '../../../../../../hooks/useFuseSearch';

type Props = {
    open: boolean;
    setOpen: (open: boolean) => void;
    drug?: IDrugsTable;
    messageApi: MessageInstance;
    drugs?: IDrug[];
};

type DrugFormData = {
    name: string;
    substance: string;
    drugEliminatior?: string;
    shelfLife?: string;
    factory?: string;
};

export const ModalCreateAndEditDrug = ({
    open,
    setOpen,
    drug,
    messageApi,
    drugs = [],
}: Props) => {
    const [form] = Form.useForm();
    const [createDrug] = useCreateDrugMutation();
    const [editDrug] = useEditDrugMutation();

    const onSubmit = async (data: DrugFormData) => {
        try {
            if (drug) {
                await editDrug({ ...data, id: drug.id }).unwrap();
                messageApi.success('Препарат успешно изменен');
            } else {
                await createDrug(data).unwrap();
                messageApi.success('Препарат успешно создан');
            }
        } catch (e) {
            console.error(e);
            messageApi.error(
                drug ? 'Ошибка при изменении препарата' : 'Ошибка при создании препарата'
            );
        } finally {
            setOpen(false);
        }
    };

    const nameValue = Form.useWatch('name', form) || '';

    const filteredDrugs: IDrug[] = useFuseSearch<IDrug>(drugs, nameValue, {
        keys: ['name', 'substance', 'factory'],
        threshold: 0.3,
        ignoreLocation: true,
        minMatchCharLength: 2,
        minSearchLength: 2,
    });
    console.log(drugs);

    return (
        <>
            <Drawer
                title={drug ? 'Изменить препарат' : 'Добавить новый препарат'}
                open={open}
                footer={
                    <Flex gap={8}>
                        <Button onClick={() => setOpen(false)}>Отмена</Button>
                        <Button type='primary' onClick={() => form.submit()}>
                            Сохранить
                        </Button>
                    </Flex>
                }
                placement='left'
                onClose={() => setOpen(false)}
                width='400px'
            >
                <Form form={form} onFinish={onSubmit}>
                    <InputForm
                        label='Название'
                        name='name'
                        placeholder='Введите название препарата'
                        required
                        defaultValue={drug?.name}
                        styles={{
                            marginBottom: filteredDrugs.length > 0 ? '8px' : '16px',
                        }}
                    />
                    {filteredDrugs.length > 0 &&
                        filteredDrugs.length !== drugs.length && (
                            <Flex vertical gap={4} className={styles['similar-drugs']}>
                                <div className={styles['similar-drugs__title']}>
                                    Похожие препараты найдены:
                                </div>
                                <Flex vertical gap={4}>
                                    {filteredDrugs.slice(0, 3).map((drug) => (
                                        <div style={{ marginLeft: '8px' }} key={drug.id}>
                                            {drug.name}
                                        </div>
                                    ))}
                                </Flex>
                            </Flex>
                        )}

                    <InputForm
                        label='Действующее вещество'
                        name='substance'
                        placeholder='Введите действующее вещество'
                        required
                        defaultValue={drug?.substance}
                    />
                    <InputForm
                        label='Срок выведения'
                        name='drugEliminationPeriod'
                        placeholder='Напр.: 14 дней'
                        defaultValue={drug?.drugEliminatior}
                    />
                    <InputForm
                        label='Срок хранения'
                        name='shelfLife'
                        placeholder='Напр.: 2 года'
                        defaultValue={drug?.shelfLife}
                    />
                    <InputForm
                        label='Производитель'
                        name='factory'
                        placeholder='Введите производителя'
                        defaultValue={drug?.factory}
                    />
                </Form>
            </Drawer>
        </>
    );
};
