import { Form, InputNumber } from 'antd';
import { IFormItemInputNumber } from '../../../data/interface/FormInputs';
import { InputLabel } from '../../input-label/InputLabel';

export const NumberInputForm = ({
    label,
    name,
    styles,
    defaultValue,
    onChange,
    required = false,
}: IFormItemInputNumber) => {
    return (
        <div style={{ maxWidth: '416px', width: '100%', ...styles }}>
            <InputLabel label={label} required={required} />
            <Form.Item
                name={name}
                rules={[{ required: required, message: 'Введите значение' }]}
                initialValue={defaultValue}
            >
                <InputNumber min={1} onChange={onChange} style={{ width: '100%' }} />
            </Form.Item>
        </div>
    );
};
