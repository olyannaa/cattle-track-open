/* eslint-disable @typescript-eslint/no-explicit-any */
import { Form, Input } from 'antd';
import { InputLabel } from '../../../../../../global-components/custom-inputs/input-label/InputLabel';
import styles from '../ManualRegistration.module.css';
import aiStyles from './AdditionalInfoForm.module.css';
import { SelectResponseType, useGetAnimalIdentificationsQuery } from '../../../../services/registration-animal';
import { useEffect, useState } from 'react';

export const AdditionalInfoForm = ({ 
    withoutContainer = false, 
    form,
    initialValues,
    withoutClasses = false, 
}: { 
    withoutContainer?: boolean; 
    form?: any;
    initialValues?: Record<string, string>;
    withoutClasses?: boolean
}) => {
    const { data, refetch } = useGetAnimalIdentificationsQuery();
    const [fields, setFields] = useState<SelectResponseType[]>([]);

    useEffect(() => {
        refetch();
    }, []);

    useEffect(() => {
        if (data?.length) {
            setFields(data);
            
            if (form && initialValues) {
                const additionalInitialValues: Record<string, string> = {};
                
                data.forEach(field => {
                    const fieldValue = initialValues[field.name];
                    if (fieldValue !== undefined) {
                        additionalInitialValues[field.id] = fieldValue;
                    }
                });
                
                if (Object.keys(additionalInitialValues).length > 0) {
                    form.setFieldsValue(additionalInitialValues);
                }
            }
        }
    }, [data, form, initialValues]);

    if (!data || data?.length === 0) {
        return null;
    }

    return (
        <Form.Item>
            <div className={withoutContainer ? '' : styles['manual-register__additional-form']}>
                { !withoutClasses &&
                    <InputLabel
                        marginSize='16px'
                        label='Дополнительные способы идентификации'
                    />
                }
                <div className={ withoutClasses ? '' : aiStyles['additional-info__wrapper']}>
                    {fields.map((field) => (
                        <div key={field.id}>
                            <InputLabel label={field.name} />
                            <Form.Item
                                name={field.id}
                                className={ withoutClasses ? '' : styles['manual-register__changed-input']}
                                initialValue={initialValues?.[field.id]}
                            >
                                <Input placeholder='Введите значение' />
                            </Form.Item>
                        </div>
                    ))}
                </div>
            </div>
        </Form.Item>
    );
};