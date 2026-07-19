import { useState } from 'react';
import { FormAddResearch } from '../forms/form-add-research/FormAddResearch';
import { useAppSelector } from '../../../../app-service/hooks';
import { selectIsGroup } from '../../../../app-service/slices/animalsDailyActionsSlice';
import { FormAddTreatment } from '../forms/form-add-treatment/FormAddTreatment';
import { AiTreatmentFormValues } from '../../../ai-event-input/aiFormMapping';

type Props = {
    resetHistory: () => void;
    isResearch?: boolean;
    aiTreatmentValues?: AiTreatmentFormValues;
};

export const WrapperForm = ({
    resetHistory,
    isResearch = false,
    aiTreatmentValues,
}: Props) => {
    const isGroup = useAppSelector(selectIsGroup);
    const [formsId, setFormsId] = useState<string[]>([
        Date.now().toString(36) + Math.random().toString(36).substring(2),
    ]);
    return formsId.map((id, i) =>
        isResearch ? (
            <FormAddResearch
                key={id}
                num={i + 1}
                formsIdLength={formsId.length}
                idForm={id}
                setFormsId={setFormsId}
                isGroup={isGroup}
                resetHistory={resetHistory}
            />
        ) : (
            <FormAddTreatment
                key={id}
                formsIdLength={formsId.length}
                idForm={id}
                setFormsId={setFormsId}
                isGroup={isGroup}
                resetHistory={resetHistory}
                num={i + 1}
                aiFormValues={i === 0 ? aiTreatmentValues : undefined}
            />
        )
    );
};
