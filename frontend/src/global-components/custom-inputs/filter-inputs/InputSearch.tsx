import { Input } from 'antd';

import { IInputFilterSearch } from '../../data/interface/InputsFilter';
import { InputLabel } from '../input-label/InputLabel';

export const InputSearch = ({
    label,
    placeholder,
    onSearch,
    styles,
    value,
    onChange,
}: IInputFilterSearch) => {
    const { Search } = Input;
    return (
        <div style={{ maxWidth: '491px', width: '100%', ...styles }}>
            <InputLabel label={label} />
            <Search
                placeholder={placeholder}
                enterButton='Найти'
                size='large'
                onSearch={onSearch}
                value={value}
                onChange={(e) => onChange?.(e.target.value)}
            />
        </div>
    );
};
