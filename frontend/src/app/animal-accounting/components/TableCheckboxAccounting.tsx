import { Checkbox, CheckboxChangeEvent, Flex } from 'antd';
import { useAppDispatch, useAppSelector } from '../../../app-service/hooks';
import {
    addDeleteAnimals,
    deleteDeleteAnimals,
    selectDeleteAnimals,
} from '../services/animalsSlice';

type Props = {
    id: string;
};

export const TableCheckboxAccounting = ({ id }: Props) => {
    const dispatch = useAppDispatch();
    const deleteAnimals = useAppSelector(selectDeleteAnimals);
    const handlerChange = (e: CheckboxChangeEvent) => {
        if (e.target.checked) {
            dispatch(addDeleteAnimals(id));
        } else {
            dispatch(deleteDeleteAnimals(id));
        }
    };

    return (
        <Flex>
            <Checkbox
                onChange={handlerChange}
                checked={!!deleteAnimals.find((animal) => animal === id)}
                style={{ marginLeft: '20px' }}
            />
        </Flex>
    );
};
