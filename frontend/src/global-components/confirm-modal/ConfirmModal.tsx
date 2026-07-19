import { Button, Drawer, Flex, Modal } from 'antd';
import { CloseCircleOutlined } from '@ant-design/icons';
import styles from './ConfirmModal.module.css';
import { useEffect, useState } from 'react';

export interface IConfirmModal {
    title: string;
    isOpen: boolean;
    onConfirm: () => void;
    okButtonText?: string;
    cancelButtonText?: string;
    onCancel?: () => void;
}

export const ConfirmModal = ({
    title,
    isOpen,
    onConfirm,
    okButtonText = 'Да',
    cancelButtonText = 'Нет',
    onCancel = () => {},
}: IConfirmModal) => {
    const [isMobile, setIsMobile] = useState<boolean>(false);

    useEffect(() => {
        const checkIfMobile = () => {
            setIsMobile(window.innerWidth <= 748);
        };
        checkIfMobile();
        window.addEventListener('resize', checkIfMobile);
        return () => {
            window.removeEventListener('resize', checkIfMobile);
        };
    }, []);

    if (isMobile) {
        return (
            <Drawer
                placement='bottom'
                open={isOpen}
                closable={false}
                height='auto'
                onClose={onCancel}
                className={styles['confirm-modal__drawer']}
            >
                <Flex
                    justify='space-between'
                    align='start'
                    style={{ marginBottom: 24 }}
                >
                    <h3>{title}</h3>
                    <CloseCircleOutlined
                        onClick={onCancel}
                        style={{ fontSize: 24, color: 'rgba(0, 0, 0, 0.45)' }}
                    />
                </Flex>
                <Flex
                    gap={8}
                    className={styles['confirm-modal__button-container']}
                >
                    <Button
                        className={styles['confirm-modal__button']}
                        onClick={onCancel}
                    >
                        {cancelButtonText}
                    </Button>
                    <Button
                        className={styles['confirm-modal__button']}
                        type='primary'
                        onClick={onConfirm}
                    >
                        {okButtonText}
                    </Button>
                </Flex>
            </Drawer>
        );
    }

    return (
        <Modal
            title={title}
            open={isOpen}
            width={464}
            onCancel={onCancel}
            closeIcon={
                <CloseCircleOutlined
                    style={{ fontSize: 24, color: 'rgba(0, 0, 0, 0.45)' }}
                />
            }
            footer={
                <Flex
                    className={styles['confirm-modal__button-container']}
                    gap={8}
                >
                    <Button
                        className={styles['confirm-modal__button']}
                        onClick={onCancel}
                    >
                        {cancelButtonText}
                    </Button>
                    <Button
                        className={styles['confirm-modal__button']}
                        type='primary'
                        onClick={() => onConfirm()}
                    >
                        {okButtonText}
                    </Button>
                </Flex>
            }
        ></Modal>
    );
};
