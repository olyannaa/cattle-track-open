/* eslint-disable @typescript-eslint/no-explicit-any */
import { Radio } from 'antd';
import { CheckboxGroupProps } from 'antd/es/checkbox';

export interface IRadioGroup {
    title: string;
    options: CheckboxGroupProps<string>['options'];
}

export const RadioGroupButton = ({
    data,
    value,
    onChange,
}: {
    data: IRadioGroup;
    value?: string;
    onChange?: (e: any) => void;
}) => {
    return (
        <Radio.Group
            style={{
                display: 'flex',
                flexWrap: 'wrap',
                maxWidth: '432px',
            }}
            block
            options={data.options}
            optionType='button'
            value={value}
            onChange={onChange}
        />
    );
};
