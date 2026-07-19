import { Select } from "antd"
import { IUserTable } from "../../../UserManagement"
import { useChangeUserRoleMutation, useLazyGetUsersModerationQuery } from "../../../../../services/apiModeration"
import { RoleIcon } from "../../../../role-icon/RoleIcon"
import { useState } from "react"
import { ConfirmModal } from "../../../../../../../global-components/confirm-modal/ConfirmModal"

type Props = {
    user: IUserTable
}

export const ChangeRoleTable = ({user}: Props) => {
    const [isOpenChange, setIsOpenChange] = useState<boolean>(false);
    const [selectedRoleId, setSelectedRoleId] = useState<string | null>(null);
    const [changeRole] = useChangeUserRoleMutation()
    const [getUsers] = useLazyGetUsersModerationQuery()

    const openConfirmRoleChange = (value: string)=> {
        setIsOpenChange(false)
        if (value !== user.roleId) {
            setSelectedRoleId(value)
        }
    }

    const handleChangeRole = async ()=> {
        if (!selectedRoleId) return;
        try{
            await changeRole({userId: user.id, roleId: selectedRoleId}).unwrap()
            await getUsers().unwrap()
        }catch(error){
            console.error('Failed to change user role', error);
        } finally {
            setSelectedRoleId(null)
        }
    }

    return (
        <>
            {isOpenChange ?
                <Select
                    onChange={(value) => openConfirmRoleChange(value)}
                    defaultValue={user.roleId}
                    onBlur={()=> setIsOpenChange(false)} autoFocus
                    options={[
                        {
                            value: "8d5716d0-4cde-45f6-a67f-96323236b0f6",
                            label: "Пользователь"
                        },
                        {
                            value: "8d5716d0-4cde-45f6-a67f-96323236b0f7",
                            label: "Лок. админ"
                        },
                        {
                            value: "8d5716d0-4cde-45f6-a67f-96323236b0f8",
                            label: "Орг. админ"
                        }
                    ]}
                    style={{fontSize:'12px', height: '22px', maxWidth: '113px', padding:' 0 3px'}}
                /> : <div onDoubleClick={()=> setIsOpenChange(true)}><RoleIcon role={user.roleId}/></div>
            }
            <ConfirmModal
                isOpen={selectedRoleId !== null}
                title={`Изменить роль пользователя ${user.login}?`}
                okButtonText='Изменить'
                cancelButtonText='Отмена'
                onConfirm={handleChangeRole}
                onCancel={() => setSelectedRoleId(null)}
            />
        </>
    )
}
