/* eslint-disable @typescript-eslint/no-explicit-any */
import { Alert, Button, DatePicker, Form, Input, Radio, Select } from 'antd';
import { InboxOutlined } from '@ant-design/icons';
import {
    IRadioGroup,
    RadioGroupButton,
} from '../../../../../global-components/custom-inputs/radio-group/RadioGroup';
import Dragger from 'antd/es/upload/Dragger';
import { InputLabel } from '../../../../../global-components/custom-inputs/input-label/InputLabel';
import styles from './ManualRegistration.module.css';
import { AdditionalInfoForm } from './additional-info-form/AdditionalInfoForm';
import {
    useGetAnimalGroupsQuery,
    useGetBreedQuery,
    useGetParentsAnimalsQuery,
    useRegistrationAnimalMutation,
} from '../../../services/registration-animal';
import { useEffect, useState } from 'react';
import { IAlert } from '../../../../../utils/alertType';
import { NetelFormRegister } from './netel-form/NetelForm';
import {
    formatDataForSelectInput,
    SelectDataType,
} from '../../../../../utils/formatting-data';
import dayjs from 'dayjs';
import { useWatch } from 'antd/es/form/Form';
import { IUser, useGetUsersQuery } from '../../../../../app-service/services/general';
import { AnimalFilters } from '../../../../../utils/animals';

const requiredRule = [{ required: true, message: 'Обязательное поле' }];
const animalsOptions: IRadioGroup = {
    title: 'Категория животного',
    options: [
        { label: 'Телка', value: 'Телка' },
        { label: 'Нетель', value: 'Нетель' },
        { label: 'Корова', value: 'Корова' },
        { label: 'Бычок', value: 'Бычок' },
        { label: 'Бык', value: 'Бык' },
    ],
};

