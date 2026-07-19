import { Flex } from 'antd';
import { useEffect } from 'react';
import { FilterAnimals } from '../../../global-components/filter-animals/FilterAnimals';
import { FormAddTransfer } from './forms/FormAddTransfer';
import { FormAddDisposal } from './forms/FormAddDisposal';
import { useAppDispatch, useAppSelector } from '../../../app-service/hooks';
import {
    changeIsGroup,
    selectKeyTab,
} from '../../../app-service/slices/animalsDailyActionsSlice';
import { FormAddAssigmentNumber } from './forms/FormAddAssigmentNumber';
import { getNameTabs, items } from '../data/const/tabs';
import {
    useLazyGetDailyActionsQuery,
    useLazyGetPaginationInfoDailyActionsQuery,
} from '../../../app-service/services/dailyActions';
import {
    deleteAllActions,
    selectSortersDailyActions,
} from '../../../app-service/slices/dailyActionsSlice';
import { FormChangeAgeGenderGroup } from './forms/FormChangeAgeGenderGroup';

export const TabsContent = () => {
    const sorters = useAppSelector(selectSortersDailyActions);
    const keyTab = useAppSelector(selectKeyTab);
    const title = items && items.find((item) => item.key === keyTab)?.label?.toString();

    const dispatch = useAppDispatch();

    const [getDailyActionsQuery] = useLazyGetDailyActionsQuery();
    const [getPaginationInfoDailyActionsQuery] =
        useLazyGetPaginationInfoDailyActionsQuery();

    const resetHistory = async () => {
        const name = getNameTabs(keyTab);
        await getDailyActionsQuery({
            ...sorters,
            type: name,
        });
        dispatch(deleteAllActions());
        await getPaginationInfoDailyActionsQuery(name);
    };

    useEffect(() => {
        dispatch(changeIsGroup(false));
    }, [keyTab]);

    return (
        <Flex vertical style={{ width: '100%' }} gap={24}>
            <h2>{title}</h2>
            <FilterAnimals />
            {keyTab === '1' && <FormAddTransfer resetHistory={resetHistory} />}
            {keyTab === '5' && <FormAddDisposal resetHistory={resetHistory} />}
            {keyTab === '6' && <FormAddAssigmentNumber resetHistory={resetHistory} />}
            {keyTab === '7' && <FormChangeAgeGenderGroup resetHistory={resetHistory} />}
        </Flex>
    );
};
