import { IUser } from './userType';

export enum Permissions {
    animalEditTable = 'animal table edit',
    test = 'test',
}

export const CheckPermissions = (permission: string): boolean => {
    const user: IUser = JSON.parse(localStorage.getItem('user') || '{}');
    if (!user.permissionIds.length) {
        return false;
    }

    return !!user.permissionIds.find((permissionCurr) => permissionCurr === permission);
};
