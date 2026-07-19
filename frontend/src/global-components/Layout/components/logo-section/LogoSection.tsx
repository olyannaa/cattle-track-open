import { Button } from 'antd';
import styles from '../../Layout.module.css';
import { AppstoreFilled } from '@ant-design/icons';
import logo from '../../../../assets/header-logo.svg';
import mobileLogo from '../../../../assets/mobile-logo.svg';

type Props = {
    isMobile: boolean;
    windowWidth: number;
    collapsed: boolean;
    setCollapsed: (value: boolean) => void;
};

export const LogoSection = ({ isMobile, windowWidth, collapsed, setCollapsed }: Props) => (
    <div className={styles['header__content-logo']}>
        {isMobile && (
            <Button
                type='primary'
                danger
                icon={<AppstoreFilled />}
                onClick={() => setCollapsed(!collapsed)}
                style={{ marginRight: 4 }}
            />
        )}
        <img className={styles['layout__logo']} src={windowWidth < 348 ? mobileLogo : logo} />
    </div>
);
