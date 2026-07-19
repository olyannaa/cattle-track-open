import { Form, Radio } from 'antd';

import { InputLabel } from '../../input-label/InputLabel';
import { IFormItemRadioGroup } from '../../../data/interface/FormInputs';

export const RadioGroupForm = ({
    label,
    options,
    name,
    styles,
    required = false,
}: IFormItemRadioGroup) => {
    return (
        <div style={{ width: '100%' }}>
            <InputLabel label={label} required={required} />
            <Form.Item
                name={name}
                rules={[{ required: required, message: 'Сделайте выбор' }]}
            >
                <Radio.Group
                    style={{
                        display: 'flex',
                        flexWrap: 'wrap',
                        ...styles,
                    }}
                    block
                    options={options}
                    optionType='button'
                />
            </Form.Item>
        </div>
    );
};
