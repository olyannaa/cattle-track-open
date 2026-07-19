export interface IGroupRationInfo {
    groupId: string;
    groupName: string;
    activeAnimalsCount: number;
    morningFeeding: number;
    dayFeeding: number;
    nightFeeding: number;
    rationCostPerHead: number;
    totalRationCost: number;
    svPerHead: number;
    spPerHead: number;
    cepPerHead: number;
    ndkPerHead: number;
    totalSv: number;
    totalSp: number;
    totalCep: number;
    totalNdk: number;
    rationId: string;
    rationName: string;
}

export interface IGroupRation {
    rationId: string;
    name: string;
    description: string;
    organizationId: string;
    createdAt: string;
    totalDryMatter: string;
    totalNEMaintenance: number;
    totalNEGain: number;
    totalCrudeProtein: number;
    totalDegradableProtein: number;
    totalCrudeFat: number;
    totalByproduct: number;
    totalRoughage: number;
    totalNDF: number;
    totalForageNDF: number;
    totalStarch: number;
    totalCalcium: number;
    totalPhosphorus: number;
    totalSalt: number;
    totalPotassium: number;
    totalSulfur: number;
    totalCost: number;
    componentsCount: number;
}
