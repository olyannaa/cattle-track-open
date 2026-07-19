import { ItemType, MenuItemType } from 'antd/es/menu/interface';
import {
    BellFilled,
    CarryOutFilled,
    ClockCircleFilled,
    FolderOpenFilled,
    HeartFilled,
    IdcardFilled,
    LogoutOutlined,
    PicRightOutlined,
    SafetyCertificateFilled,
    SettingOutlined,
    SwapOutlined,
    TeamOutlined,
} from '@ant-design/icons';
import { Menu } from 'antd';
import { Link, useLocation } from 'react-router-dom';
import { useMemo } from 'react';
import { useIsMobile } from '../../../../hooks/useIsMobile';
import { checkRole } from '../../../../const/roles';

export const AppMenu = ({
    logout,
    isHiddenMenu,
}: {
    logout: () => Promise<void>;
    isHiddenMenu: boolean;
}) => {
    const isMobile = useIsMobile();
    const mobileItems = isMobile
        ? [
              { type: 'divider' as const },
              {
                  key: 'logout',
                  icon: <LogoutOutlined />,
                  label: 'Выход',
                  onClick: logout,
                  danger: true,
              },
          ]
        : [];
    const location = useLocation();

    // Сопоставление путей с ключами меню
    const pathToKeyMap: Record<string, string> = {
        '/accounting': '1',
        '/animalregister': '2',
        '/infrastructure': '3',
        '/veterinary-activities': '4',
        '/livestock-movement': '5',
        '/reproductive-accounting': '6',
        '/weight-control': '7',
        '/animal-card': '8',
        '/feeding-records': '9',
        '/reports': '10',
        '/user-moderation': '11',
    };

    const selectedKey = useMemo(() => {
        const match = Object.entries(pathToKeyMap).find(([path]) =>
            location.pathname.startsWith(path)
        );
        return match ? [match[1]] : ['0'];
    }, [location.pathname]);

    const menuItems: ItemType<MenuItemType>[] = [
        {
            key: '1',
            icon: <FolderOpenFilled />,
            label: <Link to='/accounting'>Учет животных</Link>,
            danger: true,
        },
        {
            key: '2',
            icon: <IdcardFilled />,
            label: <Link to='/animalregister'>Регистрация животных</Link>,
            danger: true,
        },
        {
            key: '3',
            icon: <CarryOutFilled />,
            label: <Link to='/infrastructure'>Инфраструктура</Link>,
            danger: true,
        },
        {
            key: '4',
            icon: <HeartFilled />,
            label: <Link to='/veterinary-activities'>Ветеринарные мероприятия</Link>,
            danger: true,
        },
        {
            key: '5',
            icon: <SwapOutlined />,
            label: <Link to='/livestock-movement'>Движение поголовья</Link>,
            danger: true,
        },
        {
            key: '6',
            icon: <ClockCircleFilled />,
            label: <Link to='reproductive-accounting'>Репродуктивный учет</Link>,
            danger: true,
        },
        {
            key: '7',
            icon: <BellFilled />,
            label: <Link to='/weight-control'>Контроль привесов</Link>,
            danger: true,
        },
        {
            key: '8',
            icon: <PicRightOutlined />,
            label: <Link to='/animal-card'>Карточка животного</Link>,
            danger: true,
        },
        {
            key: '9',
            icon: <SafetyCertificateFilled />,
            label: <Link to='/feeding-records'>Учет кормления</Link>,
            danger: true,
        },
        {
            key: '10',
            icon: <SettingOutlined />,
            label: <Link to='/reports'>Отчеты</Link>,
            danger: true,
        },
        ...(checkRole('user')
            ? []
            : [
                  {
                      key: '11',
                      icon: <TeamOutlined />,
                      label: <Link to='/user-moderation'>Модерация пользователей</Link>,
                      danger: true,
                  },
              ]),
        ...mobileItems,
    ];

    return (
        <Menu
            theme='light'
            mode='inline'
            selectedKeys={selectedKey}
            items={isHiddenMenu ? mobileItems : menuItems}
        />
    );
};
