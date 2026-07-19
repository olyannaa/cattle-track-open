/* eslint-disable @typescript-eslint/no-explicit-any */
import { Alert, Button, Form, List } from 'antd';
import Dragger from 'antd/es/upload/Dragger';
import { InboxOutlined } from '@ant-design/icons';
import styles from './ImportSelexForm.module.css';
import { useRegistrationAnimalFromCSVMutation } from '../../../services/registration-animal';
import { useState } from 'react';
import { IAlert } from '../../../../../utils/alertType';

const data = [
    'Инвентарный номер (обязательное поле, всегда первая колонка)',
    'Дата рождения',
    'Дата поступления',
    'Дата выбытия',
    'Причина выбытия',
    'Расход',
    'Живая масса при выбытии',
    'Последнее взвешивание (дата)',
    'Последнее взвешивание (вес)',
    'Пол (1-бычок, 2-телка, 3-бык, 4-корова)',
    'Статус',
    'Инв.№ предка - O (отец)',
    'Инв.№ предка - M (мать)',
    'Порода',
    'Хозяйство рождения',
    'Область рождения',
    'Гос-во рождения',
];

export const ImportSelexForm = () => {
    const [registerAnimalFromCsv, { isLoading }] =
        useRegistrationAnimalFromCSVMutation();
    const [registerAnimalCSVForm] = Form.useForm();
    const [visibleAlert, setVisibleAlert] = useState(false);
    const [alert, setAlert] = useState<IAlert | null>(null);

    const registrationAnimalCsv = async (dataForm: {
        file: {
            file: File;
            fileList: any;
        };
    }) => {
        const registerData: FormData = new FormData();
        registerData.append('file', dataForm.file.fileList?.[0]?.originFileObj);
        try {
            await registerAnimalFromCsv(registerData).unwrap();
            setAlert({
                message: 'Животное успешно зарегистрировано',
                type: 'success',
            });
            setVisibleAlert(true);
            registerAnimalCSVForm.resetFields();
        } catch (err: any) {
            if (err?.ErrorText) {
                setAlert({
                    message: err.ErrorText,
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
    };

    return (
        <div className={styles['import-selex']}>
            <Form onFinish={registrationAnimalCsv} form={registerAnimalCSVForm}>
                <h2>Импорт животных из файла выгрузки СЕЛЭКС</h2>
                <Form.Item
                    name='file'
                    className={styles['import-selex__dragger']}
                    rules={[{ required: true, message: 'Обязательное поле' }]}
                >
                    <Dragger beforeUpload={() => false} accept='.csv'>
                        <p className='ant-upload-drag-icon'>
                            <InboxOutlined />
                        </p>
                        <p className='ant-upload-text'>
                            Выберите или перетащите файл
                        </p>
                        <p className='ant-upload-hint'>
                            Максимальный размер файла 200Mb. Формат CSV
                        </p>
                    </Dragger>
                </Form.Item>
                {visibleAlert && alert && (
                    <Alert
                        className='alert'
                        message={alert.message}
                        type={alert.type}
                        showIcon
                    />
                )}
                <Button htmlType='submit' type='primary' loading={isLoading}>
                    Импортировать данные
                </Button>
            </Form>
            <div>
                <h3 className={styles['import-selex__additional-title']}>
                    Файл должен содержать следующие колонки:
                </h3>
                <div className={styles['import-selex__additional-container']}>
                    <h3 className={styles['import-selex__additional-subtitle']}>
                        Дополнительные способы идентификации
                    </h3>
                    <p>
                        Вы можете добавить любые дополнительные колонки для
                        идентификации животных (например, УНСМ, Электронная
                        метка, и др.). Эти поля будут автоматически созданы в
                        системе как дополнительные поля идентификации.
                    </p>
                </div>
                <List
                    className={styles['import-selex__additional-list']}
                    bordered
                    dataSource={data}
                    renderItem={(item) => <List.Item>{item}</List.Item>}
                />
            </div>
        </div>
    );
};