export const ManualRegistrationForm = () => {
    const { data: users } = useGetUsersQuery();
    const { data, refetch } = useGetAnimalGroupsQuery();
    const [registerAnimalForm] = Form.useForm();
    const [selectedAnimalType, setSelectedAnimalType] = useState<string | undefined>('');
    const org_id: string = JSON.parse(localStorage.getItem('user') ?? '')?.organizationId;
    const [animalGroups, setAnimalGroups] = useState<SelectDataType[]>([]);
    const { data: breed } = useGetBreedQuery();
    const [breeds, setBreeds] = useState<SelectDataType[]>([]);
    const selectedBreed = useWatch('Breed', registerAnimalForm);
    const { data: mothers, refetch: refetchMothers } =
        useGetParentsAnimalsQuery('Корова');
    const { data: fathers, refetch: refetchFathers } = useGetParentsAnimalsQuery('Бык');

    const [registerAnimal, { isLoading }] = useRegistrationAnimalMutation();
    const [visibleAlert, setVisibleAlert] = useState(false);
    const [alert, setAlert] = useState<IAlert | null>(null);
    const [isExchangeOrPurchase, setIsExchangeOrPurchase] = useState(false);

    useEffect(() => {
        refetch();
        refetchMothers();
        refetchFathers();
    }, []);

    useEffect(() => {
        if (data) {
            setAnimalGroups(formatDataForSelectInput(data));
        }
        if (breed) {
            setBreeds([
                ...formatDataForSelectInput(breed),
                { value: '0', label: 'Другая' },
            ]);
        }
    }, [data, breed]);

    const handleRadioChange = (e: any) => {
        setSelectedAnimalType(e.target.value);
    };

    const handleOriginChange = (e: any) => {
        const value = e.target.value;
        if (value === 'Обмен' || value === 'Покупка') {
            setIsExchangeOrPurchase(true);
        } else {
            setIsExchangeOrPurchase(false);
        }
    };

    const onFinish = async (values: FormData) => {
        const formData = new FormData();
        const additionalInfo: Record<string, string> = {};
        const uuidRegex =
            /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
        Object.entries(values).forEach(([key, value]) => {
            if (uuidRegex.test(key)) {
                additionalInfo[key] = value;
            } else if (key === 'Photo' && value?.fileList) {
                formData.append('Photo', value.fileList[0]?.originFileObj);
            } else if (
                key === 'BirthDate' ||
                key === 'ExpectedCalvingDate' ||
                key === 'InseminationDate'
            ) {
                formData.append(key, dayjs(value).format('YYYY-MM-DD'));
            } else if (key === 'Breed') {
                const selectBreed = breeds.find((breed) => breed.value === value)?.label;

                if (selectBreed && selectBreed !== 'Другая') {
                    formData.append(key, selectBreed);
                }
            } else if (key === 'CustomBreed' && selectedBreed === '0') {
                formData.append('Breed', value);
            } else if (key === 'Technician') {
                formData.append(
                    key,
                    usersOptions.find((option) => value === option.value)?.label || value
                );
            } else {
                formData.append(key, String(value ?? ''));
            }
        });
        formData.append('AdditionalFields', JSON.stringify(additionalInfo));
        formData.append('OrganizationId', org_id);
        try {
            await registerAnimal(formData).unwrap();
            setAlert({
                message: 'Животное успешно зарегистрировано',
                type: 'success',
            });
            setVisibleAlert(true);
            setIsExchangeOrPurchase(false);
            registerAnimalForm.resetFields();
            setSelectedAnimalType('');
        } catch (err: any) {
            if (err?.data?.errorText) {
                setAlert({
                    message: err.data.errorText,
                    type: 'error',
                });
            } else {
                setAlert({
                    message: 'Что-то пошло не так. Попробуйте снова',
                    type: 'error',
                });
            }
            setVisibleAlert(true);
        }
        refetchMothers();
        refetchFathers();
    };
    const createOptions = (animals?: AnimalFilters[], users?: IUser[]) => [
        { label: '', value: '' },
        ...(animals?.map((item) => ({
            label: item.tagNumber,
            value: item.id,
        })) ||
            users?.map((user) => ({
                label: user.name,
                value: user.id,
            })) ||
            []),
    ];

    const mothersOptions = mothers ? createOptions(mothers) : [];
    const fathersOptions = fathers ? createOptions(fathers) : [];
    const usersOptions = users ? createOptions(undefined, users) : [];

    return (
        <>
            <Form form={registerAnimalForm} requiredMark={false} onFinish={onFinish}>
                <div>
                    <div>
                        <InputLabel label='Номер бирки/RFID' required={true} />
                        <Form.Item name='TagNumber' rules={requiredRule}>
                            <Input
                                className={styles['manual-register__input']}
                                placeholder='Введите номер бирки'
                            ></Input>
                        </Form.Item>
                    </div>
                    <div className={styles['manual-register__changed-form']}>
                        <div>
                            <InputLabel label='Порода' />
                            <Form.Item name='Breed'>
                                <Select
                                    options={breeds}
                                    className={styles['manual-register__input']}
                                    placeholder='Укажите породу'
                                ></Select>
                            </Form.Item>
                        </div>
                        {selectedBreed === '0' && (
                            <div>
                                <InputLabel label='Укажите породу' />
                                <Form.Item name='CustomBreed'>
                                    <Input
                                        className={styles['manual-register__input']}
                                        placeholder='Введите название породы'
                                    ></Input>
                                </Form.Item>
                            </div>
                        )}
                    </div>
                    <Form.Item
                        name='Type'
                        rules={requiredRule}
                        label={
                            <InputLabel label='Половозрастная группа' required={true} />
                        }
                        labelCol={{ span: 24 }}
                    >
                        <RadioGroupButton
                            onChange={handleRadioChange}
                            data={animalsOptions}
                        />
                    </Form.Item>
                </div>
                {selectedAnimalType === 'Нетель' && <NetelFormRegister />}
                <div className={styles['manual-register__changed-form']}>
                    <div>
                        <InputLabel label='Дата рождения' required={true} />
                        <Form.Item
                            rules={requiredRule}
                            name='BirthDate'
                            className='form-input_default'
                            initialValue={dayjs()}
                        >
                            <DatePicker
                                format='DD.MM.YYYY'
                                type='date'
                                className='form-input_default date'
                                placeholder='xx.xx.xxxx'
                            />
                        </Form.Item>
                    </div>
                    <div>
                        <InputLabel label='Происхождение' required={true} />
                        <Form.Item name='Origin' rules={requiredRule}>
                            <Radio.Group onChange={handleOriginChange}>
                                <div className={styles['manual-register__origin']}>
                                    <div className='radio-border'>
                                        <Radio value='Собственное'>Собственное</Radio>
                                    </div>
                                    <div className='radio-border'>
                                        <Radio value='Покупка'>Покупка</Radio>
                                    </div>
                                    <div className='radio-border'>
                                        <Radio value='Обмен'>Обмен</Radio>
                                    </div>
                                </div>
                            </Radio.Group>
                        </Form.Item>
                    </div>
                </div>
                {isExchangeOrPurchase && (
                    <div>
                        <InputLabel label='Место происхождения' />
                        <Form.Item name='OriginLocation'>
                            <Input
                                className={styles['manual-register__input']}
                                placeholder='Укажите место происхождения'
                            ></Input>
                        </Form.Item>
                    </div>
                )}
                <div className={styles['manual-register__changed-form']}>
                    <div>
                        <InputLabel label='Номер матери' />
                        <Form.Item name='MotherTag'>
                            <Select
                                showSearch
                                options={mothersOptions}
                                className={styles['manual-register__input']}
                                optionFilterProp='label'
                            ></Select>
                        </Form.Item>
                    </div>
                    <div>
                        <InputLabel label='Номер отца' />
                        <Form.Item name='FatherTag'>
                            <Select
                                showSearch
                                options={fathersOptions}
                                className={styles['manual-register__input']}
                                optionFilterProp='label'
                            ></Select>
                        </Form.Item>
                    </div>
                </div>
                <div>
                    <InputLabel label='Группа содержания' />
                    <Form.Item name='GroupId'>
                        <Select
                            options={animalGroups}
                            className={styles['manual-register__input']}
                        ></Select>
                    </Form.Item>
                </div>
                <AdditionalInfoForm />
                <div>
                    <InputLabel label='Фотография животного' />
                    <Form.Item name='Photo' className={styles['manual-register__input']}>
                        <Dragger beforeUpload={() => false} accept='.jpg,.jpeg,.png'>
                            <p className='ant-upload-drag-icon'>
                                <InboxOutlined />
                            </p>
                            <p className='ant-upload-text'>
                                Выберите или перетащите файл
                            </p>
                            <p className='ant-upload-hint'>
                                Максимальный размер изображения 200Mb. Формат JPG/JPEG/PNG
                            </p>
                        </Dragger>
                    </Form.Item>
                </div>
                {visibleAlert && alert && (
                    <Alert
                        className={styles['manual-register__alert']}
                        message={alert.message}
                        type={alert.type}
                        showIcon
                    />
                )}
                <Button htmlType='submit' type='primary' loading={isLoading}>
                    Зарегистрировать животное
                </Button>
            </Form>
        </>
    );
};
