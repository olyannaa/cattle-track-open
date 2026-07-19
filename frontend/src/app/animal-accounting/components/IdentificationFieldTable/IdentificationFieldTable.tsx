import { Input } from 'antd';
import { useState } from 'react';
import styles from './IdentificationFieldTable.module.css';
import { useAppDispatch } from '../../../../app-service/hooks';
import { updateChangedAnimalsMoreFields } from '../../services/animalsSlice';

type Props = {
    value: string;
    nameField: string;
    isEditTable: boolean;
    id: string;
};

export const IdentificationFieldTable = ({
    value,
    isEditTable,
    nameField,
    id,
}: Props) => {
    const [isOpenChange, setIsOpenChange] = useState<boolean>(false);
    const [name, setName] = useState<string>(value);
    const dispatch = useAppDispatch();

    const changeAnimal = async () => {
        setIsOpenChange(false);
        const newValue = name ? name.trim() : '';
        dispatch(
            updateChangedAnimalsMoreFields({
                id: id,
                nameField: nameField,
                value: newValue === value ? null : newValue,
            })
        );
    };

    const handlerOpenChange = (e: React.MouseEvent<HTMLSpanElement, MouseEvent>) => {
        e.stopPropagation();
        setIsOpenChange(true);
    };

    return isOpenChange && isEditTable ? (
        <Input
            value={name}
            onChange={(e) => setName(e.target.value)}
            onBlur={() => changeAnimal()}
            autoFocus
            className={styles[`input`]}
        />
    ) : (
        <div className={styles[`text-cell`]} onDoubleClick={(e) => handlerOpenChange(e)}>
            {name}
        </div>
    );
};
