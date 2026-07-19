import { Button, DatePicker, Flex, Form, Input, message, Select } from 'antd';
import { AnimalDetail } from '../../data/interfaces/animal-details';
import { useEffect, useState } from 'react';
import styles from './BaseInfo.module.css';
import { EditOutlined, SaveOutlined } from '@ant-design/icons';
import { AdditionalInfoForm } from '../../../register-animal/components/forms/manual-registration-form/additional-info-form/AdditionalInfoForm';
import { InputLabel } from '../../../../global-components/custom-inputs/input-label/InputLabel';
import {
    useGetAnimalGroupsQuery,
    useGetBreedQuery,
} from '../../../register-animal/services/registration-animal';
import { SelectDataType } from '../../../../utils/selectDataType';
import { requiredRule } from '../../../../const/req-rule';
import { animalCat, originTypes } from '../../../../const/animal-cat';
import dayjs from 'dayjs';
import { useWatch } from 'antd/es/form/Form';
import regStyles from '../../../register-animal/components/forms/manual-registration-form/ManualRegistration.module.css';
import { formatDataForSelectInput } from '../../../../utils/formatting-data';
import { useUpdateBaseInfoMutation } from '../../services/animal-card';
import { isErrorType } from '../../../../utils/errorType';
import { IBaseInfo } from '../../data/interfaces/base-info';
import { optionsStatus } from '../../../../const/animal-status -options';
import { isDevMode } from '../../../../utils/dev-mode';

interface InfoItem {
    [key: string]: string | null;
}

