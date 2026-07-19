import { IUser } from "../utils/userType";

export const roles = [
  {
    id: "8d5716d0-4cde-45f6-a67f-96323236b0f6",
    role: "user"
  },
  {
    id: "8d5716d0-4cde-45f6-a67f-96323236b0f7",
    role: "local_admin"
  },
  {
    id: "8d5716d0-4cde-45f6-a67f-96323236b0f8",
    role: "org_admin"
  }
]

export const getNameRoles = (id: string)=> {
    return roles.find((role) => role.id === id)?.role || ''
}

export const checkRole = (role: string)=> {
    const user: IUser = JSON.parse(localStorage.getItem('user') || '{}');
    return getNameRoles(user.roleId) === role
}