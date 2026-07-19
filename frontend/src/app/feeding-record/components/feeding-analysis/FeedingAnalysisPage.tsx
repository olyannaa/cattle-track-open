import { Flex, Select } from 'antd';
import { GroupInfo } from './components/group-info/GroupInfo';
import { CommonConsumptionChart } from './components/common-consuption-chart/CommonConsumptionChart';
import { useState } from 'react';
import { useGetGroupQuery } from '../../../../app-service/services/general';
import { CommonCostChart } from './components/common-cost-chart/CommonCostChart';
import { CostChartMonth } from './components/cost-chart-month/CostChartMonth';
import { GroupChartConsumptionMonth } from './components/group-chart-consumption/GroupChartConsuptionMonth';
import { NutrientsDynamicChart } from './components/nutrients-dynamics-chart/NutrientsDymamicChart';

export const FeedingAnalysisPage = () => {
    const [currentGroup, setCurrentGroup] = useState(null);
    const groups = useGetGroupQuery().data?.map((field) => ({
        label: field.name,
        value: field.id,
    }));

    return (
        <Flex vertical className='content-container content_without-max-width'>
            <CommonConsumptionChart />
            <Flex vertical className='form-input_default form-title'>
                <h3 className='form-title'>Выбор группы</h3>
                <Select
                    options={groups}
                    onChange={(e) => setCurrentGroup(e)}
                    placeholder='Выберите группу'
                ></Select>
            </Flex>
            {currentGroup && <GroupInfo id={currentGroup} />}
            {currentGroup && <GroupChartConsumptionMonth id={currentGroup} />}
            {currentGroup && <CostChartMonth id={currentGroup} />}
            {currentGroup && <CommonCostChart id={currentGroup} />}
            {currentGroup && <NutrientsDynamicChart id={currentGroup} />}
        </Flex>
    );
};
