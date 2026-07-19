// SettingsDrugTable.tsx
import { Button, Flex } from 'antd';
import { DeleteOutlined, EditOutlined } from '@ant-design/icons';
import { IDrugsTable } from '../../../data/interface/IDrugsTable';
import { useState } from 'react';
import { ModalCreateAndEditDrug } from './modal-create-and-edit-drug/ModalCreateAndEditDrug';
import { ConfirmModal } from '../../../../../global-components/confirm-modal/ConfirmModal';
import { useDeleteDrugMutation } from '../../../services/drugs';
import type { MessageInstance } from 'antd/es/message/interface';

type Props = {
    drug: IDrugsTable;
    messageApi: MessageInstance;
};

export const SettingsDrugTable = ({ drug, messageApi }: Props) => {
    const [openEditModal, setOpenEditModal] = useState(false);
    const [openConfirmModal, setOpenConfirmModal] = useState(false);
    const [deleteDrug] = useDeleteDrugMutation();

    const handleDelete = async () => {
        try {
            await deleteDrug(drug.id).unwrap();
            messageApi.success('Препарат успешно удален');
        } catch {
            messageApi.error('Ошибка при удалении препарата');
        } finally {
            setOpenConfirmModal(false);
        }
    };

    return (
        <>
            <Flex gap={8}>
                <Button
                    type='text'
                    style={{ padding: 0, width: '32px' }}
                    onClick={() => setOpenEditModal(true)}
                >
                    <EditOutlined />
                </Button>
                <Button
                    type='text'
                    danger
                    style={{ padding: 0, width: '32px' }}
                    onClick={() => setOpenConfirmModal(true)}
                >
                    <DeleteOutlined />
                </Button>
            </Flex>
            {openEditModal && (
                <ModalCreateAndEditDrug
                    open={openEditModal}
                    setOpen={setOpenEditModal}
                    drug={drug}
                    messageApi={messageApi}
                />
            )}
            <ConfirmModal
                isOpen={openConfirmModal}
                onCancel={() => setOpenConfirmModal(false)}
                onConfirm={handleDelete}
                title='Вы действительно хотите удалить препарат?'
            />
        </>
    );
};
