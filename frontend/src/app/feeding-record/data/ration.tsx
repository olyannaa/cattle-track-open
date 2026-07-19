export interface IRation {
    rationId: string;
    rationName: string;
    rationDescription: string;
    createdAt: string;
    components: RationComponent[];
    totalCost: number;
    groupNames: string[];
}

export type RationComponent = {
    componentId: string;
    componentName: string;
    kg: number;
    cost: number;
    sv: number;
    cep: number;
    ndk: number;
    sp: number;
};