export const BaseInfo = ({
    animal,
    updateAnimal,
}: {
    animal: AnimalDetail;
    updateAnimal: () => Promise<void>;
}) => {
    const [baseInfo, setBaseInfo] = useState<InfoItem[]>([]);
    const [additionalIds, setAdditionalIds] = useState<InfoItem[]>([]);
    const [leftColumn, setLeftColumn] = useState<InfoItem[]>([]);
    const [rightColumn, setRightColumn] = useState<InfoItem[]>([]);
    const [isEdit, setIsEdit] = useState<boolean>(false);
    const [registerAnimalForm] = Form.useForm();
    const { data } = useGetAnimalGroupsQuery();
    const [updateInfo] = useUpdateBaseInfoMutation();
    const org_id: string = JSON.parse(localStorage.getItem('user') ?? '')?.organizationId;
    const [animalGroups, setAnimalGroups] = useState<SelectDataType[]>([]);
    const { data: breed } = useGetBreedQuery();
    const [breeds, setBreeds] = useState<SelectDataType[]>([]);
    const selectedBreed = useWatch('Breed', registerAnimalForm);
    const [messageApi, contextHolder] = message.useMessage();

    useEffect(() => {
        fillInfo();
        parseAdditionalInfo();
        setFormInitialValues();
    }, [animal]);

    useEffect(() => {
        setLeftColumn(baseInfo.slice(0, 6));
        setRightColumn(baseInfo.slice(6));
    }, [baseInfo]);

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

    const setFormInitialValues = () => {
        if (animal) {
            const initialValues = {
                tagNumber: animal.tagNumber,
                breed: animal.breed,
                type: animal.type,
                birthDate: animal.birthDate ? dayjs(animal.birthDate) : dayjs(),
                origin: animal.origin,
                originLocation: animal.originLocation,
                groupId: animal.groupId,
                status: animal.status,
                dateOfDisposal: animal.dateOfDisposal,
                reasonOfDisposal: animal.reasonOfDisposal,
            };

            registerAnimalForm.setFieldsValue(initialValues);

            if (animal.identificationDataJson) {
                try {
                    const parsedAdditionalIds = JSON.parse(animal.identificationDataJson);
                    registerAnimalForm.setFieldsValue(parsedAdditionalIds);
                } catch (error) {
                    if (isDevMode()) {
                        console.error(
                            'Ошибка при парсинге identificationDataJson:',
                            error
                        );
                    }
                }
            }
        }
    };

    const fillInfo = () => {
        setBaseInfo([
            { Категория: animal.type },
            { Статус: animal.status },
            {
                'Дата и причины выбытия': `${animal.dateOfDisposal ?? 'Не указано'}, ${
                    animal.reasonOfDisposal ?? 'Не указано'
                }`,
            },
            { Группа: animal.groupName },
            { 'Дата рождения': animal.birthDate },
            { Порода: animal.breed },
            { Происхождение: animal.origin },
            { 'Место происхождения': animal.originLocation },
            { Мать: animal.motherTagNumber },
            { Отец: animal.fatherTagNumbers ? animal.fatherTagNumbers.join(', ') : null },
        ]);
    };

    const parseAdditionalInfo = () => {
        try {
            const rawJson = animal.identificationDataJson;
            if (!rawJson) return;

            const parsed = JSON.parse(rawJson) as Record<string, string>;
            const entries = Object.entries(parsed).map(([key, value]) => ({
                [key]: value,
            }));
            setAdditionalIds(entries);
        } catch (error) {
            if (isDevMode()) {
                console.error('Ошибка при парсинге identificationDataJson:', error);
            }
        }
    };

    const onFinish = async () => {
        const values = registerAnimalForm.getFieldsValue();
        const additionalInfo: Record<string, string | null> = {};
        const uuidRegex =
            /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
        const requestData: IBaseInfo = {
            orgId: org_id,
            id: animal.id,
            motherTagNumber: null,
            motherId: null,
            fatherIds: null,
            fatherTagNumber: null,
            dateOfReceipt: null,
        };

        Object.entries(values).forEach(([key, value]) => {
            const stringValue = value as string;
            if (uuidRegex.test(key)) {
                additionalInfo[key] = stringValue ?? null;
            } else if (key === 'birthDate') {
                requestData[key] = dayjs(stringValue).format('YYYY-MM-DD');
            } else if (key === 'breed') {
                if (value && value !== 'Другая') {
                    requestData['breed'] = stringValue;
                }
            } else if (key === 'customBreed' && selectedBreed === '0') {
                requestData['breed'] = stringValue;
            } else {
                requestData[key] = value !== '' ? value : null;
            }
        });

        requestData.identificationData = additionalInfo;

        try {
            await updateInfo(requestData).unwrap();
            messageApi.open({
                type: 'success',
                content: 'Изменения сохранены',
            });
            registerAnimalForm.resetFields();
            updateAnimal();
            setIsEdit(false);
        } catch (err) {
            if (isErrorType(err) && err?.data?.errorText) {
                messageApi.open({
                    type: 'error',
                    content: err.data.errorText,
                });
            } else {
                messageApi.open({
                    type: 'error',
                    content: 'Сервис временно не доступен. Попробуйте позже',
                });
            }
        }
    };

    const handleEditToggle = () => {
        if (!isEdit) {
            setFormInitialValues();
        }
        setIsEdit(!isEdit);
    };

    const handleCancel = () => {
        setFormInitialValues();
        setIsEdit(false);
    };

    return (
        <Flex vertical className={`${styles['base-info__wrapper']} form-additional`}>
            {contextHolder}
            <Flex justify='space-between'>
                <h2 className='form-title'>Информация о животном №{animal.tagNumber}</h2>
                {isEdit ? (
                    <Flex gap={16}>
                        <Button
                            type='primary'
                            htmlType='submit'
                            icon={<SaveOutlined />}
                            onClick={onFinish}
                        >
                            Сохранить
                        </Button>
                        <Button onClick={handleCancel}>Отменить</Button>
                    </Flex>
                ) : (
                    <Button icon={<EditOutlined />} onClick={handleEditToggle}>
                        Редактировать
                    </Button>
                )}
            </Flex>
            {!isEdit ? (
                <div>
                    <div className={styles.gridContainer}>
                        {baseInfo.length &&
                            [leftColumn, rightColumn].map((column, colIdx) => (
                                <div key={colIdx} className={styles.gridColumn}>
                                    {column.map((item, rowIdx) => {
                                        const key = Object.keys(item)[0];
                                        const value = item[key];
                                        return (
                                            <div key={rowIdx} className={styles.gridRow}>
                                                <span className={styles.label}>
                                                    {key}:
                                                </span>
                                                <span className={styles.value}>
                                                    {value || 'Не указано'}
                                                </span>
                                            </div>
                                        );
                                    })}
                                </div>
                            ))}
                    </div>
                    <h3>Дополнительные идентификаторы</h3>
                    <div className={styles['base-info__add-container']}>
                        {additionalIds.length > 0 ? (
                            additionalIds.map((item, index) => {
                                const key = Object.keys(item)[0];
                                const value = item[key];

                                return (
                                    <div key={index + key} className={styles.gridRow}>
                                        <span className={styles.label}>{key}:</span>
                                        <span className={styles.value}>
                                            {value || 'Не указано'}
                                        </span>
                                    </div>
                                );
                            })
                        ) : (
                            <span>Нет данных</span>
                        )}
                    </div>
                </div>
            ) : (
                <Form
                    form={registerAnimalForm}
                    requiredMark={false}
                    onFinish={onFinish}
                    initialValues={{
                        TagNumber: animal?.tagNumber,
                        BirthDate: animal?.birthDate ? dayjs(animal.birthDate) : dayjs(),
                    }}
                    className={styles['base-info__form-wrapper']}
                >
                    <div>
                        <InputLabel label='Номер бирки/RFID' required={true} />
                        <Form.Item
                            className={regStyles['manual-register__changed-form']}
                            name='tagNumber'
                            rules={requiredRule}
                        >
                            <Input
                                className={regStyles['manual-register__input']}
                                placeholder='Введите номер бирки'
                            ></Input>
                        </Form.Item>
                    </div>
                    <div className={regStyles['manual-register__changed-form']}>
                        <div>
                            <InputLabel label='Порода' />
                            <Form.Item name='breed'>
                                <Select
                                    options={breeds}
                                    className={regStyles['manual-register__input']}
                                    placeholder='Укажите породу'
                                ></Select>
                            </Form.Item>
                        </div>
                        {selectedBreed === '0' && (
                            <div>
                                <InputLabel label='Укажите породу' />
                                <Form.Item name='customBreed'>
                                    <Input
                                        className={regStyles['manual-register__input']}
                                        placeholder='Введите название породы'
                                    ></Input>
                                </Form.Item>
                            </div>
                        )}
                    </div>
                    <div className={regStyles['manual-register__changed-form']}>
                        <div>
                            <InputLabel label='Статус' required={true} />
                            <Form.Item name='status' rules={requiredRule}>
                                <Select
                                    showSearch
                                    options={optionsStatus}
                                    className={regStyles['manual-register__input']}
                                ></Select>
                            </Form.Item>
                        </div>
                        <div>
                            <InputLabel label='Дата выбытия' />
                            <Form.Item name='dateOfDisposal'>
                                <DatePicker
                                    format='DD.MM.YYYY'
                                    type='date'
                                    className='form-input_default date'
                                    placeholder='xx.xx.xxxx'
                                />
                            </Form.Item>
                        </div>
                    </div>
                    <div className={regStyles['manual-register__changed-form']}>
                        <div>
                            <InputLabel label='Причины выбытия' />
                            <Form.Item name='reasonOfDisposal'>
                                <Input
                                    className={regStyles['manual-register__input']}
                                    placeholder='Укажите причину выбытия'
                                ></Input>
                            </Form.Item>
                        </div>
                        <div>
                            <InputLabel label='Половозрастная группа' required={true} />
                            <Form.Item name='type' rules={requiredRule}>
                                <Select
                                    options={animalCat}
                                    className={regStyles['manual-register__input']}
                                    placeholder='Укажите породу'
                                ></Select>
                            </Form.Item>
                        </div>
                    </div>
                    <div className={regStyles['manual-register__changed-form']}>
                        <div>
                            <InputLabel label='Происхождение' required={true} />
                            <Form.Item name='origin' rules={requiredRule}>
                                <Select
                                    showSearch
                                    options={originTypes}
                                    className={regStyles['manual-register__input']}
                                ></Select>
                            </Form.Item>
                        </div>
                        <div>
                            <InputLabel label='Место происхождения' />
                            <Form.Item name='originLocation'>
                                <Input
                                    className={regStyles['manual-register__input']}
                                    placeholder='Укажите место происхождения'
                                ></Input>
                            </Form.Item>
                        </div>
                    </div>
                    <div className={regStyles['manual-register__changed-form']}>
                        <div>
                            <InputLabel label='Дата рождения' required={true} />
                            <Form.Item
                                rules={requiredRule}
                                name='birthDate'
                                className='form-input_default'
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
                            <InputLabel label='Группа содержания' />
                            <Form.Item name='groupId'>
                                <Select
                                    options={animalGroups}
                                    className={regStyles['manual-register__input']}
                                ></Select>
                            </Form.Item>
                        </div>
                    </div>
                    <AdditionalInfoForm
                        withoutContainer={true}
                        form={registerAnimalForm}
                        initialValues={
                            animal.identificationDataJson
                                ? JSON.parse(animal.identificationDataJson)
                                : {}
                        }
                    />
                </Form>
            )}
        </Flex>
    );
};
