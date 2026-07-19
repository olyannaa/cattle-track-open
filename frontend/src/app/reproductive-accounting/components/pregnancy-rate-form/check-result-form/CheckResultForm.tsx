import { DatePicker, Form } from 'antd';
import { InputLabel } from '../../../../../global-components/custom-inputs/input-label/InputLabel';
import { FullPregnancyInfo } from '../../../services/reproductive';
import dayjs from 'dayjs';
import { useEffect } from 'react';
import useFormInstance from 'antd/es/form/hooks/useFormInstance';

export const CheckResultForm = ({ data }: { data: FullPregnancyInfo[] }) => {
    const checkResultType = Form.useWatch('status');
    const selectedCowId = Form.useWatch('cowId');
    const form = useFormInstance();

    useEffect(() => {
        if (!selectedCowId || !data) return;

        const selectedCow = data.find((animal) => animal.id === selectedCowId);
        if (selectedCow?.inseminationDate) {
            const calvingDate = dayjs(selectedCow.inseminationDate).add(285, 'day');
            form.setFieldsValue({ expectedCalvingDate: calvingDate });
        }
    }, [selectedCowId, data]);

    if (!checkResultType) {
        return null;
    }

    return (
        <>
            {checkResultType === 'Стельная' && (
                <div className='form-additional'>
                    <div className='form-input_default'>
                        <InputLabel label='Ожидаемая дата отёла' />
                        <Form.Item className='form-input_default' name='expectedCalvingDate'>
                            <DatePicker format='DD.MM.YYYY' type='date' className='form-input_default date' placeholder='xx.xx.xxxx'></DatePicker>
                        </Form.Item>
                    </div>
                </div>
            )}
        </>
    );
};
