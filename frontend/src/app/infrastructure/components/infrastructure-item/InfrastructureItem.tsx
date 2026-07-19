/* eslint-disable @typescript-eslint/no-explicit-any */
import { Button, Flex, message } from 'antd';
import { InfrastructureTypes } from '../../data/enums/infrastructureTypes';
import {
    IGroup,
    InfrastructureDataItem,
    useEditGroupMutation,
} from '../../services/infrastructure-service';
import { useEffect, useState } from 'react';
import { EditGroupModal } from '../groups/components/edit-group-modal/EditGroupModal';
import { ConfirmModal } from '../../../../global-components/confirm-modal/ConfirmModal';
import { isErrorType } from '../../../../utils/errorType';
import styles from './InfrastructureItem.module.css';

export const InfrastructureItem = ({
    item,
    partInfrastructure,
    onDelete,
}: {
    item: InfrastructureDataItem;
    partInfrastructure: InfrastructureTypes;
    onDelete: (id: string) => any;
}) => {
    const firstLetter: string = item.name[0].toUpperCase();
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [isConfirmOpen, setIsConfirmOpen] = useState(false);
    const [editGroup, { isLoading }] = useEditGroupMutation();
    const [messageApi, contextHolder] = message.useMessage();
    const [confirmTitle, setConfirmTitle] = useState('');
    const [successTitle, setSuccessTitle] = useState('');

    useEffect(() => {
        switch (partInfrastructure) {
            case InfrastructureTypes.group:
                setConfirmTitle('Вы действительно хотите удалить группу?');
                setSuccessTitle('Группа успешно удалена');
                break;
            case InfrastructureTypes.identificationField:
                setConfirmTitle('Вы действительно хотите удалить поле?');
                setSuccessTitle('Поле идентификации успешно удалено');
                break;
            case InfrastructureTypes.typeGroup:
                setConfirmTitle('Вы действительно хотите удалить тип группы?');
                setSuccessTitle('Тип группы успешно удалён');
                break;
        }
    }, [partInfrastructure]);

    const openEditModal = () => {
        setIsModalOpen(true);
    };

    const closeEditModal = () => {
        setIsModalOpen(false);
    };

    const openConfirmModal = () => {
        setIsConfirmOpen(true);
    };

    const closeConfirmModal = () => {
        setIsConfirmOpen(false);
    };

    const handleSave = async (values: IGroup) => {
        const editedGroup: IGroup = { id: item.id, ...values };
        try {
            editGroup(editedGroup).unwrap();
            messageApi.open({
                type: 'success',
                content: 'Изменения успешно сохранены',
            });
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
        closeEditModal();
    };

    const handleDelete = async () => {
        try {
            await onDelete(item.id).unwrap();
            messageApi.open({
                type: 'success',
                content: successTitle,
            });
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
        closeConfirmModal();
    };

    return (
        <Flex className={styles['item-container']}>
            {contextHolder}
            <Flex className={styles['item-container__info']}>
                <div className={styles['item__icon']}>
                    <div className={styles['item__letter']}>{firstLetter}</div>
                </div>
                <div>
                    <h3>{item.name}</h3>
                    {item.description && (
                        <div className={styles['item-desc']}>
                            {item.description}
                        </div>
                    )}
                </div>
            </Flex>
            <Flex className={styles['item-buttons__container']}>
                {partInfrastructure === InfrastructureTypes.group && (
                    <Flex style={{ alignItems: 'center' }}>
                        <Button
                            type='link'
                            onClick={() => openEditModal()}
                            style={{ padding: 0 }}
                        >
                            Редактировать
                        </Button>
                        <div
                            style={{
                                height: '14px',
                                width: '1px',
                                backgroundColor: '#e0e0e0',
                                margin: '0 8px',
                            }}
                        />
                    </Flex>
                )}
                <Button
                    type='link'
                    onClick={() => openConfirmModal()}
                    style={{ padding: 0 }}
                >
                    Удалить
                </Button>
            </Flex>
            <EditGroupModal
                open={isModalOpen}
                onClose={closeEditModal}
                onSave={handleSave}
                onDelete={openConfirmModal}
                initialValue={item}
                isLoading={isLoading}
            ></EditGroupModal>
            <ConfirmModal
                title={confirmTitle}
                isOpen={isConfirmOpen}
                onConfirm={handleDelete}
                onCancel={closeConfirmModal}
            ></ConfirmModal>
        </Flex>
    );
};
