import { IUser } from './userType';

export function getUser() {
    const user: IUser = JSON.parse(localStorage.getItem('user') || '');
    return user;
}
