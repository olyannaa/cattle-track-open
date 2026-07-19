import { Form, Select } from 'antd';
import { IFormItemSelect } from '../../../data/interface/FormInputs';
import { InputLabel } from '../../input-label/InputLabel';
import { useState } from 'react';

export const SelectInputForm = ({
    label,
    name,
    options,
    placeholder,
    styles,
    defaultValue,
    required = false,
}: IFormItemSelect) => {
    const [valueSearch, setValueSearch] = useState<string>('');
    const onSearch = (value: string) => {
        setValueSearch(value);
    };

    return (
        <div style={{ maxWidth: '491px', width: '100%', ...styles }}>
            <InputLabel label={label} required={required} />
            <Form.Item
                name={name}
                rules={[{ required: required, message: 'Сделайте выбор' }]}
                initialValue={defaultValue}
            >
                <Select
                    options={
                        valueSearch
                            ? [...options, { label: valueSearch, value: valueSearch }]
                            : options
                    }
                    placeholder={placeholder}
                    onChange={(value) => {
                        if (value !== valueSearch) {
                            setValueSearch('');
                        }
                    }}
                    showSearch
                    onSearch={onSearch}
                />
            </Form.Item>
        </div>
    );
};
