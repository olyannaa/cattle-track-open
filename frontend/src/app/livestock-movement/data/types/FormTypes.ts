export type FormTypeTransfer = {
    dateTransfer: string | undefined;
    group: string | undefined;
    name: string | undefined;
    note: string | undefined;
};

export type FormTypeAssigmentNumber = {
    date: string | undefined;
    name: string | undefined;
    type: string | undefined;
    value: string | undefined;
};

export type FormTypeDisposal = {
    dateCulling: string | undefined;
    name: string | undefined;
    reason: string | undefined;
};

export type FormTypeChangeAgeGenderGroup = {
    date: string | undefined;
    name: string | undefined;
    notes: string | undefined;
};
