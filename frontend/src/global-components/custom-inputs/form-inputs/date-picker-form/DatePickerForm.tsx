import { DatePicker, Form } from 'antd';
import { IFormItem } from '../../../data/interface/FormInputs';
import { InputLabel } from '../../input-label/InputLabel';

export const DatePickerForm = ({
    name,
    label,
    required = false,
    defaultValue,
    styles,
}: IFormItem) => {
    return (
        <div style={{ maxWidth: '475px', width: '100%', ...styles }}>
            <InputLabel label={label} required={required} />
            <Form.Item
                name={name}
                rules={[{ required: required, message: 'Заполните дату' }]}
                initialValue={defaultValue}
            >
                <DatePicker
                    placeholder='xx.xx.xxxx'
                    format='DD.MM.YYYY'
                    style={{ width: '100%' }}
                    size='large'
                />
            </Form.Item>
        </div>
    );
};
