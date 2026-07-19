import { Flex, Modal, Button, Drawer } from 'antd';
import { CloseCircleOutlined } from '@ant-design/icons';
import { useEffect, useState } from 'react';

export type ResultCalvingModal = {
    mother?: string;
    calfType?: string;
    date?: string;
};

type ConfirmCalvingModalProps = {
    open: boolean;
    onClose: () => void;
    data: ResultCalvingModal;
};

export const ConfirmCalvingModal = ({ open, onClose, data }: ConfirmCalvingModalProps) => {
    const [isMobile, setIsMobile] = useState(false);

    useEffect(() => {
        const checkMobile = () => {
            setIsMobile(window.innerWidth <= 440);
        };
        checkMobile();
        window.addEventListener('resize', checkMobile);
        return () => window.removeEventListener('resize', checkMobile);
    }, []);
    return (
        <>
            {isMobile && (
                <Drawer
                    placement='bottom'
                    closable={false}
                    onClose={onClose}
                    open={open}
                    height='100%'
                    footer={
                        <Button type='primary' block onClick={onClose} style={{ marginBottom: '24px' }}>
                            Понятно
                        </Button>
                    }
                >
                    <Flex justify='space-between' align='start' style={{ marginBottom: 24 }}>
                        <h2>Отёл успешно зарегистрирован!</h2>
                        <CloseCircleOutlined onClick={onClose} style={{ fontSize: 24, color: 'rgba(0, 0, 0, 0.45)' }} />
                    </Flex>
                    <Flex vertical style={{ marginTop: '24px' }}>
                        <div>
                            <p>🐮 Мать: {data?.mother}</p>
                            <p>👶 Теленок: {data?.calfType}</p>
                            <p>📅 Дата отёла: {data?.date}</p>
                        </div>
                    </Flex>
                </Drawer>
            )}
            {!isMobile && (
                <Modal title='Отел успешно зарегистрирован' open={open} onCancel={onClose} footer={null} closeIcon={<CloseCircleOutlined style={{ fontSize: 24, color: 'rgba(0, 0, 0, 0.45)' }} />}>
                    <Flex vertical style={{ marginTop: '24px' }}>
                        <div>
                            <p>🐮 Мать: {data?.mother}</p>
                            <p>👶 Теленок: {data?.calfType}</p>
                            <p>📅 Дата отёла: {data?.date}</p>
                        </div>
                        <Button type='primary' style={{ marginTop: '24px' }} onClick={onClose}>
                            Понятно
                        </Button>
                    </Flex>
                </Modal>
            )}
        </>
    );
};
