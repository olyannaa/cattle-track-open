import { SortersAnimalsType } from '../utils/sortersAnimals';
import { columnsTableHistoryAssignmentNumbers } from '../global-components/daily-actions/history/columnsTables/columnsTableHistoryAssignmentNumbers';
import { columnsTableHistoryChangeGroup } from '../global-components/daily-actions/history/columnsTables/columnsTableHistoryChangeGroup';
import { columnsTableHistoryDisposal } from '../global-components/daily-actions/history/columnsTables/columnsTableHistoryDisposal';
import { columnsTableHistoryTransfer } from '../global-components/daily-actions/history/columnsTables/columnsTableHistoryTransfer';
import { columnsTableHistoryTreatment } from '../global-components/daily-actions/history/columnsTables/columnsTableHistoryTreatment';
import { columnsTableHistoryResearch } from '../global-components/daily-actions/history/columnsTables/columnsTableHistoryResearch';

export const getColumnsTable = (keyTab: string, sorters: SortersAnimalsType) => {
    switch (keyTab) {
        case '1':
            return columnsTableHistoryTreatment(sorters);
        case '2':
            return columnsTableHistoryResearch(sorters);
        case '4':
            return columnsTableHistoryTransfer(sorters);
        case '5':
            return columnsTableHistoryDisposal(sorters);
        case '6':
            return columnsTableHistoryAssignmentNumbers(sorters);
        case '7':
            return columnsTableHistoryChangeGroup(sorters);
        default:
            break;
    }
};
