import { Checkbox, CheckboxChangeEvent, Flex } from 'antd';
import { useAppDispatch, useAppSelector } from '../../app-service/hooks';
import {
    addAction,
    deleteAction,
    selectSelectedDailyActions,
} from '../../app-service/slices/dailyActionsSlice';
import {
    addSelectedAnimals,
    deleteSelectedAnimals,
    selectSelectedAnimals,
} from '../../app-service/slices/animalsDailyActionsSlice';

type Props = {
    id: string;
    isAnimal?: boolean;
};

export const TableCheckbox = ({ id, isAnimal = false }: Props) => {
    const dispatch = useAppDispatch();
    const actions = useAppSelector(selectSelectedDailyActions);
    const animals = useAppSelector(selectSelectedAnimals);
    const handlerChange = (e: CheckboxChangeEvent) => {
        if (isAnimal) {
            if (e.target.checked) {
                dispatch(addSelectedAnimals(id));
            } else {
                dispatch(deleteSelectedAnimals(id));
            }
        } else {
            if (e.target.checked) {
                dispatch(addAction(id));
            } else {
                dispatch(deleteAction(id));
            }
        }
    };

    return (
        <Flex>
            <Checkbox
                onChange={handlerChange}
                checked={
                    isAnimal
                        ? !!animals.find((animal) => animal === id)
                        : !!actions.find((action) => action === id)
                }
                style={{ marginLeft: '20px' }}
            />
        </Flex>
    );
};
