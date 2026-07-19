import { Flex, message } from 'antd';
import { useEffect } from 'react';

import { useAppDispatch, useAppSelector } from '../../../app-service/hooks';

import { getNameTabs, items } from '../data/const/tabs';

import { WrapperForm } from './wrapper-form/WrapperForm';
import { FilterAnimals } from '../../../global-components/filter-animals/FilterAnimals';

import {
    deleteAllActions,
    selectSortersDailyActions,
} from '../../../app-service/slices/dailyActionsSlice';
import {
    changeIsGroup,
    selectKeyTab,
} from '../../../app-service/slices/animalsDailyActionsSlice';
import {
    useLazyGetDailyActionsQuery,
    useLazyGetPaginationInfoDailyActionsQuery,
} from '../../../app-service/services/dailyActions';

export const TabsContent = () => {
    const sorters = useAppSelector(selectSortersDailyActions);
    const keyTab = useAppSelector(selectKeyTab);
    const [, contextHolder] = message.useMessage();
    const title = items && items.find((item) => item.key === keyTab)?.label?.toString();
    console.log('keyTab in TabsContent:', keyTab);
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
            {contextHolder}
            <h2>{title}</h2>
            <FilterAnimals />
            <>
                {keyTab === '1' && (
                    <WrapperForm resetHistory={resetHistory} />
                )}
                {keyTab === '2' && <WrapperForm resetHistory={resetHistory} isResearch />}
            </>
        </Flex>
    );
};
