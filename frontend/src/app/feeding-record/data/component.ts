export interface ComponentDietType {
    name: string;
    dryMatter: number;
    neMaintenance: number;
    neGain: number;
    crudeProtein: number;
    degradableProtein: number;
    crudeFat: number;
    byProductPercentDM: number;
    roughagePercentDM: number;
    ndf: number;
    forageNDF: number;
    starch: number;
    calcium: number;
    phosphorus: number;
    salt: number;
    potassium: number;
    sulfur: number;
    cost: number;
}

export interface ComponentDietItem {
    id: string;
    name: string;
    sv: number;
    sp: number;
    cep: number;
    ndk: number;
    cost: number;
    inRation: boolean;
}
