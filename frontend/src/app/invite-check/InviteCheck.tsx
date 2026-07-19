import { Button, Flex } from "antd"
import { useNavigate, useParams } from "react-router-dom"
import { useEffect, useState } from "react"
import { IUser } from "../../utils/userType"
import { useCheckInviteMutation } from "./services/inviteApi"
import { useLoginUpdateMutation } from "../../app-service/services/auth"

export const InviteCheck = () => {
    const user: IUser = JSON.parse(localStorage.getItem('user') || '{}');
    const token = useParams().token
    const [isInviteCheck, setIsInviteCheck] = useState<boolean>(false)
    const [checkInviteQuery] = useCheckInviteMutation()
    const [loginUpdate] = useLoginUpdateMutation()
    const navigate = useNavigate();
    const [isLoading, setIsLoading] = useState<boolean>(false)


    const checkInvite = async () => {
        setIsLoading(true)
        try{
            await checkInviteQuery(token || '').unwrap()
            const response = await loginUpdate().unwrap()
            const newUser: IUser = {
                ...response
            }
            localStorage.setItem('user', JSON.stringify(newUser));
            setIsInviteCheck(true)
        }catch(error){
            console.error('Failed to check invite', error);
        }finally{
            setIsLoading(false)
        }
    }

    useEffect(()=> {
        if (user.organizationId === 'Нет организации'){
            if (user.roleId === '8d5716d0-4cde-45f6-a67f-96323236b0f8'){
                navigate('/new-user')
            } else{
                checkInvite()
            }
        }
    }, [])

    return (
        <Flex align="center" vertical gap={20} style={{marginTop: '20px', padding:'0 10px'}}>
            {isLoading ? <div className="">Проверка</div> : 
            <>
                <div style={{textAlign:'center', fontSize:'20px', fontWeight: '500'}}>
                    {
                        user.organizationId !== 'Нет организации' && !isInviteCheck ? 
                        'Попросите админа удалить вас из организации и снова перейдите по ссылке' :
                        user.organizationId !== 'Нет организации' && isInviteCheck ? 
                        `Вы добавлены в ${user.organizationName}` : 
                        'Ссылка больше недействительна, попросите новую'
                    }
                </div>
                <Button 
                    type='primary'
                    size='large'
                    color='default'
                    variant='solid'
                    onClick={()=> navigate(user.organizationId === 'Нет организации' && !isInviteCheck ? '/new-user' : '/accounting')}
                >
                    Перейти на главную
                </Button>
            </>}
        </Flex>
        
    )
}
