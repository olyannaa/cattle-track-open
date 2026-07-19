import { Checkbox, CheckboxChangeEvent } from 'antd';

type Props = {
    onChange?: (e: CheckboxChangeEvent) => void;
    title: string;
    style?: React.CSSProperties;
    defaultChecked?: boolean;
};

export const CheckboxCustom = ({ title, onChange, style, defaultChecked }: Props) => {
    return (
        <Checkbox
            onChange={onChange}
            style={{
                maxWidth: '243.5px',
                padding: '8px 12px 10px',
                border: '1px solid var(--grey-border)',
                borderRadius: '2px',
                width: '100%',
                background: 'var(--global-bg)',
                ...style,
            }}
            defaultChecked={defaultChecked}
        >
            {title}
        </Checkbox>
    );
};
