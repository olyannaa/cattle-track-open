import { Flex } from 'antd';
import { AssignmentRation } from './components/assignment-ration/AssignmentRation';
import FeedingPlanTable from './components/feeding-plan-table/FeedingPlanTable';
import { useState } from 'react';

export const MainWrapper = () => {
    const [needsRefresh, setNeedsRefresh] = useState(false);
    return (
        <Flex vertical className='content-container content_without-max-width'>
            <FeedingPlanTable refresh={needsRefresh} setRefresh={setNeedsRefresh} />
            <h2 className='form-title'>Присвоение рациона</h2>
            <AssignmentRation refreshPlan={setNeedsRefresh} />
        </Flex>
    );
};
