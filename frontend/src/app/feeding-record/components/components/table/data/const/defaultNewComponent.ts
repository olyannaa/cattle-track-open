import { ComponentDietItem } from '../../../../../data/component';
import { v4 as uuidv4 } from 'uuid';

export type ComponentItem = ComponentDietItem & {
    isNew?: boolean;
};

export const defaultComponent = (): ComponentItem => ({
    id: uuidv4(),
    name: '',
    sv: 0,
    sp: 0,
    cep: 0,
    ndk: 0,
    cost: 0,
    isNew: true,
    inRation: false,
});
