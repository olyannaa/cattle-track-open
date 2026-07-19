import { CheckboxGroupProps } from 'antd/es/checkbox';

export interface IInputFilterSearch {
    label: string;
    styles?: React.CSSProperties;
    placeholder?: string;
    onSearch?: (value: string) => void;
    value?: string;
    onChange?: (value: string) => void;
}

export interface ISelectFilter {
    placeholder?: string;
    options: { value: string; label: string }[];
    label: string;
    value: string;
    onChange: (value: string) => void;
    styles?: React.CSSProperties;
}

export interface IItemRadioGroupWithSwitch {
    isGroup: boolean;
    onChangeAll?: () => void;
    value: string;
    options: CheckboxGroupProps<string>['options'];
    onChange?: (value: string) => void;
    label: string;
    styles?: React.CSSProperties;
}
