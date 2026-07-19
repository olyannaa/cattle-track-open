import React, { useState } from 'react';
import {
    AudioOutlined,
    CloseOutlined,
    HistoryOutlined,
    LeftOutlined,
    PlusOutlined,
    RightOutlined,
    StarOutlined,
    UserOutlined,
} from '@ant-design/icons';
import { Button, Drawer, Flex, Layout } from 'antd';
import { Navigate, Outlet, useNavigate, useParams } from 'react-router-dom';
import { AppMenu } from './components/Menu/Menu';
import { useLogoutMutation } from '../../app-service/services/auth';
import { IUser } from '../../utils/userType';
import { useIsMobile } from '../../hooks/useIsMobile';
import { useWindowSize } from '../../hooks/useWindowSize';
import styles from './Layout.module.css';
import { getSiderStyle } from './helpers/siderStyle';
import { LogoSection } from './components/logo-section/LogoSection';
import { truncate } from '../../functions/truncate';
import { useDispatch } from 'react-redux';
import { feedingRecord } from '../../app/feeding-record/services/feeding-record';
import { AiAssistantCommand, AiEventInput } from '../../app/ai-event-input';

const { Header, Sider, Content } = Layout;

export const LayoutPage: React.FC = () => {
    const user: IUser = JSON.parse(localStorage.getItem('user') || '{}');
    const isMobile = useIsMobile();
    const [collapsed, setCollapsed] = useState(true);
    const [isAiAssistantOpen, setIsAiAssistantOpen] = useState(false);
    const [aiAssistantCommand, setAiAssistantCommand] = useState<AiAssistantCommand | undefined>();
    const [logout] = useLogoutMutation();
    const navigate = useNavigate();
    const windowWidth = useWindowSize();
    const dispatch = useDispatch();
    const isInvite = window.location.pathname.startsWith('/invite');
    const params = useParams();

    const handlerLogout = async () => {
        await logout().unwrap();
        localStorage.removeItem('user');
        dispatch(feedingRecord.util.resetApiState());
        navigate('/');
    };

    if (!user?.id)
        return <Navigate to={isInvite ? `/invite-auth/${params.token}` : '/'} />;

    return (
        <Layout style={{ minHeight: '100vh' }}>
            <Header className={styles['layout__header']}>
                <div
                    style={{
                        display: 'flex',
                        justifyContent: 'space-between',
                        alignItems: 'center',
                        width: '100%',
                    }}
                >
                    <div className={styles['header__content-logo']}>
                        <LogoSection
                            isMobile={isMobile}
                            windowWidth={windowWidth}
                            collapsed={collapsed}
                            setCollapsed={setCollapsed}
                        />
                    </div>
                    <Flex gap={'4px'} align='center'>
                        <Button type={'text'} title={user.organizationName}>
                            <UserOutlined />
                            {windowWidth >= 768
                                ? user.organizationName
                                : windowWidth > 650
                                ? truncate(user.organizationName, 60)
                                : windowWidth > 550
                                ? truncate(user.organizationName, 44)
                                : windowWidth > 500
                                ? truncate(user.organizationName, 37)
                                : windowWidth > 450
                                ? truncate(user.organizationName, 30)
                                : windowWidth > 400
                                ? truncate(user.organizationName, 22)
                                : windowWidth > 348
                                ? truncate(user.organizationName, 15)
                                : truncate(user.organizationName, 17)}
                        </Button>
                        {!isMobile && (
                            <Button onClick={handlerLogout} variant='link'>
                                Выход
                            </Button>
                        )}
                    </Flex>
                </div>
            </Header>
            <Layout style={{ minHeight: 'calc(100vh - 64px)' }}>
                {((user.organizationId === 'Нет организации' && isMobile) ||
                    user.organizationId !== 'Нет организации') && (
                    <Sider
                        trigger={null}
                        collapsible
                        collapsed={collapsed}
                        width='220'
                        style={getSiderStyle(isMobile, collapsed)}
                    >
                        <div className='demo-logo-vertical' />
                        <AppMenu
                            logout={handlerLogout}
                            isHiddenMenu={user.organizationId === 'Нет организации'}
                        />
                        {!isMobile && (
                            <div
                                onClick={() => setCollapsed(!collapsed)}
                                className={styles['trapezoid-button']}
                            >
                                <div className={styles['trapezoid-button__icon']}>
                                    {collapsed ? <RightOutlined /> : <LeftOutlined />}
                                </div>
                            </div>
                        )}
                    </Sider>
                )}
                <Layout
                    style={{
                        display: 'flex',
                        flexDirection: 'column',
                        flex: 1,
                    }}
                >
                    <Content className={styles['layout__content']}>
                        <Outlet />
                    </Content>
                </Layout>
            </Layout>
            <Drawer
                title={
                    <div className={styles['assistant-drawer__title']}>
                        <div className={styles['assistant-drawer__identity']}>
                            <span className={styles['assistant-drawer__avatar']}>
                                <StarOutlined />
                            </span>
                            <div>
                                <div className={styles['assistant-drawer__name']}>AI ассистент</div>
                                <div className={styles['assistant-drawer__status']}>
                                    <span className={styles['assistant-drawer__status-dot']} />
                                    Онлайн · готов помочь
                                </div>
                            </div>
                        </div>
                        <div className={styles['assistant-drawer__actions']}>
                            <button
                                className={styles['assistant-drawer__action']}
                                type='button'
                                aria-label='История диалога'
                                title='История'
                                onClick={() => setAiAssistantCommand({ type: 'history', nonce: Date.now() })}
                            >
                                <HistoryOutlined />
                            </button>
                            <button
                                className={styles['assistant-drawer__action']}
                                type='button'
                                aria-label='Новый диалог'
                                title='Новый диалог'
                                onClick={() => setAiAssistantCommand({ type: 'new', nonce: Date.now() })}
                            >
                                <PlusOutlined />
                            </button>
                            <button
                                className={styles['assistant-drawer__action']}
                                type='button'
                                aria-label='Закрыть AI ассистента'
                                title='Закрыть'
                                onClick={() => setIsAiAssistantOpen(false)}
                            >
                                <CloseOutlined />
                            </button>
                        </div>
                    </div>
                }
                open={isAiAssistantOpen}
                onClose={() => setIsAiAssistantOpen(false)}
                closable={false}
                mask={isMobile}
                width={isMobile ? '100%' : 560}
                destroyOnClose={false}
                classNames={{
                    header: styles['assistant-drawer__header'],
                    body: styles['assistant-drawer__body'],
                    content: styles['assistant-drawer__content'],
                }}
                styles={{ body: { overflow: 'hidden' } }}
            >
                <AiEventInput command={aiAssistantCommand} />
            </Drawer>
            {!isAiAssistantOpen && (
                <button
                    className={styles['assistant-launcher']}
                    type='button'
                    aria-label='Открыть AI ассистента'
                    onClick={() => setIsAiAssistantOpen(true)}
                >
                    <span className={styles['assistant-launcher__label']}>AI ассистент</span>
                    <span className={styles['assistant-launcher__button']}>
                        <AudioOutlined />
                    </span>
                </button>
            )}
        </Layout>
    );
};
