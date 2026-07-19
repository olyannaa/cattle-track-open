import { Input } from 'antd';
import { ChangeEvent } from 'react';

type DrugSearchProps = {
    value: string;
    onChange: (value: string) => void;
};

export function DrugSearch({ value, onChange }: DrugSearchProps) {
    const handleChange = (e: ChangeEvent<HTMLInputElement>) => {
        onChange(e.target.value);
    };

    return (
        <Input
            value={value}
            onChange={handleChange}
            placeholder='Поиск по названию, веществу или производителю'
            allowClear
        />
    );
}
