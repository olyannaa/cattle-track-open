import { Select } from 'antd';

import { ISelectFilter } from '../../data/interface/InputsFilter';
import { InputLabel } from '../input-label/InputLabel';

export const SelectFilters = ({
    label,
    options,
    placeholder,
    value,
    onChange,
    styles,
}: ISelectFilter) => {
    return (
        <div style={{ maxWidth: '491px', width: '100%', ...styles }}>
            <InputLabel label={label} />
            <Select
                options={options}
                placeholder={placeholder}
                style={{ width: '100%' }}
                value={value}
                onChange={(el) => onChange(el)}
            />
        </div>
    );
};
