import { Button, Flex, Form, Input, Modal, Select } from 'antd';
import { InputLabel } from '../../../../../../global-components/custom-inputs/input-label/InputLabel';
import { CloseCircleOutlined } from '@ant-design/icons';
import styles from './EditGroupModal.module.css';
import {
    IGroup,
    InfrastructureDataItem,
    useGetGroupsTypesQuery,
} from '../../../../services/infrastructure-service';
import TextArea from 'antd/es/input/TextArea';
import { useEffect, useState } from 'react';
import { formatDataForSelectInput } from '../../../../../../utils/data-functions/formatting-data';
import { SelectDataType } from '../../../../../../utils/selectDataType';

export const EditGroupModal = ({
    open,
    onClose,
    onSave,
    onDelete,
    initialValue,
    isLoading = false,
}: {
    open: boolean;
    onClose: () => void;
    onSave: (values: IGroup) => void;
    onDelete: () => void;
    initialValue: InfrastructureDataItem;
    isLoading?: boolean;
}) => {
    const { data } = useGetGroupsTypesQuery();
    const [groupsTypes, setGroupsTypes] = useState<SelectDataType[]>([]);

    useEffect(() => {
        if (data) {
            setGroupsTypes(formatDataForSelectInput(data));
        }
    }, [data]);

    const openConfirmModal = () => {
        onClose();
        onDelete();
    };

    return (
        <Modal
            className={styles['edit-group-modal']}
            open={open}
            title='Редактирование или удаление'
            onCancel={onClose}
            footer={null}
            width='464px'
            closeIcon={
                <CloseCircleOutlined
                    style={{ fontSize: 24, color: 'rgba(0, 0, 0, 0.45)' }}
                />
            }
        >
            <Form onFinish={onSave}>
                <div>
                    <InputLabel label='Название группы' />
                    <Form.Item name='Name'>
                        <Input
                            className={styles['edit-group-modal__input']}
                            placeholder='Введите название'
                            defaultValue={initialValue.name}
                        ></Input>
                    </Form.Item>
                </div>
                <div>
                    <InputLabel label='Тип группы' />
                    <Form.Item name='TypeId'>
                        <Select
                            options={groupsTypes}
                            className={styles['edit-group-modal__input']}
                            placeholder='Выберите тип'
                            defaultValue={initialValue.typeId}
                        ></Select>
                    </Form.Item>
                </div>
                <div>
                    <InputLabel label='Расположение' />
                    <Form.Item name='Location'>
                        <Input
                            className={styles['edit-group-modal__input']}
                            placeholder='Укажите расположение'
                            defaultValue={initialValue.location}
                        ></Input>
                    </Form.Item>
                </div>
                <div>
                    <InputLabel label='Описание' />
                    <Form.Item
                        name='Description'
                        className={styles['edit-group-modal__input']}
                    >
                        <TextArea
                            rows={4}
                            placeholder='Дополнительная информация'
                            defaultValue={initialValue.description}
                        ></TextArea>
                    </Form.Item>
                </div>
                <Flex className={styles['edit-group-modal__button-container']}>
                    <Button
                        className={styles['edit-group-modal__button']}
                        size='large'
                        onClick={() => openConfirmModal()}
                    >
                        Удалить
                    </Button>
                    <Button
                        className={styles['edit-group-modal__button']}
                        size='large'
                        htmlType='submit'
                        type='primary'
                        loading={isLoading}
                    >
                        Сохранить
                    </Button>
                </Flex>
            </Form>
        </Modal>
    );
};
