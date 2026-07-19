import { Form, Input } from 'antd';
import { InputLabel } from '../../input-label/InputLabel';
import { IFormItemInput } from '../../../data/interface/FormInputs';

export const InputForm = ({ name, label, placeholder, required = false, styles, className, defaultValue }: IFormItemInput) => {
    return (
        <div style={{ maxWidth: '475px', width: '100%', ...styles }} className={className}>
            <InputLabel label={label} required={required} />
            <Form.Item name={name} rules={[{ required: required, message: 'Заполните поле' }]} initialValue={defaultValue}>
                <Input placeholder={placeholder} />
            </Form.Item>
        </div>
    );
};
