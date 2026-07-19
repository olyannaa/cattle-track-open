export type NewDietComponent = {
    componentId: string;
    count: number;
    cost: number;
};

export type NewDiet = {
    ratioName: string;
    groupId?: string;
    components: NewDietComponent[];
};
